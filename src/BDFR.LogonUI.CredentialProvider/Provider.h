#pragma once

#include <windows.h>
#include <credentialprovider.h>
#include <atomic>

#include "Credential.h"
#include "SecureExperienceLauncher.h"

class BDFRProvider final :
    public ICredentialProvider,
    public ICredentialProviderSetUserArray
{
public:
    BDFRProvider();

    IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;

    IFACEMETHODIMP SetUsageScenario(CREDENTIAL_PROVIDER_USAGE_SCENARIO scenario, DWORD flags) override;
    IFACEMETHODIMP SetSerialization(const CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* serialization) override;
    IFACEMETHODIMP Advise(ICredentialProviderEvents* events, UINT_PTR adviseContext) override;
    IFACEMETHODIMP UnAdvise() override;
    IFACEMETHODIMP GetFieldDescriptorCount(DWORD* count) override;
    IFACEMETHODIMP GetFieldDescriptorAt(
        DWORD index,
        CREDENTIAL_PROVIDER_FIELD_DESCRIPTOR** descriptor) override;
    IFACEMETHODIMP GetCredentialCount(
        DWORD* count,
        DWORD* defaultCredential,
        BOOL* autoLogonWithDefault) override;
    IFACEMETHODIMP GetCredentialAt(
        DWORD index,
        ICredentialProviderCredential** credential) override;

    IFACEMETHODIMP SetUserArray(ICredentialProviderUserArray* users) override;

private:
    ~BDFRProvider();

    HRESULT EnsureCredential();
    void ReleaseCredential();

    std::atomic_ulong _refCount{1};
    CREDENTIAL_PROVIDER_USAGE_SCENARIO _scenario{CPUS_INVALID};
    ICredentialProviderUserArray* _users{nullptr};
    BDFRCredential* _credential{nullptr};
    SecureExperienceLauncher _secureExperience;
};
