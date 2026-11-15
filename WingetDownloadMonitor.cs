using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PC_Medic;

/// <summary>
/// winget prints almost nothing while it downloads an installer, so a progress bar fed only
/// by its output sits at zero for minutes and the run looks hung. This watcher recovers real
/// progress the way WinGet-GUI does: once a "Downloading" line appears, it polls winget's
/// temp folders for the installer file that is actually growing and reports megabytes and
/// percentage from that file's size.
/// </summary>
internal sealed class WingetDownloadMonitor : IDisposable
{
    private static readonly Regex UrlPattern = new(@"https?://[^\s""']+", RegexOptions.Compiled);

    private readonly Action<string, int?> _report;
    private readonly DateTime _operationStart;
    private CancellationTokenSource? _cts;
    private Task? _poll;
    private string? _pendingUrl;
    private string? _trackedFile;
    private long _contentLength;
    private bool _finished;

    public WingetDownloadMonitor(Action<string, int?> report)
    {
        _report = report;
        _operationStart = DateTime.UtcNow.AddSeconds(-5);
    }

    /// <summary>Feed every winget output line through here. Cheap when idle.</summary>
    public void HandleLine(string line)
    {
        if (_finished) return;
        var lower = line.ToLowerInvariant();

        if (lower.Contains("verified") || lower.Contains("installer hash") ||
            lower.Contains("download completed") || lower.Contains("starting package install") ||
            lower.Contains("installing..."))
        {
            Stop();
            return;
        }

        if (!lower.Contains("download")) return;

        // winget sometimes wraps the URL across two lines; hold a fragment that looks
        // like the start of one until the next line completes it.
        if (_pendingUrl is not null)
        {
            var trimmed = line.Trim();
            if (!trimmed.Contains(' ') && trimmed.Contains('.'))
            {
                TryQueryContentLength(_pendingUrl + trimmed);
            }
            _pendingUrl = null;
        }

        var match = UrlPattern.Match(line);
        if (match.Success)
        {
            var url = match.Value;
            bool looksComplete = url.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                                 url.EndsWith(".msi", StringComparison.OrdinalIgnoreCase) ||
                                 url.EndsWith(".msix", StringComparison.OrdinalIgnoreCase) ||
                                 url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            if (!looksComplete && !url.EndsWith('/'))
            {
                _pendingUrl = url;
                return;
            }
            TryQueryContentLength(url);
        }

        StartPolling();
    }

    private void StartPolling()
    {
        if (_poll is not null) return;
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _poll = Task.Run(async () =>
        {
            double lastMb = -1;
            int lastPct = -1;
            int ticks = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var file = FindBestFile(ref ticks);
                    if (file is not null)
                    {
                        _trackedFile = file.FullName;
                        long downloaded = DownloadedBytes(file);
                        long total = EffectiveTotal(file);

                        if (downloaded > 0 || total > 0)
                        {
                            double pct = total > downloaded ? downloaded * 100.0 / total : 0;
                            if (pct >= 99) pct = 99;
                            double mb = downloaded / (1024.0 * 1024.0);
                            int ipct = (int)Math.Floor(pct);

                            if (Math.Abs(mb - lastMb) >= 0.2 || ipct != lastPct)
                            {
                                lastMb = mb;
                                lastPct = ipct;
                                string text = total > 0
                                    ? $"Downloading: {mb:0.#} MB / {total / (1024.0 * 1024.0):0.#} MB ({ipct}%)"
                                    : $"Downloading: {mb:0.#} MB";
                                _report(text, ipct);
                            }
                        }
                    }
                }
                catch { /* a missed poll is not worth surfacing */ }

                try { await Task.Delay(250, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }, ct);
    }

    private FileInfo? FindBestFile(ref int ticksSinceScan)
    {
        ticksSinceScan++;
        var tempRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");
        var wingetTemp = Path.Combine(tempRoot, "WinGet");
        var best = BestIn(wingetTemp, recursive: true);
        if (best is null || ticksSinceScan < 4)
        {
            var top = BestIn(tempRoot, recursive: false);
            if (top is not null && (best is null || top.Length > best.Length)) best = top;
        }
        if (ticksSinceScan >= 4) ticksSinceScan = 0;
        return best;
    }

    private FileInfo? BestIn(string dir, bool recursive)
    {
        try
        {
            if (!Directory.Exists(dir)) return null;
            FileInfo? best = null;
            long bestSize = -1;
            var di = new DirectoryInfo(dir);
            foreach (var f in recursive ? di.GetFiles("*", SearchOption.AllDirectories)
                                        : di.GetFiles("*.*", SearchOption.TopDirectoryOnly))
            {
                if (f.FullName.Contains(@"\cache\", StringComparison.OrdinalIgnoreCase)) continue;
                var ext = f.Extension;
                if (ext.Equals(".yaml", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".mszyml", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
                    ext.Equals(".log", StringComparison.OrdinalIgnoreCase)) continue;
                if (recursive &&
                    !ext.Equals(".tmp", StringComparison.OrdinalIgnoreCase) &&
                    !ext.Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
                    !ext.Equals(".msi", StringComparison.OrdinalIgnoreCase) &&
                    !ext.Equals(".msix", StringComparison.OrdinalIgnoreCase) &&
                    !ext.Equals(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                if (f.LastWriteTimeUtc < _operationStart) continue;

                long size = Math.Max(f.Length, PhysicalSize(f.FullName));
                if ((size > 65536 || f.Length > 65536) && size > bestSize)
                {
                    best = f;
                    bestSize = size;
                }
            }
            return best;
        }
        catch { return null; }
    }

    private static long DownloadedBytes(FileInfo f)
    {
        // DeliveryOptimization pre-allocates sparse files: physical size is the truth.
        long physical = PhysicalSize(f.FullName);
        return physical >= 0 ? physical : f.Length;
    }

    private long EffectiveTotal(FileInfo f)
    {
        if (_contentLength > 0) return _contentLength;
        long logical = f.Length;
        long physical = PhysicalSize(f.FullName);
        return logical > physical ? logical : physical;
    }

    private void TryQueryContentLength(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var ct = _cts?.Token ?? CancellationToken.None;
        _ = Task.Run(async () =>
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("PCMedic");
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                if (resp.IsSuccessStatusCode && resp.Content.Headers.ContentLength > 0)
                    Interlocked.Exchange(ref _contentLength, resp.Content.Headers.ContentLength.Value);
            }
            catch { }
        }, ct);
    }

    private void Stop()
    {
        _finished = true;
        try { _cts?.Cancel(); } catch { }
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose() => Stop();

    [DllImport("kernel32.dll", EntryPoint = "GetCompressedFileSizeW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetCompressedFileSizeW(
        [MarshalAs(UnmanagedType.LPWStr)] string lpFileName, out uint lpFileSizeHigh);

    private static long PhysicalSize(string path)
    {
        try
        {
            uint high;
            uint low = GetCompressedFileSizeW(path, out high);
            if (low == 0xFFFFFFFF && Marshal.GetLastWin32Error() != 0) return -1;
            return ((long)high << 32) | low;
        }
        catch { return -1; }
    }
}
