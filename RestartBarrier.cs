using System.Diagnostics;
using Microsoft.Win32;

namespace PC_Medic;

/// <summary>
/// Holds automatic restarts back for the length of a bulk run.
///
/// Two things restart a machine underneath an installer batch, and this covers both:
///
/// * A shutdown that something already scheduled - Windows Update's mandatory restart, a previous
///   package that called <c>shutdown /r /t</c>, a vendor helper. Windows only ever schedules those
///   with a delay, and <c>shutdown /a</c> cancels a pending one, so the barrier aborts any pending
///   shutdown every few seconds while the batch is running. Nothing is pending between installers,
///   which the abort reports as 1116 and is ignored.
///
/// * Windows Update's own automatic restart after installing updates. That is governed by the
///   documented <c>NoAutoRebootWithLoggedOnUsers</c> policy, so the barrier sets it for the run and
///   puts the previous value back when the batch finishes - including removing it again if it was
///   not there before. It is written only when PC Medic is elevated, because the key is machine-wide.
///
/// The barrier is deliberately not a driver or a service: it only acts on restarts that Windows or
/// an installer schedules through the normal shutdown path. An installer that turns the machine off
/// with no delay at all cannot be intercepted from user mode, and silent switches are chosen to
/// avoid that path in the first place.
/// </summary>
internal sealed class RestartBarrier : IDisposable
{
    private const string PolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU";
    private const string PolicyValue = "NoAutoRebootWithLoggedOnUsers";

    /// <summary>Often enough to catch a scheduled restart, cheap enough to leave running for hours.</summary>
    private static readonly TimeSpan Poll = TimeSpan.FromSeconds(3);

    private readonly System.Threading.Timer _timer;
    private readonly object? _previousPolicy;
    private readonly bool _policyHeld;
    private int _abortedRestarts;
    private bool _disposed;

    private RestartBarrier()
    {
        if (Elevation.IsElevated)
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.CreateSubKey(PolicyPath, writable: true);
                if (key is not null)
                {
                    _previousPolicy = key.GetValue(PolicyValue);
                    key.SetValue(PolicyValue, 1, RegistryValueKind.DWord);
                    _policyHeld = true;
                    Log.Info("Restart barrier: automatic restarts disabled for the length of the bulk run.");
                }
            }
            catch (Exception ex)
            {
                // Without the policy the abort loop still covers scheduled shutdowns, so carry on.
                Log.Warn("Restart barrier could not set the Windows Update policy: " + ex.Message);
            }
        }
        else
        {
            Log.Warn("Restart barrier: not elevated, so only scheduled restarts can be cancelled.");
        }

        _timer = new System.Threading.Timer(_ => AbortPendingShutdown(), null, Poll, Poll);
    }

    /// <summary>Starts holding restarts back. Dispose to release everything it changed.</summary>
    public static RestartBarrier Engage() => new();

    /// <summary>How many restarts the barrier actually caught mid-run.</summary>
    public int AbortedRestarts => Volatile.Read(ref _abortedRestarts);

    /// <summary>True when the Windows Update policy was successfully held for the run.</summary>
    public bool PolicyHeld => _policyHeld;

    private void AbortPendingShutdown()
    {
        if (_disposed) return;

        try
        {
            var start = new ProcessStartInfo("shutdown.exe", "/a")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using Process? process = Process.Start(start);
            if (process is null || !process.WaitForExit(4000)) return;

            // 1116 is ERROR_NO_SHUTDOWN_IN_PROGRESS: the normal answer between installers.
            if (process.ExitCode != 0) return;

            Interlocked.Increment(ref _abortedRestarts);
            Log.Warn("Restart barrier cancelled a restart that was scheduled while the batch was running.");
            ActivityLog.Warn("A restart was cancelled — the bulk install is still running.");
        }
        catch (Exception ex)
        {
            Log.Debug("Restart barrier abort failed: " + ex.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _timer.Dispose();

        if (!_policyHeld) return;

        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(PolicyPath, writable: true);
            if (key is not null)
            {
                if (_previousPolicy is null) key.DeleteValue(PolicyValue, throwOnMissingValue: false);
                else key.SetValue(PolicyValue, _previousPolicy);
            }

            Log.Info("Restart barrier released: the Windows Update policy is back to how it was.");
        }
        catch (Exception ex)
        {
            Log.Warn("Restart barrier could not restore the Windows Update policy: " + ex.Message);
        }
    }
}
