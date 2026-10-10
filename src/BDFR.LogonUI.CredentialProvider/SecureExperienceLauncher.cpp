#include "SecureExperienceLauncher.h"

#include <vector>

namespace
{
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
        return S_FALSE;

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
        return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    }

    std::wstring commandLine =
        L"\"" + executable + L"\" --secure-lock";
    std::vector<wchar_t> mutableCommand(
        commandLine.begin(),
        commandLine.end());
    mutableCommand.push_back(L'\0');

    auto desktopSpec = CurrentDesktopSpec();

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
        return HRESULT_FROM_WIN32(GetLastError());

    CloseHandle(process.hThread);

    _process = process.hProcess;
    _processId = process.dwProcessId;
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
