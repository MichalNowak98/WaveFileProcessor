#pragma once
#include <stdint.h>
#ifdef ASM_DYNAMIC_LIBRARY_EXPORTS
#define ASM_DYNAMIC_LIBRARY_API __declspec(dllexport)
#else
#define ASM_DYNAMIC_LIBRARY_API __declspec(dllimport)
#endif

extern "C" int __cdecl addBytesAsm(int a, int b);

extern "C" ASM_DYNAMIC_LIBRARY_API int addBytesWTF(int a, int b);

extern "C" ASM_DYNAMIC_LIBRARY_API int addBytesCpp(int a, int b);