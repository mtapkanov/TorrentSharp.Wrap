//
// Created by Albie on 01/05/2024.
//

#ifndef TSW_STRUCT_ALIGN_H
#define TSW_STRUCT_ALIGN_H

#ifdef _MSC_VER
#define TSW_STRUCT __declspec(align(8))
#elif defined(__GNUC__) || defined(__clang__)
#define TSW_STRUCT __attribute__((aligned(8)))
#else
#error "Unsupported compiler"
#endif

#endif //TSW_STRUCT_ALIGN_H
