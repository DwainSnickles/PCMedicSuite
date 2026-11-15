namespace PC_Medic
{
    internal static class Program
    {
        private const string InstanceMutex = @"Local\PCMedicSuite.Console.1";
        private const string ShowEventName = @"Local\PCMedicSuite.ShowConsole.1";

        /// <summary>Exit code when a copy already owns the console, so the debugger and scripts can tell why nothing opened.</summary>
        private const int AlreadyRunningExitCode = 2;

        /// <summary>The main entry point for the application.</summary>
        [STAThread]
        private static void Main(string[] args)
        {
            // Scheduled-task entry point: scan for software updates headlessly, optionally
            // balloon a notice, and get out. Never installs, never touches the console UI.
            if (args.Contains(ScanSchedule.ScanArgument, StringComparer.OrdinalIgnoreCase))
            {
                Environment.ExitCode = RunBackgroundScan();
                return;
            }

            // One console per session: a second launch surfaces the running window and exits.
            // --elevated-restart is exempt: the copy being replaced has released the mutex name but
            // may not have exited yet, so the elevated relaunch must not read that as a duplicate.
            bool elevatedRestart = args.Contains("--elevated-restart", StringComparer.OrdinalIgnoreCase);
            using var instance = new Mutex(true, InstanceMutex, out bool isFirst);
            _instance = instance;

            // Allow a second instance when debugging so breakpoints work.
            // Normal launches still enforce single-instance behavior.
            if (!isFirst && !elevatedRestart && !System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debug.WriteLine(
                    "PC MEDIC is already running — asking the existing console to come back from the tray.");

                ActivityLog.Info("PC MEDIC is already running — brought the console back from the tray.");
                SignalRunningConsole();
                Environment.ExitCode = AlreadyRunningExitCode;
                return;
            }

            ApplicationConfiguration.Initialize();

            try
            {
                Application.SetColorMode(SystemColorMode.Dark);
            }
            catch
            {
                // Window manager has no dark title bar; the app itself is dark regardless.
            }

            if (!Native.IsElevated)
            {
                using var gate = new ElevationGateForm();

                if (gate.ShowDialog() == DialogResult.OK && gate.RestartRequested)
                {
                    try
                    {
                        instance.ReleaseMutex();
                    }
                    catch (ApplicationException)
                    {
                        // Already released; the elevated copy still needs the name free.
                    }

                    if (Native.RestartElevated()) return;

                    ActivityLog.Warn("Administrator access was declined \u2014 continuing in limited mode.");
                }
                else
                {
                    ActivityLog.Warn("Running limited: repairs, machine-wide tweaks and Disk Defrag are read-only.");
                }
            }
            else
            {
                ActivityLog.Info("Running elevated \u2014 every maintenance task is available.");
            }

            ActivityLog.Info("PC Medic online \u2014 portable maintenance suite.");

            Application.ThreadException += (_, e) => ActivityLog.Error(e.Exception.ToString());
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex) ActivityLog.Error(ex.ToString());
            };

            using var splash = new SplashForm();

            splash.Show();
            Application.DoEvents();

            System.Diagnostics.Debug.WriteLine("DEBUG: Creating MainForm...");

            //Breakpoint never hits here unless not elevated but we need it to hit for both 
            using var shell = new MainForm();

            System.Diagnostics.Debug.WriteLine("DEBUG 5: MainForm created");

            while (!splash.Finished)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }

            System.Diagnostics.Debug.WriteLine("DEBUG 6: Splash finished");

            splash.Close();
            ListenForSecondLaunch(shell);
            Application.Run(shell);
        }

        /// <summary>
        /// The --scan path used by the "PC Medic Scan" scheduled task. Runs the full
        /// inventory scan, writes everything to the daily log, and pops a balloon tip
        /// when updates were found and the user asked to be notified.
        /// </summary>
        private static int RunBackgroundScan()
        {
            try
            {
                Log.Info("Background scan started (scheduled task).");

                var settings = new SettingsService();
                settings.Load();
                var activity = new ActivityService();
                activity.Load();
                var inventory = new InventoryService(new WingetClient(), settings);

                ScanResult result = inventory
                    .ScanAsync()
                    .GetAwaiter()
                    .GetResult();

                Log.Info($"Background scan finished: {result.Updates.Count} update(s) available " +
                         $"across {result.Installed.Count} installed app(s).");

                if (result.Updates.Count > 0 && settings.Current.NotifyOnUpdates)
                {
                    using var tray = new NotifyIcon
                    {
                        Icon = SystemIcons.Information,
                        Visible = true,
                    };
                    tray.ShowBalloonTip(6000, "PC Medic",
                        $"{result.Updates.Count} software update(s) are available — open PC Medic to review them.",
                        ToolTipIcon.Info);
                    Thread.Sleep(6500);
                    tray.Visible = false;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.Error("Background scan failed", ex);
                return 1;
            }
        }

        /// <summary>Asks the copy that owns the console to come back out of the tray.</summary>
        private static Mutex? _instance;

    /// <summary>Gives up the console name before relaunching, so the elevated copy is not turned away.</summary>
    internal static void ReleaseInstance()
    {
        try
        {
            _instance?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Already released; the name is free either way.
        }
    }

    /// <summary>Takes the console name back when the relaunch was cancelled.</summary>
    internal static void ReclaimInstance()
    {
        try
        {
            _instance?.WaitOne(0);
        }
        catch (Exception)
        {
            // Nothing to reclaim; the next launch simply starts a fresh console.
        }
    }        private static void SignalRunningConsole()
        {
            // TryOpenExisting rather than OpenExisting: opening a name that is not there throws, and a
            // debugger prints that first-chance exception as though the launch had faulted. A copy that
            // is still on its splash has no event yet, and is about to be on screen anyway.
            if (!EventWaitHandle.TryOpenExisting(ShowEventName, out EventWaitHandle? show)) return;

            using (show)
            {
                show.Set();
            }
        }

        /// <summary>Wakes the shell whenever a second copy asks it to surface.</summary>
        private static void ListenForSecondLaunch(MainForm shell)
        {
            var show = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);

            // Create the handle up front so a request that arrives during the splash still lands.
            _ = shell.Handle;

            var listener = new Thread(() =>
            {
                while (true)
                {
                    show.WaitOne();

                    try
                    {
                        shell.BeginInvoke(() =>
                        {
                            ActivityLog.Info("PC MEDIC was launched again \u2014 window restored from the tray.");
                            shell.ShowFromTray();
                            shell.NotifySecondLaunch();
                        });
                    }
                    catch (Exception)
                    {
                        // The console is closing; nothing left to surface.
                    }
                }
            })
            {
                IsBackground = true,
                Name = "PC Medic single instance",
            };

            listener.Start();
        }
    }
}
