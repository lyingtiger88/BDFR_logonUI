#include <windows.h>

#include "ClassFactory.h"
#include "ProviderGuids.h"

long g_moduleRefCount = 0;

BOOL APIENTRY DllMain(HMODULE, DWORD, LPVOID)
{
    return TRUE;
}

STDAPI DllCanUnloadNow()
{
    return g_moduleRefCount == 0 ? S_OK : S_FALSE;
}

STDAPI DllGetClassObject(
    const REFCLSID clsid,
    const REFIID riid,
    void** object)
{
    if (!object)
        return E_POINTER;

    *object = nullptr;

    if (clsid != CLSID_BDFRLogonUICredentialProvider)
        return CLASS_E_CLASSNOTAVAILABLE;

    auto* factory = new (std::nothrow) BDFRClassFactory();
    if (!factory)
        return E_OUTOFMEMORY;

    const HRESULT hr = factory->QueryInterface(riid, object);
    factory->Release();
    return hr;
}
