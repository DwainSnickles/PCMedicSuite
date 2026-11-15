// ---------------------------------------------------------------------------------------------
// Ported from AppGeek (https://github.com/techygeekshome/AppGeek), Copyright (C) 2026
// TechyGeeksHome, and adapted for PC Medic.
//
// AppGeek is free software under the GNU General Public License v3.0, and PC Medic is too from
// this port onwards. See LICENSE.
// ---------------------------------------------------------------------------------------------

namespace PC_Medic;

/// <summary>Where an application is installed, as recorded by its uninstall registry key.</summary>
internal enum InstallScope { Machine, User, Store }

/// <summary>
/// Decides what install scope an operation must be pinned to, and explains the refusals
/// that decision causes.
///
/// This is the rule that prevents the most damaging thing the updater can do to a machine:
/// running elevated, winget defaults to the machine-wide installer, so pointing it at a per-user
/// app installs a second copy under Program Files and strands the original.
/// </summary>
internal static class InstallScopePolicy
{
    /// <summary>Exit code winget returns when no installer matches the constraints given.</summary>
    public const uint NoApplicableInstaller = 0x8A150011;

    /// <summary>
    /// The value to pass to winget's --scope for an app that is already installed, or null
    /// when the flag must not be sent at all.
    ///
    /// Null happens for three different reasons and they are not the same thing:
    ///   - the scope is unknown, so we have nothing to pin and the caller should warn;
    ///   - the app came from the Microsoft Store, which has no scope concept;
    ///   - the source is msstore, which rejects the flag outright.
    /// </summary>
    public static string? ScopeFlag(InstallScope? installedScope, string? source)
    {
        if (installedScope is null) return null;
        if (string.Equals(source, "msstore", StringComparison.OrdinalIgnoreCase)) return null;

        return installedScope switch
        {
            InstallScope.User => "user",
            InstallScope.Machine => "machine",
            _ => null   // InstallScope.Store - nothing to pin.
        };
    }

    /// <summary>
    /// True when this failure is the scope pin doing its job rather than something going
    /// wrong: the package simply has no installer at the scope the app is installed at.
    /// </summary>
    public static bool IsScopeRefusal(int exitCode, InstallScope? installedScope) =>
        unchecked((uint)exitCode) == NoApplicableInstaller &&
        installedScope is InstallScope.User or InstallScope.Machine;

    /// <summary>
    /// Plain English for a scope refusal. Returns null when the failure is not one.
    /// The wording matters: the user needs to understand PC Medic chose not to act, and why
    /// that was the safer choice.
    /// </summary>
    public static string? DescribeScopeRefusal(int exitCode, InstallScope? installedScope)
    {
        if (!IsScopeRefusal(exitCode, installedScope)) return null;

        return installedScope == InstallScope.User
            ? "Installed for your user only, and this package has no per-user installer. " +
              "Left alone on purpose - installing the machine-wide version would move the app " +
              "and break your existing shortcuts."
            : "Installed machine-wide, and this package has no machine-wide installer. " +
              "Left alone on purpose rather than installing a second per-user copy.";
    }

    /// <summary>Plain English for a scope, used in the activity log.</summary>
    public static string Describe(InstallScope? scope) => scope switch
    {
        InstallScope.User => "per-user",
        InstallScope.Machine => "machine-wide",
        InstallScope.Store => "Microsoft Store",
        _ => "unknown-scope"
    };
}
