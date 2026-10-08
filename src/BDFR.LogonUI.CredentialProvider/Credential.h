#pragma once

#include <windows.h>
#include <credentialprovider.h>
#include <propkey.h>
#include <shlwapi.h>
#include <atomic>

#include "FieldDefinitions.h"

class BDFRCredential final : public ICredentialProviderCredential2
{
public:
    BDFRCredential();

    HRESULT Initialize(
        CREDENTIAL_PROVIDER_USAGE_SCENARIO scenario,
        ICredentialProviderUser* user);

    IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;

    IFACEMETHODIMP Advise(ICredentialProviderCredentialEvents* events) override;
    IFACEMETHODIMP UnAdvise() override;
    IFACEMETHODIMP SetSelected(BOOL* autoLogon) override;
    IFACEMETHODIMP SetDeselected() override;
    IFACEMETHODIMP GetFieldState(
        DWORD fieldId,
        CREDENTIAL_PROVIDER_FIELD_STATE* state,
        CREDENTIAL_PROVIDER_FIELD_INTERACTIVE_STATE* interactive) override;
    IFACEMETHODIMP GetStringValue(DWORD fieldId, PWSTR* value) override;
    IFACEMETHODIMP GetBitmapValue(DWORD fieldId, HBITMAP* bitmap) override;
    IFACEMETHODIMP GetCheckboxValue(DWORD fieldId, BOOL* checked, PWSTR* label) override;
    IFACEMETHODIMP GetComboBoxValueCount(DWORD fieldId, DWORD* count, DWORD* selectedItem) override;
    IFACEMETHODIMP GetComboBoxValueAt(DWORD fieldId, DWORD item, PWSTR* itemText) override;
    IFACEMETHODIMP GetSubmitButtonValue(DWORD fieldId, DWORD* adjacentTo) override;
    IFACEMETHODIMP SetStringValue(DWORD fieldId, PCWSTR value) override;
    IFACEMETHODIMP SetCheckboxValue(DWORD fieldId, BOOL checked) override;
    IFACEMETHODIMP SetComboBoxSelectedValue(DWORD fieldId, DWORD selectedItem) override;
    IFACEMETHODIMP CommandLinkClicked(DWORD fieldId) override;
    IFACEMETHODIMP GetSerialization(
        CREDENTIAL_PROVIDER_GET_SERIALIZATION_RESPONSE* response,
        CREDENTIAL_PROVIDER_CREDENTIAL_SERIALIZATION* serialization,
        PWSTR* optionalStatusText,
        CREDENTIAL_PROVIDER_STATUS_ICON* optionalStatusIcon) override;
    IFACEMETHODIMP ReportResult(
        NTSTATUS status,
        NTSTATUS substatus,
        PWSTR* optionalStatusText,
        CREDENTIAL_PROVIDER_STATUS_ICON* optionalStatusIcon) override;

    IFACEMETHODIMP GetUserSid(PWSTR* sid) override;

private:
    ~BDFRCredential();

    std::atomic_ulong _refCount{1};
    CREDENTIAL_PROVIDER_USAGE_SCENARIO _scenario{CPUS_INVALID};
    ICredentialProviderCredentialEvents* _events{nullptr};
    PWSTR _userSid{nullptr};
    PWSTR _displayName{nullptr};
};
