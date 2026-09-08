namespace TorrentSharp.Wrap;

/// <summary>
/// A read-only stream over a torrent file, translating byte-range reads into piece fetches via
/// <see cref="TorrentManager.MapFileRange"/> and <see cref="TorrentManager.ReadPieceAsync"/>, with
/// a small sliding prefetch window ahead of the read position. Returned by
/// <see cref="TorrentManager.OpenFileStream"/> - each instance owns its own single-piece cache and
/// lock, so opening several streams doesn't serialize or evict each other's reads.
/// </summary>
internal sealed class TorrentFileStream(TorrentManager manager, TorrentManagerFile file, TimeSpan? pieceTimeout) : Stream
{
    // Сколько кусков вперёд от текущего чтения запрашивать заранее и с каким шагом между их
    // дедлайнами. Держит воспроизведение плавным при последовательном чтении/обычных перемотках,
    // не прося при этом скачать весь остаток файла целиком, как это сделала бы приоритетность
    // "качать всё".
    private const int PrefetchPieceCount = 3;
    private const int PrefetchStepMs = 1000;

    private readonly SemaphoreSlim _readLock = new(1, 1);
    private int _activePieceIndex = -1;
    private byte[]? _activePieceData;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => file.Info.FileSize;
    public override long Position { get; set; }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (Position >= Length || buffer.IsEmpty)
            return 0;

        var remaining = Length - Position;
        var requestSize = (int)Math.Min(buffer.Length, remaining);

        await _readLock.WaitAsync(cancellationToken);
        try
        {
            var mapping = manager.MapFileRange(file.Info.Index, Position, requestSize);
            if (mapping.Piece < 0)
                throw new InvalidOperationException("Unable to map the requested byte range to a piece.");

            // Быстрый путь: кусок уже полностью скачан, значит его байты уже лежат в реальном
            // файле на диске - читаем их напрямую вместо того, чтобы платить за
            // set_piece_deadline -> планировщик libtorrent -> чтение с диска -> read_piece_alert
            // -> маршалинг обратно в C#. У этого цикла есть заметная задержка даже когда ничего
            // скачивать не нужно.
            if (manager.HavePiece(mapping.Piece))
            {
                var directRead = await ReadFromDiskAsync(buffer[..requestSize], Position, cancellationToken);
                if (directRead > 0)
                {
                    Position += directRead;
                    return directRead;
                }
            }

            var pieceData = await GetPieceAsync(mapping.Piece, cancellationToken);
            var sliceLength = Math.Min(mapping.Length, pieceData.Length - mapping.Offset);

            pieceData.AsSpan(mapping.Offset, sliceLength).CopyTo(buffer.Span);

            for (var i = 1; i <= PrefetchPieceCount; i++)
                manager.SetPieceDeadline(mapping.Piece + i, i * PrefetchStepMs);

            Position += sliceLength;
            return sliceLength;
        }
        finally
        {
            _readLock.Release();
        }
    }

    // переиспользует последний кусок, если это тот же самый - последовательные мелкие чтения в
    // пределах одного куска (частый случай) не переспрашивают нативное чтение на каждый вызов.
    private async Task<byte[]> GetPieceAsync(int piece, CancellationToken cancellationToken)
    {
        if (_activePieceIndex == piece && _activePieceData is not null)
            return _activePieceData;

        var data = await manager.ReadPieceAsync(piece, cancellationToken, pieceTimeout);
        _activePieceIndex = piece;
        _activePieceData = data;
        return data;
    }

    private async ValueTask<int> ReadFromDiskAsync(Memory<byte> buffer, long position, CancellationToken cancellationToken)
    {
        await using var fileStream = new FileStream(
            file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 1, useAsync: true);

        fileStream.Seek(position, SeekOrigin.Begin);
        return await fileStream.ReadAsync(buffer, cancellationToken);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();

    public override long Seek(long offset, SeekOrigin origin)
    {
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        return Position;
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _readLock.Dispose();

        base.Dispose(disposing);
    }
}
