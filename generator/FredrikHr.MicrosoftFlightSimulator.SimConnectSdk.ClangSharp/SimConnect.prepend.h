#include <Windows.h>

typedef unsigned char SIMCONNECT_STRINGCHAR;
#define SIMCONNECT_STRING(name, size) SIMCONNECT_STRINGCHAR name[size]

#ifdef MSVC
#define SIMCONNECT_REFSTRUCT struct
//#define SIMCONNECT_ENUM_FLAGS typedef DWORD
#else // !MSVC
#define SIMCONNECT_REFSTRUCT struct __attribute__((annotate("SIMCONNECT_REFSTRUCT")))
//#define SIMCONNECT_ENUM_FLAGS typedef __attribute__((flag_enum)) DWORD
#endif // MSVC

typedef DWORD SIMCONNECT_ENUM_FLAGS_VALUE;
#define SIMCONNECT_ENUM_FLAGS typedef SIMCONNECT_ENUM_FLAGS_VALUE
