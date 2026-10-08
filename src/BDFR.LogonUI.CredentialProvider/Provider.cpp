#include "Provider.h"
#include "FieldDefinitions.h"

#include <shlwapi.h>
#include <new>\n\nextern long g_moduleRefCount;

BDFRProvider::BDFRProvider()\n{\n    InterlockedIncrement(&g_moduleRefCount);\n}

BDFRProvider::~BDFRProvider()
{
    ReleaseCredential();

    if (_users)
    {
        _users->Release();
        _users = nullptr;
    }
}

HRESULT BDFRProvider::QueryInterface(const REFIID riid, void** object)
{
    if (!object)
        return E_POINTER;

    *object = nullptr;

    if (riid == IID_IUnknown || riid == IID_ICredentialProvider)
        *object = static_cast<ICredentialProvider*>(this);
    else if (riid == IID_ICredentialProviderSetUserArray)
        *object = static_cast<ICredentialProviderSetUserArray*>(this);
    else
        return E_NOINTERFACE;

    AddRef();
    return S_OK;
}

ULONG BDFRProvider::AddRef()
{
    return ++_refCount;
}

ULONG BDFRProvider::Release()
{
    const ULONG remaining = --_refCount;
    if (!remaining)
        delete this;
    return remaining;
}

HRESULT BDFRProvider::SetUsageScenario(
    const CREDENTIAL_PROVIDER_USAGE_SCENARIO scenario,
    DWORD)
{
    switch (scenario)
    {
    case CPUS_LOGON:
    case CPUS_UNLOCK_WORKSTATION:
        _scenario = scenario;
        ReleaseCredential();
        return S_OK;

    case CPUS_CHANGE_PASSWORD:
    case CPUS_CREDUI:
        return E_NOTIMPL;

    default:
        return E_INVALIDARG;
    }
}

HRESULT BDFRProvider::SetSerialization(
    const CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION*)
{
    return E_NOTIMPL;
}

HRESULT BDFRProvider::Advise(ICredentialProviderEvents*, UINT_PTR)
{
    return S_OK;
}

HRESULT BDFRProvider::UnAdvise()
{
    return S_OK;
}

HRESULT BDFRProvider::GetFieldDescriptorCount(DWORD* count)
{
    if (!count)
        return E_POINTER;

    *count = BFI_NUM_FIELDS;
    return S_OK;
}

HRESULT BDFRProvider::GetFieldDescriptorAt(
    const DWORD index,
    CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** descriptor)
{
    if (!descriptor)
        return E_POINTER;

    *descriptor = nullptr;

    if (index >= BFI_NUM_FIELDS)
        return E_INVALIDARG;

    auto* copy = static_cast<CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR*>(
        CoTaskMemAlloc(sizeof(CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR)));

    if (!copy)
        return E_OUTOFMEMORY;

    *copy = BDFR_FIELDS[index];
    copy->pszLabel = nullptr;

    const HRESULT hr = BDFR_FIELDS[index].pszLabel
        ? SHStrDupW(BDFR_FIELDS[index].pszLabel, &copy->pszLabel)
        : S_OK;

    if (FAILED(hr))
    {
        CoTaskMemFree(copy);
        return hr;
    }

    *descriptor = copy;
    return S_OK;
}

HRESULT BDFRProvider::GetCredentialCount(
    DWORD* count,
    DWORD* defaultCredential,
    BOOL* autoLogonWithDefault)
{
    if (!count || !defaultCredential || !autoLogonWithDefault)
        return E_POINTER;

    *count = 0;
    *defaultCredential = CREDENTIAL_PROVIDER_NO_DEFAULT;
    *autoLogonWithDefault = FALSE;

    const HRESULT hr = EnsureCredential();
    if (SUCCEEDED(hr) && _credential)
        *count = 1;

    return SUCCEEDED(hr) ? S_OK : hr;
}

HRESULT BDFRProvider::GetCredentialAt(
    const DWORD index,
    ICredentialProviderCredential** credential)
{
    if (!credential)
        return E_POINTER;

    *credential = nullptr;

    if (index != 0)
        return E_INVALIDARG;

    const HRESULT hr = EnsureCredential();
    if (FAILED(hr) || !_credential)
        return FAILED(hr) ? hr : E_UNEXPECTED;

    return _credential->QueryInterface(IID_PPV_ARGS(credential));
}

HRESULT BDFRProvider::SetUserArray(ICredentialProviderUserArray* users)
{
    if (!users)
        return E_INVALIDARG;

    if (_users)
        _users->Release();

    _users = users;
    _users->AddRef();

    ReleaseCredential();
    return S_OK;
}

HRESULT BDFRProvider::EnsureCredential()
{
    if (_credential)
        return S_OK;

    if (!_users)
        return S_FALSE;

    DWORD userCount = 0;
    HRESULT hr = _users->GetCount(&userCount);
    if (FAILED(hr) || userCount == 0)
        return FAILED(hr) ? hr : S_FALSE;

    ICredentialProviderUser* user = nullptr;
    hr = _users->GetAt(0, &user);
    if (FAILED(hr))
        return hr;

    auto* credential = new (std::nothrow) BDFRCredential();
    if (!credential)
    {
        user->Release();
        return E_OUTOFMEMORY;
    }

    hr = credential->Initialize(_scenario, user);
    user->Release();

    if (FAILED(hr))
    {
        credential->Release();
        return hr;
    }

    _credential = credential;
    return S_OK;
}

void BDFRProvider::ReleaseCredential()
{
    if (_credential)
    {
        _credential->Release();
        _credential = nullptr;
    }
}
