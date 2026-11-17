namespace PC_Medic;

/// <summary>One titled block inside a help topic.</summary>
internal sealed record HelpSection(string Heading, string Body);

/// <summary>Everything the help window knows about one feature.</summary>
internal sealed record HelpTopic(
    string Key,
    string Title,
    string Group,
    string Summary,
    HelpSection[] Sections,
    string[]? Tips = null);

/// <summary>
/// The help manual, held next to the code it describes so a page and its documentation move together.
///
/// Every page and every dialog that does real work has a topic here; the "?" in the header opens the
/// topic for whatever page is on screen, and a dialog's own "?" opens its own. Text is plain and
/// deliberately specific about what is read, what is written, and what needs administrator rights.
/// </summary>
internal static class HelpContent
{
    public static string ImageResource(string key) => $"PC_Medic.Assets.help.{key}.png";

    public static HelpTopic? Find(string? key) => key is null
        ? null
        : Topics.FirstOrDefault(topic => topic.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public static readonly HelpTopic[] Topics =
    {
        new(
            "overview", "Overview", "PAGES",
            "The dashboard: live CPU, memory, disk, pending updates and uptime, the machine profile, and the activity feed.",
            new[]
            {
                new HelpSection("WHAT IT SHOWS",
                    "Four live tiles sample once a second: CPU load from the kernel and user counters, memory through " +
                    "GlobalMemoryStatusEx, the system drive's free space from the volume itself, and the number of pending " +
                    "software and driver updates shared with the Updates page. Nothing here is estimated — if a figure cannot " +
                    "be read from the OS it is left blank rather than guessed."),
                new HelpSection("THE ACTION TILES",
                    "REDUCE CPU stops the processes using the most processor time after listing them for confirmation. " +
                    "FREE MEMORY trims working sets of idle processes; it never touches a process that owns a window you are " +
                    "using. CLEAN DRIVE opens the temp cleanup, and OPEN UPDATES jumps straight to the Updates page."),
                new HelpSection("MACHINE PROFILE AND FEED",
                    "The profile card names the CPU, GPU, board, memory and Windows build. The activity feed on the right is " +
                    "the app's own log: every scan, install, tweak and failure is written there with a timestamp, and the same " +
                    "lines go to the daily log file in %APPDATA%\\PCMedicSuite\\Logs."),
                new HelpSection("WHEN IT IS USEFUL",
                    "Leave it open while working on a slow machine: the tiles show whether a cleanup actually helped, and the " +
                    "update badge tells you something is waiting before you open the Updates page."),
            },
            new[]
            {
                "Nothing on this page changes anything until you press one of its four actions.",
                "The tray icon keeps sampling while the window is parked, so its tooltip stays current.",
            }),

        new(
            "startup", "Startup & Boot", "PAGES",
            "Everything that starts with Windows — Run keys, Startup folders and auto-start services — with reversible disabling, plus boot metrics.",
            new[]
            {
                new HelpSection("WHAT IT LISTS",
                    "Per-user and machine-wide Run and RunOnce values, both Startup folders, and services whose start type is " +
                    "Automatic. Each row shows the command's target, whether the file still exists, and an estimated impact " +
                    "based on what it launches. A missing target is shown as dead rather than hidden."),
                new HelpSection("DISABLING SAFELY",
                    "Switching a row off moves the value into PC Medic's own store, or changes a service to Manual — the " +
                    "original entry is kept, so switching it back restores exactly what was there. The boot menu and Fast " +
                    "Startup settings are written only after a JSON backup of the previous values is saved."),
                new HelpSection("RESTART OPTIONS",
                    "Two buttons beside Fast Startup change how Windows starts. SAFE MODE sets the boot entry to " +
                    "safe mode - minimal drivers and services - and restarts, for a machine that will not start " +
                    "properly otherwise. That flag stays set until it is cleared, so the button becomes CLEAR SAFE " +
                    "while it is armed and the state line says what the next start will be. REPAIR MODE restarts into " +
                    "Windows Recovery (Advanced startup), where Startup Repair, System Restore, Reset this PC and " +
                    "Command Prompt live. Both write the boot configuration, so both need an elevated run, and both " +
                    "ask before restarting because unsaved work goes with the restart."),
                new HelpSection("BOOT METRICS",
                    "The card at the bottom reads the last boot, shutdown and Fast Startup state from the event log and the " +
                    "registry. It is read-only: a slow boot is reported with its biggest contributors rather than 'fixed' " +
                    "automatically."),
                new HelpSection("GOOD CANDIDATES TO TURN OFF",
                    "Updaters that start a second copy of themselves, launchers for software you open manually, and game " +
                    "clients. Leave security software, audio drivers and input-device helpers alone."),
            },
            new[]
            {
                "Machine-wide entries need an elevated run; per-user entries do not.",
                "Anything disabled here can be switched back on from the same row.",
            }),

        new(
            "sleep", "Optimize Sleep", "PAGES",
            "The idle ladder: display, sleep, hibernate and shut-down timers, wake timers, USB suspend and Hybrid Sleep, applied to the active power plan.",
            new[]
            {
                new HelpSection("THE IDLE LADDER",
                    "Four rows set the powercfg timeouts for the active plan: turn off display, sleep after, hibernate after and " +
                    "turn off disk. NEVER is a real value (0, meaning the timer is off), not a missing reading."),
                new HelpSection("POWER BEHAVIOUR",
                    "ALLOW WAKE TIMERS lets scheduled tasks wake the machine; USB SELECTIVE SUSPEND powers down idle USB devices; " +
                    "HYBRID SLEEP writes memory to disk before sleeping; KEEP AWAKE holds an execution-state request so nothing " +
                    "sleeps while it is on — useful during a long backup or install."),
                new HelpSection("OPTIMIZE SLEEP AND RESTORE PREVIOUS",
                    "OPTIMIZE SLEEP applies a measured ladder (display 10 minutes, sleep 30, hibernate 2 hours), disables wake " +
                    "timers and keeps USB devices powered on AC. RESTORE PREVIOUS puts back the values saved before the last " +
                    "optimisation, so the change is reversible."),
                new HelpSection("REQUIREMENTS",
                    "Changing the active plan's timeouts needs administrator rights on most machines. If hibernate is disabled " +
                    "system-wide, the hibernate row reports that instead of pretending to set a timer."),
            }),

        new(
            "maintenance", "Maintenance", "PAGES",
            "Fifteen recovery and cleanup tools in one grid: SFC, DISM, restore points, Storage Sense, Memory Diagnostic, Event Viewer, Disk Cleanup and Disk Defrag.",
            new[]
            {
                new HelpSection("HOW THE GRID WORKS",
                    "Each tile launches one tool. Repair tools that write to the component store (SFC, DISM) show their output " +
                    "in the activity feed as they run; utilities with their own window (Event Viewer, Memory Diagnostic, Disk " +
                    "Cleanup) open normally."),
                new HelpSection("THE REPAIR TOOLS",
                    "SFC /scannow verifies protected system files. DISM /RestoreHealth repairs the component store that SFC " +
                    "reads from. Run DISM first when SFC reports that it cannot repair a file. Both need administrator rights " +
                    "and can take a long time; the work is shown, not hidden."),
                new HelpSection("RESTORE POINTS",
                    "Create a restore point before a batch of tweaks or driver work. System Protection must be on for the " +
                    "volume; if it is not, PC Medic says so rather than failing silently."),
                new HelpSection("DISK DEFRAG",
                    "The Defrag tile opens the companion tool in its own page. It reads the volume's cluster map " +
                    "through FSCTL_GET_VOLUME_BITMAP, so the map shows where files actually sit rather than an animation."),
            },
            new[]
            {
                "Most tiles need elevation; a standard run opens them read-only or reports the requirement.",
                "Nothing in the grid runs on its own — each tile is one deliberate click.",
            }),

        new(
            "defrag", "Disk Defrag", "PAGES",
            "The companion defragmentation tool hosted inside PC Medic: a real cluster map, a chosen strategy, and a stop that finishes the current pass cleanly.",
            new[]
            {
                new HelpSection("THE MAP",
                    "Each block is a cluster run coloured by what occupies it — free, in use, fragmented or immovable. The map " +
                    "is read from the volume bitmap, so it reflects the disk rather than a drawing of one."),
                new HelpSection("STRATEGIES",
                    "Consolidate groups free space, Defragment joins split files, and Quick only moves what is cheap to move. " +
                    "The pass reports how many files it moved and how long it took."),
                new HelpSection("STOPPING",
                    "STOP finishes the file in flight and then stops; it does not kill the engine mid-write. Closing PC Medic " +
                    "while a defrag runs asks first, because closing stops it."),
                new HelpSection("WHEN NOT TO RUN IT",
                    "On an SSD. Windows already trims SSDs, and moving files around gains nothing — use the storage tools " +
                    "instead. Defrag is for spinning disks and heavily fragmented volumes."),
                new HelpSection("ELEVATION",
                    "Reading the cluster map needs the volume opened for direct access, which only an elevated run can do. " +
                    "Unelevated, the map reports that instead of drawing an empty disk."),
            }),

        new(
            "backup", "Backup & Restore", "PAGES",
            "WIM/E SD disk images through wimlib: capture a volume, apply an image to another disk, repair boot files, and check the component store.",
            new[]
            {
                new HelpSection("DISK IMAGE BACKUP",
                    "SOURCE cycles through the disks PC Medic can read, including USB and external disks. The destination is a " +
                    ".wim (or .esd for tighter compression) written from the chosen volume. COMPRESSION chooses FAST, MAXIMUM " +
                    "(LZMS solid) or NONE; FAST is a good default for a system volume."),
                new HelpSection("BACK UP TO DISK…",
                    "The guided path, and the one to use for a real backup: pick the source volume, pick a destination disk " +
                    "that is actually big enough, and optionally let a small USB stick carry a pointer to the image. The wizard " +
                    "shows the estimated image size against the destination's free space before anything starts."),
                new HelpSection("THE RECOVERY POINTER",
                    "A full Windows image rarely fits on a 16 GB stick, so the stick does not need to hold it. With a stick " +
                    "selected, PC Medic writes PCMedicRecovery\\backup-location.json and RESTORE-README.txt to it: the image path, " +
                    "its size, the source volume and the restore steps. Boot that stick with the backup disk attached, open PC " +
                    "Medic, and the image path is filled in for you."),
                new HelpSection("IMAGE RESTORE",
                    "BROWSE to a .wim/.esd on any attached disk; PC Medic lists its images with wimlib-imagex info. Pick the " +
                    "image, pick the target disk, then RESTORE IMAGE. Everything on the target is overwritten, and boot files " +
                    "are written afterwards with bcdboot. A restore needs administrator rights."),
                new HelpSection("THE STICK IS READ, NOT TYPED",
                    "Every refresh looks for PCMedicRecovery\\backup-location.json on the attached fixed and removable drives. " +
                    "When one is found, its image path is filled into IMAGE RESTORE and that image's list is read, so a bare-metal " +
                    "restore is a target pick and one button. The card says so when the named disk is not attached, and STICK " +
                    "re-reads the pointer, or cycles between several sticks. A BROWSE choice always wins over the stick."),
                new HelpSection("KEEP AWAKE",
                    "Every capture, apply, boot repair and health check holds the machine awake, screen included, for as long " +
                    "as it runs. Sleep cannot interrupt an image halfway through; an explicit restart from you still can."),
                new HelpSection("WHAT IS LEFT OUT",
                    "The exclusion list beside the engine (tools/wim_exclusions.ini, copied next to the exe) skips System Volume " +
                    "Information, the Recycle Bin, pagefile.sys, hiberfil.sys, swapfile.sys, Windows Temp and the root Temp " +
                    "folder. Opening the volume with --snapshot means a running machine can be captured without being " +
                    "stopped, and the excluded paths keep the image smaller and restorable."),
            },
            new[]
            {
                "Capture reads a live volume through a snapshot, so the machine can keep running.",
                "A restore overwrites the target disk — check the TARGET line twice.",
            }),

        new(
            "backup-disk", "Back up to disk", "DIALOGS",
            "The wizard for full images: choose a disk with room, then optionally stamp a small USB stick with a pointer to the image.",
            new[]
            {
                new HelpSection("WHY A WIZARD",
                    "A full Windows image is usually far larger than the USB stick you want to boot from. The wizard separates " +
                    "the two jobs: the image goes to a disk with room, the stick carries a pointer and the restore instructions."),
                new HelpSection("THE THREE CHOICES",
                    "SOURCE is the volume to capture. DESTINATION is any fixed, external or removable disk PC Medic can write " +
                    "to — the default image path is D:\\PCMedic-Backups\\PCMedic-Backup-YYYYMMDD-HHmm.wim on the chosen disk. " +
                    "RECOVERY USB is optional: press it to cycle through attached removable disks, ending at NONE."),
                new HelpSection("THE SIZE VERDICT",
                    "The footer compares an estimate — used bytes on the source times a compression factor (FAST 55%, " +
                    "MAXIMUM 38%, NONE 100%) — against the destination's free space. Green means it fits; red names both numbers " +
                    "and START BACKUP refuses until you pick a bigger disk."),
                new HelpSection("WHAT THE STICK RECEIVES",
                    "A PCMedicRecovery folder holding backup-location.json and RESTORE-README.txt. The JSON records the image " +
                    "path, its size, the source volume and label, the compression, the date and the machine name. The README " +
                    "says, in plain steps, how to boot the stick, open PC Medic and restore from that path."),
                new HelpSection("WHEN THE POINTER IS WRITTEN",
                    "After a successful capture, never before: a pointer to an image that failed to write would be worse than " +
                    "no pointer. If the stick is write-protected or removed mid-run, the backup still completes and the status " +
                    "line says the pointer failed."),
                new HelpSection("RESTORING FROM WINDOWS RECOVERY",
                    "Stamping a stick also writes the Windows RE side of it: restore.cmd plus wimlib-imagex.exe " +
                    "and libwim-15.dll, in PCMedicRecovery beside the pointer. Boot the stick, choose Troubleshoot, " +
                    "Advanced options, Command Prompt, and run that restore.cmd (RESTORE-README.txt carries the " +
                    "one line that finds it when the stick's letter has changed). The menu reads the pointer, finds " +
                    "the image by its path and volume label, verifies the archive, lists the disks with their model " +
                    "and size, and then asks for the target volume. Nothing is written until ERASE is typed. It " +
                    "applies to the volume you name - it does not partition or format - then writes boot files with " +
                    "bcdboot for the boot mode the pointer records, falling back to /f ALL. Each run appends to " +
                    "restore-log.txt on the stick, so a failed recovery can be read afterwards."),
                new HelpSection("CHECKING A STICK BEFORE YOU NEED IT",
                    "CHECK STICK, beside RESTORE IMAGE, tests the selected stick while Windows is still running: " +
                    "it reads PCMedicRecovery\\backup-location.json, finds the image that file names, checks the file " +
                    "is still the size the stick recorded, then runs wimlib's integrity check over the whole archive. " +
                    "Every step is logged and the status line carries the verdict, so a stick that is missing its " +
                    "image, was unplugged before the backup finished or has gone bad is found now rather than in the " +
                    "middle of a recovery. It cannot test booting the stick - that needs a restart - so it proves " +
                    "everything except the boot itself."),
                new HelpSection("WINDOWS REPAIR ON THE SAME STICK",
                    "A pointer is only half a recovery stick: it needs something to boot. RECOVERY DRIVE\u2026 opens " +
                    "Windows' own \"Create a recovery drive\" tool, and a stick made there boots into Windows recovery, " +
                    "where " + RecoveryStick.StandardRepairs + " live. The wizard reports which stick has that " +
                    "environment - " + RecoveryStick.PresentVerdict + " or " + RecoveryStick.MissingVerdict + " - and " +
                    "writes its pointer and restore note beside it, so one stick offers both the Windows repairs and the " +
                    "image restore. The drive list is re-read when the tool closes, because it reformats the stick."),
                new HelpSection("AFTER A BARE-METAL RESTORE",
                    "Boot the stick with the backup disk attached and open Backup & Restore: the pointer is found and the image " +
                    "it names is filled in, so you pick the target disk and press RESTORE IMAGE. If the card says the disk is not " +
                    "attached, plug it in and press REFRESH; BROWSE still works if you would rather find the file yourself. Boot " +
                    "repair runs automatically afterwards."),
            },
            new[]
            {
                "The stick only needs room for two small text files, so 16 GB is plenty.",
                "Removable and external disks appear in both SOURCE and DESTINATION lists.",
            }),

        new(
            "tweaks", "Tweaks", "PAGES",
            "Explorer and visual-effects tweaks, each written with a backup of the previous value so it can be reverted.",
            new[]
            {
                new HelpSection("WHAT IS OFFERED",
                    "Menu and window animation delays, taskbar and Explorer behaviour, thumbnail caching and a handful of " +
                    "visual-effects switches. Each row states exactly which registry value it writes."),
                new HelpSection("BACKUPS AND HISTORY",
                    "Before a value changes, its previous data is stored in PC Medic's tweak backup. TWEAK HISTORY lists what " +
                    "was changed, when, and offers PUT BACK for anything recorded. Rows that were never changed have nothing " +
                    "to revert, and say so."),
                new HelpSection("SCOPE",
                    "Per-user values apply to your account immediately. Machine-wide ones need elevation and a sign-out or " +
                    "restart before Explorer picks them up."),
                new HelpSection("SENSIBLE DEFAULTS",
                    "Turning every animation off makes the shell feel fast but can make some older applications look broken. " +
                    "Change one group at a time and check before moving on."),
            }),

        new(
            "software", "Software", "PAGES",
            "The installed-software inventory with security-flagged updates, pins, ignore rules, batch runs, and the updater settings.",
            new[]
            {
                new HelpSection("THE UPDATES CARD",
                    "SCAN asks winget what has a newer version, then flags updates that look security-relevant. Rows can be " +
                    "ticked and updated together, or one at a time from the row's own action. CANCEL stops the run after the " +
                    "package in flight."),
                new HelpSection("THE INSTALLED CARD",
                    "Every application recorded in either uninstall registry hive, plus Store packages, with a search plate, " +
                    "sorting, CSV/HTML export and CLEAN DEAD for entries whose uninstaller no longer exists. STORE and PIN " +
                    "badges say how a row is tracked."),
                new HelpSection("BULK EXES",
                    "Opens the bulk installer for files already on disk — vendor update packages, MSI and MSU hotfixes — run " +
                    "one at a time with restarts held until the last one finishes. That is the right tool for a folder of Dell " +
                    "update packages, which winget does not know about."),
                new HelpSection("PINNING, IGNORING AND SCHEDULES",
                    "A pin freezes an application at its current version. An ignore rule hides a package from scans entirely — " +
                    "the blacklist window lists and clears them. The settings card also holds the scan schedule and the reboot " +
                    "policy used at the end of a run."),
                new HelpSection("WHAT A RUN ACTUALLY DOES",
                    "Updates and installs go through winget with the flags its version supports, one package at a time, because " +
                    "two Windows Installer operations cannot run together. Every run writes a log under Logs\\ and reports the " +
                    "tail of winget's own output on failure."),
            },
            new[]
            {
                "Hiding a package is reversible from MANAGE HIDDEN UPDATES.",
                "A failed update can be hidden so the next scan stops offering it.",
            }),

        new(
            "catalogue", "Catalogue", "PAGES",
            "A curated list of common Windows software: tick what you want and let PC Medic install it quietly from the vendors' own installers.",
            new[]
            {
                new HelpSection("WHAT IT IS",
                    "A bundled catalogue ships inside the executable, so the list works with no network. Each entry names the " +
                    "publisher, the download source and the silent switch its installer accepts."),
                new HelpSection("INSTALLING",
                    "Tick the entries and press INSTALL SELECTED. Each download is verified against the catalogue's SHA-256 when " +
                    "one is recorded, or checked for a valid Authenticode signature when it is not, before anything is run. " +
                    "The progress window shows one row per package."),
                new HelpSection("REFRESH CATALOGUE",
                    "Pulls a newer catalogue from the configured address so the list can change without a new build. The bundled " +
                    "copy remains the fallback when the network is unavailable."),
                new HelpSection("LIMITS",
                    "Catalogue installs are the publisher's own installer with a silent switch; a package that ignores its " +
                    "switch will show its own window. Nothing is installed without you ticking it."),
            }),

        new(
            "updates", "Updates", "PAGES",
            "winget application upgrades on the left, Windows Update driver packages on the right, sharing one pending count with the shell badge.",
            new[]
            {
                new HelpSection("APPLICATIONS",
                    "SCAN runs winget's upgrade query. DOWNLOAD ALL installs every listed upgrade in one pass; the row action " +
                    "upgrades or uninstalls a single package. An upgrade is verified afterwards — winget is asked again, then " +
                    "Add or Remove Programs — and one that cannot be confirmed offers to be hidden rather than left forever."),
                new HelpSection("DRIVERS",
                    "SCAN asks the Windows Update agent for driver packages. A separate device audit names hardware Windows has " +
                    "no driver for at all, which is a different answer from 'nothing was offered'. Installing drivers needs " +
                    "administrator rights."),
                new HelpSection("OFFLINE",
                    "OFFLINE holds the folder workflow: INSTALL FROM FOLDER imports driver packages from a USB stick or a saved " +
                    "backup, matching them against this machine's devices; SAVE DRIVER BACKUP exports the driver store so a " +
                    "rebuild needs no network. It also opens the hidden-updates file."),
                new HelpSection("WHY A PACKAGE CAN STAY LISTED",
                    "winget reports 'Unknown' for some installed versions, so a package can remain on the list after a " +
                    "successful install. PC Medic confirms through the uninstall registry and offers to hide what it cannot " +
                    "verify, instead of pretending the update failed."),
            },
            new[]
            {
                "Driver installs restart nothing by themselves; a restart is offered at the end.",
                "The nav badge counts applications plus drivers.",
            }),

        new(
            "bulk", "Bulk installer", "DIALOGS",
            "Run a folder of installers (.exe, .msi, .msu) one at a time, with the silent switch each format documents and every restart held back.",
            new[]
            {
                new HelpSection("OPENING IT",
                    "Software page → UPDATES card → BULK EXES. PICK FOLDER scans a folder and its subfolders; ADD FILES… adds " +
                    "individual installers. Every row starts ticked, and the status line counts what is ticked of what was found."),
                new HelpSection("THE SWITCHES IT USES",
                    "Dell update packages get /s /l=\"log\" — Dell's own silent switch, with no /r, so /s alone installs without " +
                    "rebooting. MSI files run through msiexec with /qn /norestart REBOOT=ReallySuppress. MSU files run through " +
                    "wusa with /quiet /norestart. Any other .exe gets /S, marked '(guessed)' on the row because /S is right for " +
                    "many installers and not all of them."),
                new HelpSection("SILENT ON / OFF",
                    "OFF runs each package with its own window so you can answer its questions. The row subtitle always shows " +
                    "the exact command line that will run, before anything starts."),
                new HelpSection("DEFER RESTARTS",
                    "On by default. While the batch runs, a restart barrier cancels any shutdown already scheduled (shutdown /a " +
                    "every few seconds) and sets Windows Update's NoAutoRebootWithLoggedOnUsers policy, restoring it afterwards. " +
                    "A restart is only offered once the last package has finished."),
                new HelpSection("EXIT CODES IT UNDERSTANDS",
                    "Windows Installer 3010 and 1641 mean success with a restart pending; 2359302 means already installed; " +
                    "1618 means another installer was running, which is retried for up to five minutes. Dell packages are read " +
                    "Dell's way: 2, 6 and 14 are successes that want a reboot, 3 and 13 mean there was nothing to do."),
                new HelpSection("CANCELLING",
                    "CANCEL stops the queue, never the installer in flight: killing Setup.exe halfway leaves a machine " +
                    "half-updated. Each package writes its own log under Logs\\bulk\\."),
            },
            new[]
            {
                "Needs an elevated run: the window says so instead of starting.",
                "Running out of disk or a 1603 failure is reported on the row and in the activity feed.",
            }),

        new(
            "audit", "Registry Audit", "PAGES",
            "Two audits on one page: dead App Paths, orphaned uninstall keys and stale file associations, plus ~25,000 shortcuts resolved with IShellLinkW.",
            new[]
            {
                new HelpSection("WHAT COUNTS AS DEAD",
                    "A registry value whose target file no longer exists, or an uninstall key whose uninstaller is gone. A " +
                    "shortcut whose target, working directory or arguments cannot be resolved is listed for review rather than " +
                    "deleted, because a target missing from one drive letter may exist on a removable one."),
                new HelpSection("REMOVING",
                    "Each removal writes a .reg backup of the key first. Rows you mark as 'not dead' go into a persistent keep " +
                    "list, so a scan stops reporting them."),
                new HelpSection("TIMING",
                    "The shortcut audit resolves every link in the Start Menu, Desktop and ProgramData trees, which takes a " +
                    "minute on a full profile. Its progress count is real: it knows how many links are left."),
            }),

        new(
            "benchmarks", "Benchmarks", "PAGES",
            "Quick CPU and disk sanity runs that tell you whether a machine behaves like its specification, and install a missing benchmark rig from the page.",
            new[]
            {
                new HelpSection("WHAT IT RUNS",
                    "Short CPU integer and floating-point loops, and sequential and random disk reads, reported as rates you can " +
                    "compare against a previous run on the same machine. Nothing is uploaded anywhere."),
                new HelpSection("THE RIG",
                    "If Cinebench or a disk benchmark is not present beside the executable, the tile installs it through winget " +
                    "and then opens it. The portable folders are looked up beside PC Medic so the suite stays USB-friendly."),
                new HelpSection("INTERPRETING IT",
                    "A result far below the hardware's expectation usually means thermal throttling, a background task, or a " +
                    "failing drive. Run it twice before drawing a conclusion."),
            }),

        new(
            "console", "Command Shell", "PAGES",
            "An embedded command prompt and PowerShell runner for the jobs the pages do not wrap, with the transcript kept on screen.",
            new[]
            {
                new HelpSection("RUNNING THINGS",
                    "Switch between CMD and POWERSHELL with the two buttons, type a command and press Enter. The quick chips run " +
                    "the common diagnostics — SFC, DISM, netstat, disk info, hotfixes — with one press."),
                new HelpSection("ELEVATION",
                    "Commands run with the token PC Medic has. ELEVATED CMD and ELEVATED PS open a separate prompt through UAC " +
                    "for the jobs that need administrator rights; the console itself does not silently elevate."),
                new HelpSection("GOOD USES",
                    "Reading a specific event log, testing a driver flag, or checking a port before blaming the network. The " +
                    "transcript is on screen but is not written to the log file — copy anything you need to keep."),
            }),

        new(
            "downloads", "Download and progress window", "DIALOGS",
            "The pass window shared by installs, driver packages, updates, benchmark rigs and bulk runs: one row per package, a real bar, and one cancel.",
            new[]
            {
                new HelpSection("THE ROWS",
                    "Each package gets its own row with a badge that moves through WAITING, a percentage, DONE or FAILED, and a " +
                    "subtitle carrying the reason when it fails. The overall bar tracks the whole queue."),
                new HelpSection("PROGRESS HONESTY",
                    "Where the underlying tool reports bytes or a percentage, the bar is that number. Where the work is one " +
                    "opaque call, the bar eases toward a ceiling and resolves when the call returns — it never reaches 100% " +
                    "before the work is done."),
                new HelpSection("CANCELLING",
                    "CANCEL stops the package in flight where the tool supports it, and always stops the queue. The corner close " +
                    "also stops the pass and shuts the window, so a stubborn installer cannot hold it hostage."),
                new HelpSection("AFTERWARDS",
                    "A batch that failed somewhere ends on the failed row, and the FAILED button cycles back to each failure after " +
                    "you have scrolled away."),
            }),

        new(
            "hidden-updates", "Hidden updates", "DIALOGS",
            "The package ids scans no longer offer, with the choice to let any of them, or all of them, be offered again.",
            new[]
            {
                new HelpSection("WHERE IT IS",
                    "Updates page \u2192 the OFFLINE button's menu \u2192 MANAGE HIDDEN UPDATES\u2026 The same menu holds OPEN HIDDEN " +
                    "UPDATES\u2026, which opens the blacklist file itself in your editor for a look."),
                new HelpSection("WHAT IS LISTED",
                    "The ids remembered in updates-ignored.json under %LOCALAPPDATA%\\PC Medic. They are filtered out of winget's " +
                    "results, so a package that cannot be upgraded or removed does not come back on every scan. The count line " +
                    "says NOTHING IS HIDDEN when the list is empty."),
                new HelpSection("UNHIDING",
                    "Select one or more rows (Ctrl and Shift work) and press UNHIDE SELECTED, or UNHIDE ALL after the confirmation. " +
                    "Only the remembered id is removed \u2014 nothing is installed or upgraded by this window. The package returns on the " +
                    "next scan, and the Updates page reloads its list as soon as the window closes."),
                new HelpSection("HOW SOMETHING GOT HIDDEN",
                    "A failed update whose new version cannot be confirmed, or one winget keeps reporting with an unknown " +
                    "installed version, offers HIDE from its row or at the end of a run. Hiding changes nothing about Windows " +
                    "Update, and it does not uninstall the application."),
                new HelpSection("IF IT REFUSES",
                    "COULD NOT UNHIDE means the blacklist file could not be written \u2014 usually a permissions or disk problem \u2014 and " +
                    "the id is still in memory until the next start. Everything this window writes is logged in the activity feed."),
            },
            new[]
            {
                "Hiding is a note to yourself, not a block: the package still exists and still works.",
                "The file is plain JSON, so a broken edit can be repaired by hand from OPEN HIDDEN UPDATES\u2026",
            }),

        new(
            "junk-cleanup", "Disk cleanup", "DIALOGS",
            "The CLEAN DRIVE dialog: eight cache and log categories, what each weighs, and removal of only the rows you tick.",
            new[]
            {
                new HelpSection("OPENING IT",
                    "Overview \u2192 CLEAN DRIVE. The scan starts as the window opens and reports the total it found; categories with " +
                    "nothing in them are listed but left unticked."),
                new HelpSection("THE CATEGORIES",
                    "TEMPORARY FILES (setup leftovers and app temp files older than ten minutes), WINDOWS UPDATE CACHE, RECYCLE " +
                    "BIN, THUMBNAIL & ICON CACHE, SHADER CACHE, BROWSER CACHES (Chrome, Edge and Firefox \u2014 close the browser for a " +
                    "full clean), WINDOWS LOGS and DELIVERY OPTIMIZATION. Rows that need an elevated session say NEEDS ADMIN."),
                new HelpSection("HOW REMOVAL WORKS",
                    "CLEAN SELECTED runs one category at a time on a background thread, so the window keeps drawing. Items go to " +
                    "the Recycle Bin wherever Windows allows it, a wrong tick is recoverable, and files held open by a running " +
                    "program are skipped rather than forced. The Recycle Bin row itself is purged with Clear-RecycleBin, which " +
                    "clears every user's bin on the machine."),
                new HelpSection("THE RESULT WINDOW",
                    "A ring reports both passes \u2014 a percentage while it scans the eight categories, and again while it cleans. " +
                    "When the clean finishes, a results window adds the pass up: bytes freed, items removed, files held open by a " +
                    "running app and what is queued for the next boot, with one line per category so a row that did nothing is " +
                    "visible. The scan re-runs behind it and leaves every row unticked; tick what remains to try the locked files " +
                    "again. Each category also writes its own line to the activity feed."),
                new HelpSection("WHAT IT WILL NOT TOUCH",
                    "Only caches, logs and already-deleted items: no documents, no program files, nothing in a user's profile " +
                    "outside the caches. Deleting logs is genuinely lossy for troubleshooting, so leave WINDOWS LOGS ticked off while " +
                    "chasing a problem."),
            },
            new[]
            {
                "CLEAN SELECTED does nothing until at least one row is ticked.",
                "Esc closes the window, but not while a clean is still running.",
            }),

        new(
            "help", "Using this help", "HELP",
            "The manual you are reading: one topic per page and dialog, opened from the ? in the header or from a dialog's own ?.",
            new[]
            {
                new HelpSection("HOW TO OPEN IT",
                    "The ? button in the shell header opens the topic for the page on screen. Dialogs that do real work — the " +
                    "bulk installer, the backup wizard — carry their own ? next to CLOSE. The list on the left switches topics " +
                    "without closing the window."),
                new HelpSection("WHAT EACH TOPIC COVERS",
                    "What the feature reads and writes, what needs administrator rights, what it refuses to do, and the limits " +
                    "worth knowing before you trust it. Where a page has a screenshot beside this text, it was taken from the " +
                    "running application."),
                new HelpSection("IF SOMETHING DISAGREES",
                    "The text and the page live in the same repository, so a behaviour change and its help change together. If " +
                    "you find a line here that no longer matches the window, that is a bug in the help — say so and it gets " +
                    "fixed with the feature."),
            },
            new[]
            {
                "Esc closes the help window; the topic list keeps its place.",
            }),
    };
}
