#include "ClassFactory.h"
#include "Provider.h"

#include <new>

extern volatile LONG g_moduleRefCount;

BDFRClassFactory::BDFRClassFactory()
{
    InterlockedIncrement(&g_moduleRefCount);
}

BDFRClassFactory::~BDFRClassFactory()
{
    InterlockedDecrement(&g_moduleRefCount);
}

HRESULT BDFRClassFactory::QueryInterface(const REFIID riid, void** object)
{
    if (!object)
        return E_POINTER;

    *object = nullptr;

    if (riid == IID_IUnknown || riid == IID_IClassFactory)
        *object = static_cast<IClassFactory*>(this);
    else
        return E_NOINTERFACE;

    AddRef();
    return S_OK;
}

ULONG BDFRClassFactory::AddRef()
{
    return ++_refCount;
}

ULONG BDFRClassFactory::Release()
{
    const ULONG remaining = --_refCount;
    if (!remaining)
        delete this;
    return remaining;
}

HRESULT BDFRClassFactory::CreateInstance(
    IUnknown* outer,
    const REFIID riid,
    void** object)
{
    if (!object)
        return E_POINTER;

    *object = nullptr;

    if (outer)
        return CLASS_E_NOAGGREGATION;

    auto* provider = new (std::nothrow) BDFRProvider();
    if (!provider)
        return E_OUTOFMEMORY;

    const HRESULT hr = provider->QueryInterface(riid, object);
    provider->Release();
    return hr;
}

HRESULT BDFRClassFactory::LockServer(const BOOL lock)
{
    if (lock)
        InterlockedIncrement(&g_moduleRefCount);
    else
        InterlockedDecrement(&g_moduleRefCount);

    return S_OK;
}
