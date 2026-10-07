using System.Runtime.InteropServices;

namespace BDFR.LogonUI.Demo;

public sealed record SystemTelemetrySnapshot(
    double CpuPercent,
    double MemoryPercent,
    double? BatteryPercent,
    bool IsOnAcPower,
    ulong AvailableMemoryMb,
    ulong TotalMemoryMb);

public sealed class SystemTelemetryService
{
    private ulong _lastIdle;
    private ulong _lastKernel;
    private ulong _lastUser;
    private bool _hasCpuSample;

    public SystemTelemetrySnapshot Read()
    {
        var cpu = ReadCpu();
        var (memoryPercent, availableMb, totalMb) = ReadMemory();
        var (batteryPercent, isOnAc) = ReadPower();

        return new SystemTelemetrySnapshot(
            cpu,
            memoryPercent,
            batteryPercent,
            isOnAc,
            availableMb,
            totalMb);
    }

    private double ReadCpu()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
            return 0;

        var idle = idleTime.ToUInt64();
        var kernel = kernelTime.ToUInt64();
        var user = userTime.ToUInt64();

        if (!_hasCpuSample)
        {
            _lastIdle = idle;
            _lastKernel = kernel;
            _lastUser = user;
            _hasCpuSample = true;
            return 0;
        }

        var idleDelta = idle - _lastIdle;
        var kernelDelta = kernel - _lastKernel;
        var userDelta = user - _lastUser;

        _lastIdle = idle;
        _lastKernel = kernel;
        _lastUser = user;

        var total = kernelDelta + userDelta;
        if (total == 0)
            return 0;

        var busy = total > idleDelta ? total - idleDelta : 0;
        return Math.Clamp(busy * 100d / total, 0, 100);
    }

    private static (double Load, ulong AvailableMb, ulong TotalMb) ReadMemory()
    {
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref status))
            return (0, 0, 0);

        const ulong mb = 1024UL * 1024UL;
        return (
            Math.Clamp(status.MemoryLoad, 0, 100),
            status.AvailablePhysical / mb,
            status.TotalPhysical / mb);
    }

    private static (double? BatteryPercent, bool IsOnAc) ReadPower()
    {
        if (!GetSystemPowerStatus(out var status))
            return (null, false);

        double? battery = status.BatteryLifePercent == byte.MaxValue
            ? null
            : Math.Clamp((double)status.BatteryLifePercent, 0, 100);

        return (battery, status.AcLineStatus == 1);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeFileTime
    {
        public uint Low;
        public uint High;

        public readonly ulong ToUInt64() => ((ulong)High << 32) | Low;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out NativeFileTime idleTime,
        out NativeFileTime kernelTime,
        out NativeFileTime userTime);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus systemPowerStatus);
}
