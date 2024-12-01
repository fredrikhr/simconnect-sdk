#include <Windows.h>

typedef unsigned char SIMCONNECT_STRINGCHAR;
#define SIMCONNECT_STRING(name, size) SIMCONNECT_STRINGCHAR name[size]

#ifdef __clang__
#define SIMCONNECT_REFSTRUCT struct [[clang::annotate("SIMCONNECT_REFSTRUCT")]]
#define SIMCONNECT_ENUM_FLAGS [[clang::annotate("SIMCONNECT_ENUM_FLAGS")]] typedef DWORD
#else // !__clang__
#ifdef GNUC
#define SIMCONNECT_REFSTRUCT struct __attribute__((annotate("SIMCONNECT_REFSTRUCT")))
#define SIMCONNECT_ENUM_FLAGS __attribute__((annotate("SIMCONNECT_ENUM_FLAGS"))) typedef DWORD
#else // !GNUC
#define SIMCONNECT_REFSTRUCT struct
#define SIMCONNECT_ENUM_FLAGS typedef DWORD
#endif // !GNUC
#endif // !__clang__
