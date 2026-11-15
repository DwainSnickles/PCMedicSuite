using System.Runtime.InteropServices;

namespace PC_Medic;

/// <summary>
/// Holds a Windows execution-state request so the machine will not sleep while long work
/// is running — winget installs, disk images, driver imports. Reference-counted: nested
/// Enter calls each need their own Leave, and the request is only released when the last
/// scope disposes. A manual toggle can hold the request open indefinitely.
/// </summary>
internal static class KeepAwake
{
    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;
    private const uint EsDisplayRequired = 0x00000002;

    private static readonly object Gate = new();
    private static int _depth;
    private static bool _manual;
    private static bool _display;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    /// <summary>True while anything (a run or the manual toggle) is holding the machine awake.</summary>
    public static bool IsActive
    {
        get { lock (Gate) return _depth > 0 || _manual; }
    }

    /// <summary>True only when the manual toggle (not a background run) holds the machine awake.</summary>
    public static bool ManualActive
    {
        get { lock (Gate) return _manual; }
    }

    /// <summary>Scoped hold for the duration of an operation. Dispose to release.</summary>
    public static IDisposable Enter(bool keepDisplayAwake = false)
    {
        lock (Gate)
        {
            _depth++;
            _display |= keepDisplayAwake;
            Apply();
        }
        return new Scope();
    }

    /// <summary>Manual hold, e.g. a "keep this PC awake" switch on the Sleep page.</summary>
    public static void SetManual(bool on, bool keepDisplayAwake = false)
    {
        lock (Gate)
        {
            _manual = on;
            if (on) _display |= keepDisplayAwake;
            Apply();
        }
    }

    private static void Apply()
    {
        if (!IsActive)
        {
            // ES_CONTINUOUS on its own clears every previous request — the hold is off.
            SetThreadExecutionState(EsContinuous);
            return;
        }

        SetThreadExecutionState(EsContinuous | EsSystemRequired |
                                (_display ? EsDisplayRequired : 0u));
    }

    private sealed class Scope : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            lock (Gate)
            {
                if (_depth > 0) _depth--;
                Apply();
            }
        }
    }
}
