using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FileForge.Platform.Windows.Memory;

public static class MemoryOptimizer
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetProcessWorkingSetSize(nint processHandle, nint minimumWorkingSetSize, nint maximumWorkingSetSize);

    /// <summary>
    /// Forces Garbage Collection and trims the process working set memory down to the bare minimum.
    /// Call when the application is minimized, idle, or after completing a large batch conversion.
    /// </summary>
    public static void TrimWorkingSet()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);

            var handle = Process.GetCurrentProcess().Handle;
            SetProcessWorkingSetSize(handle, -1, -1);
        }
        catch
        {
            // Best effort memory trimming
        }
    }

    /// <summary>
    /// Gets the current working set memory of the process in Megabytes.
    /// </summary>
    public static double GetCurrentMemoryMb()
    {
        Process.GetCurrentProcess().Refresh();
        return Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0);
    }
}
