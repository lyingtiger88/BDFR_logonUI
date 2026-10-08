#pragma once

#include <windows.h>
#include <unknwn.h>
#include <atomic>

class BDFRClassFactory final : public IClassFactory
{
public:
    BDFRClassFactory();

    IFACEMETHODIMP QueryInterface(REFIID riid, void** object) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;
    IFACEMETHODIMP CreateInstance(IUnknown* outer, REFIID riid, void** object) override;
    IFACEMETHODIMP LockServer(BOOL lock) override;

private:
    ~BDFRClassFactory() = default;
    std::atomic_ulong _refCount{1};
};
