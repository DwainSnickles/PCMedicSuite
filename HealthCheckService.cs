namespace PC_Medic;

internal enum HealthStatus
{
    Healthy,
    Repairable,
    Failed,
}

/// <summary>Outcome of the live component-store health pass.</summary>
internal sealed record HealthReport(HealthStatus Status, string Summary, string Detail);

/// <summary>
/// Live-Windows port of the legacy health check: DISM CheckHealth + ScanHealth,
/// then RestoreHealth + SFC only when the store reports repairable corruption.
/// DISM can run for tens of minutes, so everything uses the NeverKill policy.
/// </summary>
internal static class HealthCheckService
{
    private const int DetailLineCap = 400;

    public static async Task<HealthReport> RunAsync(IProgress<string>? progress, CancellationToken ct = default)
    {
        string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string dism = Path.Combine(system, "dism.exe");
        string sfc = Path.Combine(system, "sfc.exe");

        if (!File.Exists(dism) || !File.Exists(sfc))
        {
            return new HealthReport(HealthStatus.Failed, "DISM or SFC was not found.", string.Empty);
        }

        var detail = new System.Text.StringBuilder();

        async Task<string> Run(string file, string arguments, string phase)
        {
            progress?.Report(phase);
            ActivityLog.Info($"Health check: {phase}");

            var result = await ProcessRunner.RunAsync(
                file,
                arguments,
                ct,
                onOutputLine: line =>
                {
                    int? pct = WingetText.ParsePercent(line);
                    if (pct is > 0) progress?.Report($"{phase} — {pct}%");
                },
                timeout: null,
                abortPolicy: ProcessAbortPolicy.NeverKill);

            string output = result.Combined;
            detail.Append(">>> ").Append(phase).Append(": ").AppendLine(arguments);
            AppendTail(detail, output);
            detail.AppendLine();
            return output;
        }

        try
        {
            string check = await Run(dism, "/Online /Cleanup-Image /CheckHealth", "DISM CHECK HEALTH");
            string scan = await Run(dism, "/Online /Cleanup-Image /ScanHealth", "DISM SCAN HEALTH");
            string combined = (check + "\n" + scan).ToLowerInvariant();
            bool hadErrors = combined.Contains("error:");

            // "not repairable" must be tested before "repairable" — it contains both.
            if (combined.Contains("not repairable"))
            {
                return Finish(HealthStatus.Failed,
                    "Component store corruption found that DISM cannot repair.", detail);
            }

            if (combined.Contains("repairable"))
            {
                progress?.Report("DISM RESTORE HEALTH");
                string restore = await Run(dism, "/Online /Cleanup-Image /RestoreHealth", "DISM RESTORE HEALTH");
                string sfcOut = await Run(sfc, "/scannow", "SFC SCAN NOW");

                string repairCombined = (restore + "\n" + sfcOut).ToLowerInvariant();
                bool repairFailed = repairCombined.Contains("error:") ||
                                    repairCombined.Contains("could not perform");

                return Finish(
                    repairFailed ? HealthStatus.Failed : HealthStatus.Repairable,
                    repairFailed
                        ? "Repair ran but reported errors — check the activity log."
                        : "Component store was repairable; repairs applied.",
                    detail);
            }

            if (!hadErrors &&
                (combined.Contains("no component store corruption") ||
                 combined.Contains("component store is healthy") ||
                 combined.Contains("the component store is healthy")))
            {
                return Finish(HealthStatus.Healthy, "Component store is healthy.", detail);
            }

            return Finish(HealthStatus.Failed,
                hadErrors ? "Health check reported errors." : "Health check result was inconclusive.", detail);
        }
        catch (Exception ex)
        {
            ActivityLog.Error($"Health check failed: {ex.Message}");
            return new HealthReport(HealthStatus.Failed, $"Health check failed: {ex.Message}", detail.ToString());
        }
    }

    private static HealthReport Finish(HealthStatus status, string summary, System.Text.StringBuilder detail)
    {
        string tone = status switch
        {
            HealthStatus.Healthy => "OK",
            HealthStatus.Repairable => "REPAIRED",
            _ => "FAILED",
        };
        ActivityLog.Write(
            status == HealthStatus.Healthy ? LogLevel.Success :
            status == HealthStatus.Repairable ? LogLevel.Warn : LogLevel.Error,
            $"Component store health: {tone} — {summary}");
        return new HealthReport(status, summary, detail.ToString());
    }

    /// <summary>Keeps the tail of a tool's output so the detail stays readable.</summary>
    private static void AppendTail(System.Text.StringBuilder detail, string output)
    {
        string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines.TakeLast(DetailLineCap))
        {
            string trimmed = line.Trim();
            if (trimmed.Length > 0) detail.Append("  ").AppendLine(trimmed);
        }
    }
}
