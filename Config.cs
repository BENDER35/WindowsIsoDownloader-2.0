namespace WindowsIsoDownloader
{
    /// <summary>
    /// Strongly typed representation of <c>config.json</c>. It drives the whole
    /// download workflow: where to save the ISO and which browser actions to run.
    /// </summary>
    internal sealed class Config
    {
        /// <summary>Folder where the ISO file will be written. Works on Windows and Linux.</summary>
        public string DownloadFolder { get; set; } = string.Empty;

        /// <summary>File name (not the full path) of the produced ISO.</summary>
        public string DownloadFilename { get; set; } = string.Empty;

        /// <summary>Ordered list of browser actions used to reach the download link.</summary>
        public List<BrowserAction> Actions { get; set; } = new();
    }

    /// <summary>A single browser interaction (navigation, select, click or query).</summary>
    internal sealed class BrowserAction
    {
        /// <summary>Execution order, ascending.</summary>
        public int Order { get; set; }

        /// <summary>
        /// Action type. Supported values: <c>Goto</c>, <c>SelectOption</c>,
        /// <c>Click</c> and <c>QuerySelector</c>.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>Optional delay (milliseconds) applied before running the action.</summary>
        public int? WaitBeforeAction { get; set; }

        /// <summary>Action specific parameters.</summary>
        public BrowserActionParameters Parameters { get; set; } = new();
    }

    /// <summary>Parameters accepted by <see cref="BrowserAction"/>.</summary>
    internal sealed class BrowserActionParameters
    {
        /// <summary>Target URL, used by the <c>Goto</c> action.</summary>
        public string? Url { get; set; }

        /// <summary>CSS selector, used by <c>SelectOption</c>, <c>Click</c> and <c>QuerySelector</c>.</summary>
        public string? Selector { get; set; }

        /// <summary>Value(s) passed to the <c>SelectOption</c> action.</summary>
        public string? Values { get; set; }
    }
}
