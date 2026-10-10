#pragma once

#include <windows.h>
#include <string>

class SecureExperienceLauncher final
{
public:
    SecureExperienceLauncher() = default;
    ~SecureExperienceLauncher();

    HRESULT Start();
    void Stop();

private:
    static bool IsEnabled();
    static std::wstring ResolveExperiencePath();
    static std::wstring CurrentDesktopSpec();

    HANDLE _process{nullptr};
    DWORD _processId{0};
};
