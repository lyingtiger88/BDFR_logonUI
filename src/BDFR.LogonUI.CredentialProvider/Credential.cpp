#include "Credential.h"

#include <new>
#include <strsafe.h>

extern volatile LONG g_moduleRefCount;

BDFRCredential::BDFRCredential()
{
    InterlockedIncrement(&g_moduleRefCount);
}

BDFRCredential::~BDFRCredential()
{
    _secureExperience.Stop();

    if (_events)
    {
        _events->Release();
        _events = nullptr;
    }

    CoTaskMemFree(_userSid);
    CoTaskMemFree(_displayName);
    InterlockedDecrement(&g_moduleRefCount);
}

HRESULT BDFRCredential::Initialize(
    const CREDENTIAL_PROVIDER_USAGE_SCENARIO scenario,
    ICredentialProviderUser* user)
{
    if (!user)
        return E_INVALIDARG;

    _scenario = scenario;

    HRESULT hr = user->GetSid(&_userSid);
    if (FAILED(hr))
        return hr;

    hr = user->GetStringValue(PKEY_Identity_DisplayName, &_displayName);
    if (FAILED(hr))
    {
        CoTaskMemFree(_displayName);
        _displayName = nullptr;
        hr = SHStrDupW(L"Windows user", &_displayName);
    }

    return hr;
}

HRESULT BDFRCredential::QueryInterface(REFIID riid, void** object)
{
    if (!object)
        return E_POINTER;

    *object = nullptr;

    if (riid == IID_IUnknown || riid == IID_ICredentialProviderCredential)
        *object = static_cast<ICredentialProviderCredential*>(this);
    else if (riid == IID_ICredentialProviderCredential2)
        *object = static_cast<ICredentialProviderCredential2*>(this);
    else
        return E_NOINTERFACE;

    AddRef();
    return S_OK;
}

ULONG BDFRCredential::AddRef()
{
    return ++_refCount;
}

ULONG BDFRCredential::Release()
{
    const ULONG remaining = --_refCount;
    if (!remaining)
        delete this;
    return remaining;
}

HRESULT BDFRCredential::Advise(ICredentialProviderCredentialEvents* events)
{
    if (!events)
        return E_INVALIDARG;

    if (_events)
        _events->Release();

    _events = events;
    _events->AddRef();

    // Fail open: if the secure experience cannot launch, LogonUI remains usable.
    _secureExperience.Start();
    return S_OK;
}

HRESULT BDFRCredential::UnAdvise()
{
    _secureExperience.Stop();

    if (_events)
    {
        _events->Release();
        _events = nullptr;
    }

    return S_OK;
}

HRESULT BDFRCredential::SetSelected(BOOL* autoLogon)
{
    if (!autoLogon)
        return E_POINTER;

    *autoLogon = FALSE;
    _secureExperience.Start();
    return S_OK;
}

HRESULT BDFRCredential::SetDeselected()
{
    _secureExperience.Stop();
    return S_OK;
}

HRESULT BDFRCredential::GetFieldState(
    const DWORD fieldId,
    CREDENTIAL_PROVIDER_FIELD_STATE* state,
    CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE* interactive)
{
    if (!state || !interactive)
        return E_POINTER;

    if (fieldId >= BFI_NUM_FIELDS)
        return E_INVALIDARG;

    *state = BDFR_FIELD_STATES[fieldId].state;
    *interactive = BDFR_FIELD_STATES[fieldId].interactive;
    return S_OK;
}

HRESULT BDFRCredential::GetStringValue(const DWORD fieldId, PWSTR* value)
{
    if (!value)
        return E_POINTER;

    *value = nullptr;

    switch (fieldId)
    {
    case BFI_PROVIDER_NAME:
        return SHStrDupW(L"BDFR LogonUI", value);

    case BFI_STATUS_TEXT:
    {
        wchar_t buffer[384]{};
        const wchar_t* name = _displayName ? _displayName : L"Windows user";
        const HRESULT hr = StringCchPrintfW(
            buffer,
            ARRAYSIZE(buffer),
            L"%s\nBDFR Secure Lock bridge loaded. Windows remains the authentication authority.",
            name);

        return SUCCEEDED(hr) ? SHStrDupW(buffer, value) : hr;
    }

    case BFI_INFO_LINK:
        return SHStrDupW(
            L"Unlock uses Windows authentication • Esc reveals the Windows sign-in fallback",
            value);

    default:
        return E_INVALIDARG;
    }
}

HRESULT BDFRCredential::GetBitmapValue(DWORD, HBITMAP*)
{
    return E_NOTIMPL;
}

HRESULT BDFRCredential::GetCheckboxValue(DWORD, BOOL*, PWSTR*)
{
    return E_NOTIMPL;
}

HRESULT BDFRCredential::GetComboBoxValueCount(DWORD, DWORD*, DWORD*)
{
    return E_NOTIMPL;
}

HRESULT BDFRCredential::GetComboBoxValueAt(DWORD, DWORD, PWSTR*)
{
    return E_NOTIMPL;
}

HRESULT BDFRCredential::GetSubmitButtonValue(DWORD, DWORD*)
{
    return E_NOTIMPL;
}

HRESULT BDFRCredential::SetStringValue(DWORD, PCWSTR)
{
    return E_NOTIMPL;
}

HRESULT BDFRCredential::SetCheckboxValue(DWORD, BOOL)
{
    return E_NOTIMPL;
}

HRESULT BDFRCredential::SetComboBoxSelectedValue(DWORD, DWORD)
{
    return E_NOTIMPL;
}

HRESULT BDFRCredential::CommandLinkClicked(const DWORD fieldId)
{
    return fieldId == BFI_INFO_LINK ? S_OK : E_INVALIDARG;
}

HRESULT BDFRCredential::GetSerialization(
    CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* response,
    CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* serialization,
    PWSTR* optionalStatusText,
    CREDENTIAL_PROVIDER_STATUS_ICON* optionalStatusIcon)
{
    if (!response || !serialization || !optionalStatusText || !optionalStatusIcon)
        return E_POINTER;

    *response = CPGSR_NO_CREDENTIAL_NOT_FINISHED;
    ZeroMemory(serialization, sizeof(*serialization));
    *optionalStatusText = nullptr;
    *optionalStatusIcon = CPSI_NONE;

    return SHStrDupW(
        L"BDFR Secure Lock is visual-only at this stage. Continue with a built-in Windows sign-in option.",
        optionalStatusText);
}

HRESULT BDFRCredential::ReportResult(
    NTSTATUS,
    NTSTATUS,
    PWSTR* optionalStatusText,
    CREDENTIAL_PROVIDER_STATUS_ICON* optionalStatusIcon)
{
    if (!optionalStatusText || !optionalStatusIcon)
        return E_POINTER;

    *optionalStatusText = nullptr;
    *optionalStatusIcon = CPSI_NONE;
    return S_OK;
}

HRESULT BDFRCredential::GetUserSid(PWSTR* sid)
{
    if (!sid)
        return E_POINTER;

    *sid = nullptr;
    return _userSid ? SHStrDupW(_userSid, sid) : E_UNEXPECTED;
}
