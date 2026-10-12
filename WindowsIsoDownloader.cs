using Microsoft.Playwright;
using System.Text.Json;
using WindowsIsoDownloader;
using WindowsIsoDownloader.Extension;

// Allow the Unicode progress bar (█ / ░) to render correctly on every OS/terminal.
try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
}
catch
{
    // Some environments (e.g. redirected output) do not support changing the encoding.
}

var jsonConfig = ReadConfigFile(out var configPath);
Config? config = null;

try
{
    config = JsonSerializer.Deserialize<Config>(jsonConfig, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    });
}
catch (Exception)
{
    ErrorLoadingConfig(configPath);
    return -1;
}

if (null == config
    || string.IsNullOrWhiteSpace(config.DownloadFolder)
    || string.IsNullOrWhiteSpace(config.DownloadFilename)
    || config.Actions is null
    || config.Actions.Count == 0)
{
    ErrorLoadingConfig(configPath);
    return -1;
}

Directory.CreateDirectory(config.DownloadFolder);

var outputPath = Path.GetFullPath(Path.Combine(config.DownloadFolder, config.DownloadFilename));
Console.WriteLine($"[Info] Running on {System.Runtime.InteropServices.RuntimeInformation.OSDescription}.");
Console.WriteLine($"[Info] The ISO will be saved to: {outputPath}");

var exitCode = InstallPlaywrightDependencies();
if (exitCode != 0)
{
    Console.WriteLine("[Fatal] Failed to install Playwright dependencies.");
    Console.WriteLine("[Hint] On Linux install the required system libraries with:");
    Console.WriteLine("       ./bin/Release/net10.0/playwright install-deps firefox");
    return -1;
}

Console.WriteLine("[Success] Playwright dependencies installed.");
Console.WriteLine("[Info] Trying to obtain the ISO download link...");

float currentProgress = 0;
string? isoFileUrl = null;

using var playwright = await Playwright.CreateAsync();

IBrowser browser;
try
{
    browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions());
}
catch (PlaywrightException e)
{
    Console.WriteLine($"[Fatal] Could not launch Firefox: {e.Message}");
    Console.WriteLine("[Hint] On Linux install the required system libraries with:");
    Console.WriteLine("       ./bin/Release/net10.0/playwright install-deps firefox");
    return -1;
}

await using var browserScope = browser;

// Microsoft's download page uses an anti-bot mechanism ("Sentinel") that can reject the
// request from a given browser session (especially under heavy/automated use). Retrying
// with a brand new browser session usually succeeds, so give it a few attempts.
const int maxAttempts = 3;
for (int attempt = 1; attempt <= maxAttempts && string.IsNullOrWhiteSpace(isoFileUrl); attempt++)
{
    Console.WriteLine($"[Info] Attempt {attempt} of {maxAttempts}...");

    await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
    {
        Locale = "en-US",
    });
    var page = await context.NewPageAsync();

    var downloadButton = await RunActionsAsync(page, config.Actions);
    isoFileUrl = await ResolveIsoUrlAsync(page, downloadButton);

    if (string.IsNullOrWhiteSpace(isoFileUrl) && attempt < maxAttempts)
    {
        Console.WriteLine("[Warn] Download link not found yet. The site may be applying anti-bot protection (Sentinel). Retrying with a fresh session...");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

if (string.IsNullOrWhiteSpace(isoFileUrl))
{
    Console.WriteLine("[Fatal] Download button was not found. The Microsoft page layout may have changed, or the request was blocked by anti-bot protection.");
    Console.WriteLine("[Hint] Review the actions defined in config.json to match the current page.");
    return -1;
}

if (!isoFileUrl.Contains(".iso"))
{
    Console.WriteLine($"[Fatal] Found a download link but it seems incorrect: {isoFileUrl}");
    return -1;
}

Console.WriteLine($"[Success] Download link found: {isoFileUrl}");
Console.WriteLine("[Info] ISO download in progress...");
using (var httpClient = new HttpClient())
{
    httpClient.Timeout = TimeSpan.FromHours(2);

    try
    {
        using (var filestream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var progress = new Progress<float>();
            progress.ProgressChanged += ProgressChanged;
            await httpClient.DownloadAsync(isoFileUrl, filestream, progress);
        }
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine($"[Fatal] Can't write at path: {outputPath}.");
        Console.WriteLine("[Hint] Check the folder permissions, or run the tool as administrator / with sudo.");
        return -1;
    }
}

Console.WriteLine();
Console.WriteLine("[Success] ISO Downloaded successfully! Exiting with code 0.");

return 0;

async Task<IElementHandle?> RunActionsAsync(IPage page, List<BrowserAction> actions)
{
    IElementHandle? downloadButton = null;

    foreach (var action in actions.OrderBy(x => x.Order))
    {
        if (action.WaitBeforeAction is > 0)
        {
            await page.WaitForTimeoutAsync(action.WaitBeforeAction.Value);
        }

        switch (action.Kind)
        {
            case "Goto":
                if (string.IsNullOrEmpty(action.Parameters.Url))
                {
                    throw new ArgumentNullException("The Url parameter for the Goto action is not specified.", innerException: null);
                }
                await page.GotoAsync(action.Parameters.Url);
                break;

            case "SelectOption":
                if (string.IsNullOrEmpty(action.Parameters.Selector) || string.IsNullOrEmpty(action.Parameters.Values))
                {
                    throw new ArgumentNullException("The Selector and/or the Values parameter(s) for the SelectOption action is/are not specified.", innerException: null);
                }
                await page.SelectOptionAsync(action.Parameters.Selector, action.Parameters.Values);
                break;

            case "Click":
                if (string.IsNullOrEmpty(action.Parameters.Selector))
                {
                    throw new ArgumentNullException("The Selector parameter for the Click action is not specified.", innerException: null);
                }
                await page.ClickAsync(action.Parameters.Selector);
                break;

            case "QuerySelector":
                if (string.IsNullOrEmpty(action.Parameters.Selector))
                {
                    throw new ArgumentNullException("The Selector parameter for the QuerySelector action is not specified.", innerException: null);
                }
                downloadButton = await page.QuerySelectorAsync(action.Parameters.Selector);
                break;

            default:
                throw new ArgumentException($"Unknown action kind: {action.Kind}");
        }
    }

    return downloadButton;
}

async Task<string?> ResolveIsoUrlAsync(IPage page, IElementHandle? downloadButton)
{
    // Poll the page for every anchor that points to an .iso file. The download
    // section renders asynchronously, so give it a few seconds. This also makes
    // the tool resilient to layout changes.
    List<string> isoLinks = new();
    for (int attempt = 0; attempt < 10; attempt++)
    {
        // Return a JSON string and deserialize it ourselves: Playwright's own
        // EvaluateAsync deserializer does not round-trip a List<string> cleanly.
        var json = await page.EvaluateAsync<string>(
            "() => JSON.stringify(Array.from(document.querySelectorAll('a[href*=\".iso\"]')).map(a => a.href))");
        isoLinks = JsonSerializer.Deserialize<List<string>>(json) ?? new();
        if (isoLinks.Count > 0)
        {
            break;
        }
        await page.WaitForTimeoutAsync(1000);
    }

    // Some pages expose more than one image; the Windows 10 page, for example,
    // offers a 32-bit and a 64-bit ISO. Prefer the 64-bit one.
    var preferred = isoLinks.FirstOrDefault(href => href.Contains("x64", StringComparison.OrdinalIgnoreCase));
    if (!string.IsNullOrWhiteSpace(preferred))
    {
        return preferred;
    }

    // Otherwise prefer the element located by the QuerySelector action from config.json.
    if (downloadButton is not null)
    {
        var href = await downloadButton.EvaluateAsync<string>("element => element.href");
        if (!string.IsNullOrWhiteSpace(href))
        {
            return href;
        }
    }

    // Last resort: the first .iso link found.
    return isoLinks.FirstOrDefault();
}

void ProgressChanged(object? sender, float e)
{
    // The progress in the e variable is in the range 0.000 (0%) to 1.000 (100%)
    float reportedProgress = (float)Math.Round(e, 3);

    if (reportedProgress > currentProgress)
    {
        currentProgress = reportedProgress;
        for (int i = 0; i < 100; i++)
        {
            Console.Write((i < (int)(currentProgress * 100)) ? "█" : "░");
        }
        Console.Write($" {currentProgress.ToString("p1")}");

        // Console.SetCursorPosition is not supported when the output is redirected
        // (for example, when logging to a file or a CI pipeline), so only reposition
        // the cursor on interactive terminals.
        if (!Console.IsOutputRedirected)
        {
            try
            {
                Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
            }
            catch (IOException)
            {
                // Ignore: the console does not support cursor positioning.
            }
        }
    }
}

void ErrorLoadingConfig(string path)
{
    Console.WriteLine($"[Fatal] Unable to load the configuration from '{path}'. Exiting with code -1.");
}

string ReadConfigFile(out string path)
{
    // Prefer a config.json next to the executable, fall back to the current directory.
    var basePath = Path.Combine(AppContext.BaseDirectory, "config.json");
    if (File.Exists(basePath))
    {
        path = basePath;
    }
    else
    {
        path = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
    }

    return File.ReadAllText(path);
}

int InstallPlaywrightDependencies()
{
    Console.WriteLine("[Info] Installing dependencies for Playwright...");

    return Microsoft.Playwright.Program.Main(new[] { "install", "firefox" });
}
