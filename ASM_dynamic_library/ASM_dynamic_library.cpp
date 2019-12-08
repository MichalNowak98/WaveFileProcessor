#include "pch.h"
#include <utility>
#include <limits.h>
#include "ASM_dynamic_library.h"


int addBytesCpp(int a, int b)
{
	return a + b;
}

int addBytesWTF(int a, int b)
{
	return addBytesAsm(a, b);
}