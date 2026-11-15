# PC Medic Suite — portable Windows maintenance console

[![Download the portable build](https://img.shields.io/badge/download-portable%20zip-2ea043?logo=windows&logoColor=white)](https://github.com/DwainSnickles/PCMedicSuite/releases/latest)

**PC Medic** is a portable Windows 10/11 maintenance suite for people who want the jobs
"Advanced SystemCare", CCleaner, Glary Utilities or Ashampoo WinOptimizer do — without a
background service, an installer, telemetry or a subscription. One folder, one executable,
every destructive action reversible through the Recycle Bin or a backup file.

Companion project: **[Disk Defrag](https://github.com/DwainSnickles/disk-defrag)** — referenced as a
project, hosted inside the suite's Maintenance page, and synced beside the exe on build.

## What it does

| Page | Purpose |
| --- | --- |
| **Overview** | Live CPU / memory / disk / uptime / update telemetry, GPU and machine profile, plus one-click **REDUCE CPU**, **FREE MEMORY**, **CLEAN DRIVE** and **OPEN UPDATES** actions and the autoscrolling activity feed. |
| **Startup & Boot** | Every Run-key, Startup-folder and scheduled-task autostart entry with reversible disabling, plus boot metrics and Fast Startup / boot-menu tuning with a JSON backup. |
| **Optimize Sleep** | Sleep/hibernate and power-plan tuning through `powercfg`, with a restorable backup. |
| **Maintenance** | Sixteen recovery and cleanup tools in one grid (SFC, DISM, restore point, Storage Sense, Memory Diagnostic, Event Viewer, **Disk Defrag**, and more), each with measured progress. |
| **Backup & Restore** | wimlib whole-drive `.wim`/`.esd` capture and apply, bare-metal restore with `bcdboot` boot repair, component-store DISM health checks, and a **bootable disk** builder that formats a USB stick with WinRE and writes a real BCD store — without touching firmware boot order. |
| **Tweaks** | Explorer and visual-effects tweaks, each written with a backup so it can be reverted. |
| **Software** | Full installed-software inventory from both registry hives and the store, security-flagged `winget` update candidates with pins, ignore rules, sequential batch runs and updater settings. |
| **Catalogue** | A curated list of common Windows software — tick what you want and install it silently. |
| **Updates** | `winget` application upgrades, Windows Update driver scans, an **OFFLINE** action that installs driver packages from any folder and can export this machine's driver store, hidden-update management, and a shared badge counting what is waiting. |
| **Registry Audit** | One page, two audits: dead `App Paths`, orphaned uninstall keys and stale file associations on the left, ~25,000 shortcuts resolved with `IShellLinkW` on the right. Removable per item, with a `.reg` backup and a persistent "not dead" list. |
| **Benchmarks** | Quick CPU and disk sanity runs; a missing benchmark rig installs from the page itself. |
| **Command Shell** | An embedded console for the tools the UI does not wrap. |

## Why it is different

- **Portable and honest** — `asInvoker`, no service, no drivers, no scheduled tasks of its own.
  Elevation is opt-in per action and the UI always shows whether you are a standard user.
- **Reversible** — deleted files go to the Recycle Bin, registry edits write a backup first,
  and the shortcut keep-list and CPU-ceiling backup persist in `%LOCALAPPDATA%\PCMedic`.
- **No fake numbers** — every figure comes from the OS: `GlobalMemoryStatusEx`,
  `FSCTL_GET_VOLUME_BITMAP`, `powercfg`, `winget`, performance counters.

## Loading and progress

Long operations never leave the page blank. They cover their results area with a centred
**LOADING ##%** plate: the shortcut audit, the registry audit and both update scans use it, as do
temp cleanup, the memory trim and the self-update check.

Where the work reports progress, that number is real — the audits know how many links or keys they
still have to resolve and count up by that share. Where the work is one opaque call (`winget`, the
Windows Update agent, `powercfg`, a temp-file sweep), nothing can be measured, so the bar eases
toward a ceiling and resolves only when the call returns. It never reaches 100% before the work is
done, and the plate hides itself again the moment the results are ready.

Every wait also raises a `WORKING` strip along the bottom of the shell, with a segment sweeping its
track, so the window is never frozen and silent even when only one card is busy. Downloads do not
need either: `winget` streams its transfer, so each update row and each benchmark rig shows its own
bar, filled from the real percentage or sweeping when winget reports none.

## Requirements

- Windows 10 or 11 (x64)
- .NET 10 desktop runtime, or build from source with the .NET 10 SDK
- Administrator rights only for the actions that genuinely need them

## Build

The build needs the **[Disk Defrag](https://github.com/DwainSnickles/disk-defrag)** companion
cloned beside this repo (or pass `-p:DiskDefragProject=<path>`):

```sh
git clone https://github.com/DwainSnickles/disk-defrag
cd PCMedicSuite
dotnet build PCMedicSuite.csproj
```

The portable layout lands in `bin/Debug/net10.0-windows/`, with `DiskDefrag.exe` synced
beside the suite automatically. Every form and page splits into a `.Designer.cs` control
definition and code-behind behaviour, so the whole suite opens in the Visual Studio
Windows Forms designer.

## SEO / search terms

This project is the practical answer to searches such as:

`Advanced SystemCare alternative` · `IObit Advanced SystemCare free alternative` ·
`CCleaner alternative portable` · `Glary Utilities alternative` ·
`Ashampoo WinOptimizer alternative` · `Wise Care 365 alternative` ·
`free PC cleaner Windows 11` · `portable system optimizer` · `startup program manager` ·
`registry cleaner open source` · `disk cleanup tool` · `Windows maintenance utility`

## Repository topics

`windows` `system-optimizer` `pc-cleaner` `pc-optimizer` `maintenance` `registry-cleaner`
`startup-manager` `disk-cleanup` `disk-defrag` `winforms` `dotnet` `windows-11` `portable`
`advanced-systemcare-alternative` `ccleaner-alternative` `freebuff`

---

Built with **[Freebuff](https://freebuff.com)** — the AI coding agent that builds and
maintains this suite.

## Licence

PC Medic Suite is free software under the **GNU General Public License v3.0** - see [LICENSE](LICENSE).

Its application-update engine is ported from [AppGeek](https://github.com/techygeekshome/AppGeek),
Copyright (C) 2026 TechyGeeksHome, which is released under the same licence. The ported files
carry a header saying so. Driver updates through the Windows Update agent are PC Medic's own.

The PnP device audit and the offline driver repository are ported from
[Hexnest](https://github.com/ahmetcaglayan/Hexnest), Copyright (c) 2026 Ahmet Çağlayan and Hexnest
contributors, which is released under the MIT licence; that notice travels with the ported code in
`DriverDevices.cs` and `DriverRepository.cs`.
