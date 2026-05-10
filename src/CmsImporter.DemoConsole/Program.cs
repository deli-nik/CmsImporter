using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CmsImporter.DemoConsole;

/// <summary>
/// Interactive console that demonstrates the CmsImporter API — enqueue FileSystem and HttpRest
/// imports, poll job status in real time, and query imported content. Communicates with the
/// WebApi over HTTP using <see cref="System.Net.Http.HttpClient"/>.
/// </summary>
public class Program
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static string _defaultSamplesPath = string.Empty;

    private static string _defaultRestUrl = string.Empty;

    private static Guid? _lastJobId;

    /// <summary>
    /// Entry point. Accepts an optional API base URL as <c>args[0]</c> (defaults to
    /// <c>http://localhost:5050</c>) and an optional samples directory path as <c>args[1]</c>.
    /// Returns <c>0</c> on a normal exit or <c>1</c> when the API is unreachable at startup.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5050";
        _defaultSamplesPath = args.Length > 1
            ? args[1]
            : ResolveSamplesPath();
        _defaultRestUrl = $"{baseUrl.TrimEnd('/')}/demo/source-feed";

        Console.OutputEncoding = System.Text.Encoding.UTF8;
        using var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(60) };

        WriteHeader(baseUrl);

        if (!await WaitForApiAsync(client))
        {
            WriteError("API not reachable. Is the WebApi running?");
            return 1;
        }

        while (true)
        {
            ShowMenu();
            var key = Console.ReadKey(intercept: true).Key;
            Console.WriteLine();

            try
            {
                switch (key)
                {
                    case ConsoleKey.D1 or ConsoleKey.NumPad1:
                        await ImportFromFileSystemAsync(client);
                        break;

                    case ConsoleKey.D2 or ConsoleKey.NumPad2:
                        await ImportFromHttpRestAsync(client);
                        break;

                    case ConsoleKey.D3 or ConsoleKey.NumPad3:
                        await ListJobsAsync(client);
                        break;

                    case ConsoleKey.D4 or ConsoleKey.NumPad4:
                        await GetJobAsync(client);
                        break;

                    case ConsoleKey.D5 or ConsoleKey.NumPad5:
                        await QueryContentAsync(client);
                        break;

                    case ConsoleKey.D6 or ConsoleKey.NumPad6:
                        await ListConnectorsAsync(client);
                        break;

                    case ConsoleKey.H:
                        await CheckHealthAsync(client);
                        break;

                    case ConsoleKey.D0 or ConsoleKey.NumPad0 or ConsoleKey.Q or ConsoleKey.Escape:
                        WriteInfo("Bye.");
                        return 0;

                    default:
                        WriteWarn($"Unknown key '{key}'. Pick from the menu.");
                        break;
                }
            }
            catch (HttpRequestException ex)
            {
                WriteError($"HTTP error: {ex.Message}");
            }
            catch (Exception ex)
            {
                WriteError($"{ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private static void ShowMenu()
    {
        Console.WriteLine();
        WriteColor(ConsoleColor.Cyan, "=== CmsImporter Demo ===");
        Console.WriteLine("  1) Import from FileSystem (JSON files)");
        Console.WriteLine("  2) Import from HttpRest (paginated REST)");
        Console.WriteLine("  3) List recent jobs");
        Console.WriteLine("  4) Get job status by id");
        Console.WriteLine("  5) Query imported content");
        Console.WriteLine("  6) List available connectors");
        Console.WriteLine("  H) Health check");
        Console.WriteLine("  0) Exit");
        Console.Write("> ");
    }

    private static void WriteHeader(string baseUrl)
    {
        WriteColor(ConsoleColor.Cyan, """
            CmsImporter — interactive demo console
            """);
        Console.WriteLine($"  API:        {baseUrl}");
        Console.WriteLine($"  Samples:    {_defaultSamplesPath}");
        Console.WriteLine($"  REST mock:  {_defaultRestUrl}");
        Console.WriteLine();
    }

    private static async Task<bool> WaitForApiAsync(HttpClient client)
    {
        Console.Write("Waiting for /health");
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var resp = await client.GetAsync("/health");
                if (resp.IsSuccessStatusCode)
                {
                    Console.WriteLine(" ✓");
                    return true;
                }
            }
            catch
            {
                // swallow — keep polling
            }

            Console.Write(".");
            await Task.Delay(300);
        }

        Console.WriteLine();
        return false;
    }

    private static async Task ImportFromFileSystemAsync(HttpClient client)
    {
        var path = PromptDefault("Path to JSON files", _defaultSamplesPath);
        var sourceSystem = PromptDefault("Source system tag", "demo-files");

        await EnqueueAndFollowAsync(client, new
        {
            source = "FileSystem",
            config = new Dictionary<string, string>
            {
                ["path"] = path,
                ["sourceSystem"] = sourceSystem,
            },
        });
    }

    private static async Task ImportFromHttpRestAsync(HttpClient client)
    {
        var url = PromptDefault("Base URL", _defaultRestUrl);
        var sourceSystem = PromptDefault("Source system tag", "demo-rest");
        var pageSize = PromptDefault("Page size", "2");

        await EnqueueAndFollowAsync(client, new
        {
            source = "HttpRest",
            config = new Dictionary<string, string>
            {
                ["baseUrl"] = url,
                ["sourceSystem"] = sourceSystem,
                ["pageSize"] = pageSize,
            },
        });
    }

    private static async Task EnqueueAndFollowAsync(HttpClient client, object request)
    {
        var response = await client.PostAsJsonAsync("/imports", request);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await response.Content.ReadAsStringAsync();
            WriteError($"POST /imports → {(int)response.StatusCode}: {msg}");
            return;
        }

        var enqueue = await response.Content.ReadFromJsonAsync<EnqueueResponse>(Json)
            ?? throw new InvalidOperationException("Empty response from /imports");
        _lastJobId = enqueue.JobId;
        WriteInfo($"Enqueued: {enqueue.JobId}");

        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(2);
        while (DateTime.UtcNow < deadline)
        {
            var job = await client.GetFromJsonAsync<JobResponse>($"/imports/{enqueue.JobId}", Json);
            if (job is null)
            {
                WriteWarn("Tracker did not find the job (yet)...");
                await Task.Delay(200);
                continue;
            }

            DrawProgressLine(job);

            if (job.Status is "Completed" or "Failed" or "Cancelled")
            {
                Console.WriteLine();
                PrintJobSummary(job);
                return;
            }

            await Task.Delay(200);
        }

        WriteWarn("Timed out waiting for job to complete (2 min).");
    }

    private static void DrawProgressLine(JobResponse job)
    {
        var color = job.Status switch
        {
            "Running" => ConsoleColor.Yellow,
            "Completed" => ConsoleColor.Green,
            "Failed" => ConsoleColor.Red,
            "Cancelled" => ConsoleColor.DarkYellow,
            _ => ConsoleColor.Gray,
        };

        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(
            $"\r  [{job.Status,-10}] extracted={job.Counts.Extracted,-4} transformed={job.Counts.Transformed,-4} " +
            $"loaded={job.Counts.Loaded,-4} (new={job.Counts.New,-4} upd={job.Counts.Updated,-4}) " +
            $"notified={job.Counts.Notified,-4}");
        Console.ForegroundColor = prev;
    }

    private static void PrintJobSummary(JobResponse job)
    {
        Console.WriteLine();
        if (job.Status == "Completed")
        {
            WriteSuccess($"Job {job.JobId} completed.");
        }
        else if (job.Status == "Failed")
        {
            WriteError($"Job {job.JobId} failed: {job.FailureReason}");
        }
        else
        {
            WriteWarn($"Job {job.JobId} ended with status {job.Status}.");
        }

        var duration = (job.CompletedAt - job.StartedAt) ?? TimeSpan.Zero;
        Console.WriteLine($"  duration: {duration.TotalMilliseconds:N0} ms");

        if (job.Errors.Count > 0)
        {
            WriteWarn($"  {job.Errors.Count} error(s):");
            foreach (var err in job.Errors.Take(5))
            {
                Console.WriteLine($"    · {Truncate(err, 200)}");
            }
        }
    }

    private static async Task ListJobsAsync(HttpClient client)
    {
        var jobs = await client.GetFromJsonAsync<List<JobResponse>>("/imports", Json) ?? [];
        if (jobs.Count == 0)
        {
            WriteInfo("No jobs yet.");
            return;
        }

        Console.WriteLine($"{jobs.Count} job(s) (newest first):");
        foreach (var j in jobs.Take(10))
        {
            Console.WriteLine($"  {j.EnqueuedAt:HH:mm:ss} {j.JobId} {j.SourceConnector,-12} [{j.Status,-10}] " +
                $"loaded={j.Counts.Loaded} (new={j.Counts.New} upd={j.Counts.Updated})");
        }
    }

    private static async Task GetJobAsync(HttpClient client)
    {
        var defaultId = _lastJobId?.ToString() ?? "";
        var idText = PromptDefault("Job id", defaultId);
        if (!Guid.TryParse(idText, out var id))
        {
            WriteWarn("Not a valid GUID.");
            return;
        }

        var job = await client.GetFromJsonAsync<JobResponse>($"/imports/{id}", Json);
        if (job is null)
        {
            WriteWarn($"Job {id} not found.");
            return;
        }

        Console.WriteLine(JsonSerializer.Serialize(job, Json));
    }

    private static async Task QueryContentAsync(HttpClient client)
    {
        var sourceSystem = PromptDefault("Source system filter (blank for all)", "");
        var type = PromptDefault("Type filter [Page/Article/Media] (blank for all)", "");
        var limit = PromptDefault("Limit", "20");

        var qs = new List<string> { $"limit={limit}" };
        if (!string.IsNullOrWhiteSpace(sourceSystem))
        {
            qs.Add($"sourceSystem={Uri.EscapeDataString(sourceSystem)}");
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            qs.Add($"type={Uri.EscapeDataString(type)}");
        }

        var url = $"/content?{string.Join('&', qs)}";
        var items = await client.GetFromJsonAsync<List<ContentResponse>>(url, Json) ?? [];

        Console.WriteLine($"{items.Count} item(s):");
        foreach (var c in items)
        {
            Console.WriteLine($"  [{c.Type,-7}] {c.SourceSystem,-12} {c.ExternalId,-30} " +
                $"v{c.Version} \"{Truncate(c.Title, 50)}\"");
        }
    }

    private static async Task ListConnectorsAsync(HttpClient client)
    {
        var connectors = await client.GetFromJsonAsync<List<string>>("/imports/connectors", Json) ?? [];
        Console.WriteLine($"{connectors.Count} connector(s) registered:");
        foreach (var c in connectors)
        {
            Console.WriteLine($"  · {c}");
        }
    }

    private static async Task CheckHealthAsync(HttpClient client)
    {
        var resp = await client.GetAsync("/health");
        var body = await resp.Content.ReadAsStringAsync();
        var color = resp.IsSuccessStatusCode ? ConsoleColor.Green : ConsoleColor.Red;
        WriteColor(color, $"  {(int)resp.StatusCode} {resp.ReasonPhrase}: {body}");
    }

    private static string PromptDefault(string label, string defaultValue)
    {
        Console.Write(string.IsNullOrEmpty(defaultValue) ? $"  {label}: " : $"  {label} [{defaultValue}]: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? defaultValue : input.Trim();
    }

    private static string ResolveSamplesPath()
    {
        // Walk up from the working directory looking for samples/source-cms.
        var current = Directory.GetCurrentDirectory();
        for (var i = 0; i < 6; i++)
        {
            var probe = Path.Combine(current, "samples", "source-cms");
            if (Directory.Exists(probe))
            {
                return Path.GetFullPath(probe);
            }

            var parent = Directory.GetParent(current)?.FullName;
            if (parent is null || parent == current)
            {
                break;
            }

            current = parent;
        }

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "samples", "source-cms"));
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : string.Concat(s.AsSpan(0, max), "...");

    private static void WriteColor(ConsoleColor color, string message)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = prev;
    }

    private static void WriteInfo(string message) => WriteColor(ConsoleColor.Cyan, message);

    private static void WriteSuccess(string message) => WriteColor(ConsoleColor.Green, message);

    private static void WriteWarn(string message) => WriteColor(ConsoleColor.Yellow, message);

    private static void WriteError(string message) => WriteColor(ConsoleColor.Red, message);

    private sealed record EnqueueResponse(Guid JobId, DateTimeOffset EnqueuedAt);

    private sealed record JobResponse(
        Guid JobId,
        string SourceConnector,
        string Status,
        ImportCounts Counts,
        DateTimeOffset EnqueuedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        string? FailureReason,
        IReadOnlyList<string> Errors);

    private sealed record ImportCounts(
        int Extracted,
        int Transformed,
        int ValidationFailed,
        int Loaded,
        int New,
        int Updated,
        int Notified);

    private sealed record ContentResponse(
        Guid Id,
        string ExternalId,
        string SourceSystem,
        string Type,
        string Title,
        string Slug,
        uint Version,
        DateTimeOffset ImportedAt);
}
