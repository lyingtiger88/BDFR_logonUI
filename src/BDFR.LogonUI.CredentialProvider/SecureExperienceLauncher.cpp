#include "SecureExperienceLauncher.h"

#include <vector>
#include <strsafe.h>

namespace
{
void WriteLauncherLog(const std::wstring& message)
{
    wchar_t programData[MAX_PATH]{};
    const DWORD length = GetEnvironmentVariableW(
        L"ProgramData",
        programData,
        ARRAYSIZE(programData));

    if (length == 0 || length >= ARRAYSIZE(programData))
        return;

    std::wstring root = programData;
    root += L"\\BDFR\\LogonUI\\CredentialProvider";

    CreateDirectoryW((std::wstring(programData) + L"\\BDFR").c_str(), nullptr);
    CreateDirectoryW((std::wstring(programData) + L"\\BDFR\\LogonUI").c_str(), nullptr);
    CreateDirectoryW(root.c_str(), nullptr);

    std::wstring path = root + L"\\secure-experience-launch.log";

    HANDLE file = CreateFileW(
        path.c_str(),
        FILE_APPEND_DATA,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        nullptr,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        nullptr);

    if (file == INVALID_HANDLE_VALUE)
        return;

    SYSTEMTIME st{};
    GetLocalTime(&st);

    wchar_t prefix[96]{};
    StringCchPrintfW(
        prefix,
        ARRAYSIZE(prefix),
        L"%04u-%02u-%02u %02u:%02u:%02u ",
        st.wYear,
        st.wMonth,
        st.wDay,
        st.wHour,
        st.wMinute,
        st.wSecond);

    std::wstring line = prefix;
    line += message;
    line += L"\r\n";

    DWORD written = 0;
    WriteFile(
        file,
        line.data(),
        static_cast<DWORD>(line.size() * sizeof(wchar_t)),
        &written,
        nullptr);

    CloseHandle(file);
}

constexpr wchar_t kSettingsKey[] = L"SOFTWARE\\BDFR\\LogonUI";
constexpr wchar_t kEnabledValue[] = L"TrueLockEnabled";
constexpr wchar_t kPathValue[] = L"SecureExperiencePath";

bool ReadDwordValue(
    HKEY root,
    const wchar_t* subKey,
    const wchar_t* valueName,
    DWORD& value)
{
    DWORD type = 0;
    DWORD size = sizeof(value);
    const LSTATUS status = RegGetValueW(
        root,
        subKey,
        valueName,
        RRF_RT_REG_DWORD | RRF_SUBKEY_WOW6464KEY,
        &type,
        &value,
        &size);

    return status == ERROR_SUCCESS;
}

std::wstring ReadStringValue(
    HKEY root,
    const wchar_t* subKey,
    const wchar_t* valueName)
{
    DWORD size = 0;
    LSTATUS status = RegGetValueW(
        root,
        subKey,
        valueName,
        RRF_RT_REG_SZ | RRF_RT_REG_EXPAND_SZ | RRF_SUBKEY_WOW6464KEY,
        nullptr,
        nullptr,
        &size);

    if (status != ERROR_SUCCESS || size < sizeof(wchar_t))
        return {};

    std::vector<wchar_t> buffer(size / sizeof(wchar_t) + 1, L'\0');
    status = RegGetValueW(
        root,
        subKey,
        valueName,
        RRF_RT_REG_SZ | RRF_RT_REG_EXPAND_SZ | RRF_SUBKEY_WOW6464KEY,
        nullptr,
        buffer.data(),
        &size);

    if (status != ERROR_SUCCESS)
        return {};

    wchar_t expanded[32768]{};
    const DWORD expandedLength = ExpandEnvironmentStringsW(
        buffer.data(),
        expanded,
        ARRAYSIZE(expanded));

    if (expandedLength > 0 && expandedLength < ARRAYSIZE(expanded))
        return expanded;

    return buffer.data();
}

BOOL CALLBACK CloseProcessWindow(HWND hwnd, LPARAM parameter)
{
    const DWORD targetPid = static_cast<DWORD>(parameter);
    DWORD pid = 0;
    GetWindowThreadProcessId(hwnd, &pid);

    if (pid == targetPid)
        PostMessageW(hwnd, WM_CLOSE, 0, 0);

    return TRUE;
}

std::wstring ObjectName(HANDLE object)
{
    wchar_t buffer[256]{};
    DWORD needed = 0;

    if (!GetUserObjectInformationW(
            object,
            UOI_NAME,
            buffer,
            sizeof(buffer),
            &needed))
    {
        return {};
    }

    return buffer;
}
}

SecureExperienceLauncher::~SecureExperienceLauncher()
{
    Stop();
}

bool SecureExperienceLauncher::IsEnabled()
{
    DWORD enabled = 0;
    return ReadDwordValue(
        HKEY_LOCAL_MACHINE,
        kSettingsKey,
        kEnabledValue,
        enabled) &&
        enabled == 1;
}

std::wstring SecureExperienceLauncher::ResolveExperiencePath()
{
    auto configured = ReadStringValue(
        HKEY_LOCAL_MACHINE,
        kSettingsKey,
        kPathValue);

    if (!configured.empty())
        return configured;

    wchar_t programFiles[MAX_PATH]{};
    const DWORD length = GetEnvironmentVariableW(
        L"ProgramFiles",
        programFiles,
        ARRAYSIZE(programFiles));

    if (length == 0 || length >= ARRAYSIZE(programFiles))
        return {};

    std::wstring path = programFiles;
    path += L"\\BDFR\\LogonUI\\BDFR.LogonUI.Demo.exe";
    return path;
}

std::wstring SecureExperienceLauncher::CurrentDesktopSpec()
{
    const auto station = ObjectName(GetProcessWindowStation());
    const auto desktop = ObjectName(GetThreadDesktop(GetCurrentThreadId()));

    if (station.empty() || desktop.empty())
        return {};

    return station + L"\\" + desktop;
}

HRESULT SecureExperienceLauncher::Start()
{
    if (!IsEnabled())
    {
        WriteLauncherLog(L"Start skipped: TrueLockEnabled is not 1.");
        return S_FALSE;
    }

    if (_process)
    {
        if (WaitForSingleObject(_process, 0) == WAIT_TIMEOUT)
            return S_OK;

        CloseHandle(_process);
        _process = nullptr;
        _processId = 0;
    }

    const auto executable = ResolveExperiencePath();
    if (executable.empty() ||
        GetFileAttributesW(executable.c_str()) == INVALID_FILE_ATTRIBUTES)
    {
        WriteLauncherLog(L"Start failed: secure experience executable was not found.");
        return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    }

    std::wstring commandLine =
        L"\"" + executable + L"\" --secure-lock";
    std::vector<wchar_t> mutableCommand(
        commandLine.begin(),
        commandLine.end());
    mutableCommand.push_back(L'\0');

    auto desktopSpec = CurrentDesktopSpec();
    WriteLauncherLog(
        L"Attempting launch on desktop: " +
        (desktopSpec.empty() ? std::wstring(L"(default)") : desktopSpec) +
        L" | exe=" + executable);

    STARTUPINFOW startup{};
    startup.cb = sizeof(startup);
    startup.dwFlags = STARTF_USESHOWWINDOW;
    startup.wShowWindow = SW_SHOWMAXIMIZED;
    startup.lpDesktop = desktopSpec.empty()
        ? nullptr
        : desktopSpec.data();

    PROCESS_INFORMATION process{};

    const BOOL created = CreateProcessW(
        executable.c_str(),
        mutableCommand.data(),
        nullptr,
        nullptr,
        FALSE,
        CREATE_UNICODE_ENVIRONMENT | CREATE_NEW_PROCESS_GROUP,
        nullptr,
        nullptr,
        &startup,
        &process);

    if (!created)
    {
        const DWORD error = GetLastError();
        WriteLauncherLog(
            L"CreateProcessW failed. Win32=" +
            std::to_wstring(error));
        return HRESULT_FROM_WIN32(error);
    }

    CloseHandle(process.hThread);

    _process = process.hProcess;
    _processId = process.dwProcessId;
    WriteLauncherLog(
        L"Secure experience launched. PID=" +
        std::to_wstring(_processId));
    return S_OK;
}

void SecureExperienceLauncher::Stop()
{
    if (!_process)
        return;

    if (WaitForSingleObject(_process, 0) == WAIT_TIMEOUT)
    {
        const HDESK desktop = GetThreadDesktop(GetCurrentThreadId());
        if (desktop && _processId != 0)
        {
            EnumDesktopWindows(
                desktop,
                CloseProcessWindow,
                static_cast<LPARAM>(_processId));
        }

        if (WaitForSingleObject(_process, 1200) == WAIT_TIMEOUT)
            TerminateProcess(_process, 0);
    }

    CloseHandle(_process);
    _process = nullptr;
    _processId = 0;
}
