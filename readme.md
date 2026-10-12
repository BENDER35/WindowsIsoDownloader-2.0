# Windows 11 ISO Downloader

Windows 11 ISO Downloader is a small cross-platform tool that programmatically
downloads the latest official **Windows 11 ISO** directly from Microsoft's
servers. It drives a real (headless) Firefox browser with
[Playwright](https://playwright.dev/dotnet/) to walk through the download page,
collects the temporary download link and then streams the ISO to disk with a
live progress bar.

It is useful in projects that build custom Windows 11 installers (for example
[`tiny11builder`](https://github.com/ntdevlabs/tiny11builder)), where the manual
browser steps are the only part that cannot be automated.

> **Language:** [English](readme.md) | [Español](readme.es.md)
> **Technical guide:** [English](docs/TECHNICAL_GUIDE.md) | [Español](docs/TECHNICAL_GUIDE.es.md)

## Requirements

- **.NET 10** SDK (to build) or the .NET 10 runtime (to run a published build).
- Internet access to `microsoft.com` and `software.download.prss.microsoft.com`.
- **Linux only:** the system libraries required by the Playwright Firefox
  build. Install them with:
  ```bash
  ./bin/Release/net10.0/playwright install-deps firefox
  ```
  (needs `sudo`; on Debian/Ubuntu the script uses `apt-get`).

No manual browser installation is required: the tool downloads the matching
Playwright Firefox build the first time it runs.

## Building

```bash
dotnet build -c Release
```

The build output is placed in `bin/Release/net10.0/`.

To produce a self-contained executable for a specific platform:

```bash
# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true

# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true
```

## Running

The tool reads `config.json` from the same folder as the executable (falling
back to the current working directory).

**Linux / macOS**

```bash
cd bin/Release/net10.0
./WindowsIsoDownloader
```

**Windows**

```bat
cd bin\Release\net10.0
WindowsIsoDownloader.exe
```

The ISO is written to the folder configured in `config.json` (the default is
the current working directory, producing `./windows11.iso`).

## Configuration (`config.json`)

The whole workflow is data-driven so that, if Microsoft changes its page, you
only need to edit `config.json` instead of the source code.

| Field | Description |
| --- | --- |
| `DownloadFolder` | Folder where the ISO is saved. Both Windows (`C:\\isos`) and Linux (`/home/user/isos`) paths are accepted. A relative path (default `.`) is resolved against the working directory. |
| `DownloadFilename` | File name of the resulting ISO. |
| `Actions` | Ordered list of browser actions used to reach the download link. |

Each action has:

| Field | Description |
| --- | --- |
| `Order` | Execution order (ascending). |
| `Kind` | One of `Goto`, `SelectOption`, `Click`, `QuerySelector`. |
| `WaitBeforeAction` | Optional delay in **milliseconds** before running the action. |
| `Parameters` | Action specific parameters (`Url`, `Selector`, `Values`). |

Supported actions:

- **`Goto`** – navigates to `Parameters.Url`.
- **`SelectOption`** – selects `Parameters.Values` in the element matching
  `Parameters.Selector`.
- **`Click`** – clicks the element matching `Parameters.Selector`.
- **`QuerySelector`** – stores the element matching `Parameters.Selector`; the
  tool then reads its `href` to obtain the ISO link.

### Updating the option values

Microsoft periodically changes the internal ids of the edition and the
language. You can find the current values directly on the page:

1. Open the [Windows 11 download page](https://www.microsoft.com/en-us/software-download/windows11).
2. Open the browser developer tools.
3. Edition id: inspect the `#product-edition` `<select>` element. The value of
   the "multi-edition ISO for x64 devices" option goes into action `Order: 2`.
4. Language: choose the edition, click **Confirm**, then inspect the
   `#product-languages` `<select>`. Copy the whole `value` of your language
   (it looks like `{"id":"27160","language":"English"}`) into action `Order: 4`.

The tool is resilient: after running the actions it also looks for any
`a[href*=".iso"]` link on the page, so small markup changes usually do not
break it.

## Exit codes

| Code | Meaning |
| --- | --- |
| `0` | The ISO was downloaded successfully. |
| `-1` | An error occurred (configuration, browser, anti-bot protection or I/O). |

## Troubleshooting

- **`Could not launch Firefox` on Linux** – install the Playwright system
  dependencies: `playwright install-deps firefox` (see *Requirements*).
- **`Download button was not found` / anti-bot** – Microsoft protects the
  download connector with a mechanism called *Sentinel* that may reject a
  browser session, especially under heavy or automated use. The tool already
  retries up to 3 times with a fresh session; if it still fails, wait a few
  minutes and try again.
- **Permission errors** – make sure `DownloadFolder` is writable. On Windows
  run the terminal as administrator; on Linux check the folder permissions.

## License

See [LICENSE](LICENSE).
