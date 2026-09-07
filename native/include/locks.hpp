//
// Created by Albie on 25/03/2024.
//

#ifndef TSW_LOCKS_HPP
#define TSW_LOCKS_HPP

#include <mutex>

class lock {

private:
    std::mutex &m_;
    bool lock_taken;

public:
    explicit lock(std::mutex &m) : m_(m) {
        lock_taken = m.try_lock();
    }

    ~lock() {
        if (lock_taken) {
            m_.unlock();
        }
    }

    bool isLockTaken() const {
        return lock_taken;
    }
};

#endif //TSW_LOCKS_HPP
