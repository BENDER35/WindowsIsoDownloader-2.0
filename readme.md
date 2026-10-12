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

### 32-bit (x86) and other architectures

The project is built as **AnyCPU** with `Prefer32Bit` disabled, so a single
managed assembly runs on 32-bit and 64-bit runtimes; on 64-bit Windows the
default process is 64-bit. Additional builds:

```bash
# 32-bit Windows (x86)
dotnet publish -c Release -r win-x86 --self-contained true

# 64-bit ARM Linux
dotnet publish -c Release -r linux-arm64 --self-contained true
```

> **Important:** the heavy lifting is done by the browser engine. Playwright only
> ships a **64-bit** driver (`win32_x64`, `linux-x64`, `linux-arm64`, macOS). A
> `win-x86` build therefore works on a 64-bit Windows installation (the 32-bit
> process starts the bundled 64-bit driver) but cannot run on a genuinely
> 32-bit-only Windows system. Likewise, .NET 10 has no 32-bit x86 Linux runtime
> (`linux-x86`), and Playwright has no 32-bit ARM driver, so a `linux-arm` build
> compiles but the browser cannot start. See the
> [technical guide](docs/TECHNICAL_GUIDE.md#81-32-bit-x86-and-arm-support) for
> the full matrix.

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

### Available languages

The **Windows 11 multi-edition ISO for x64 devices** edition (`3813`) offers the
following 38 languages. To download one of them, replace the `Values` of the
`SelectOption` action with `Order: 4` in `config.json`:

| Language | `id` | `Values` (paste this) |
| --- | --- | --- |
| Arabic | `27122` | `{"id":"27122","language":"Arabic"}` |
| Brazilian Portuguese | `27146` | `{"id":"27146","language":"Brazilian Portuguese"}` |
| Bulgarian | `27123` | `{"id":"27123","language":"Bulgarian"}` |
| Chinese (Simplified) | `27157` | `{"id":"27157","language":"Chinese (Simplified)"}` |
| Chinese (Traditional) | `27158` | `{"id":"27158","language":"Chinese (Traditional)"}` |
| Croatian | `27136` | `{"id":"27136","language":"Croatian"}` |
| Czech | `27124` | `{"id":"27124","language":"Czech"}` |
| Danish | `27125` | `{"id":"27125","language":"Danish"}` |
| Dutch | `27144` | `{"id":"27144","language":"Dutch"}` |
| English | `27160` | `{"id":"27160","language":"English"}` |
| English (United Kingdom) | `27128` | `{"id":"27128","language":"English (United Kingdom)"}` |
| Estonian | `27131` | `{"id":"27131","language":"Estonian"}` |
| Finnish | `27132` | `{"id":"27132","language":"Finnish"}` |
| French | `27134` | `{"id":"27134","language":"French"}` |
| French Canadian | `27133` | `{"id":"27133","language":"French Canadian"}` |
| German | `27126` | `{"id":"27126","language":"German"}` |
| Greek | `27127` | `{"id":"27127","language":"Greek"}` |
| Hebrew | `27135` | `{"id":"27135","language":"Hebrew"}` |
| Hungarian | `27137` | `{"id":"27137","language":"Hungarian"}` |
| Italian | `27138` | `{"id":"27138","language":"Italian"}` |
| Japanese | `27139` | `{"id":"27139","language":"Japanese"}` |
| Korean | `27140` | `{"id":"27140","language":"Korean"}` |
| Latvian | `27142` | `{"id":"27142","language":"Latvian"}` |
| Lithuanian | `27141` | `{"id":"27141","language":"Lithuanian"}` |
| Norwegian | `27143` | `{"id":"27143","language":"Norwegian"}` |
| Polish | `27145` | `{"id":"27145","language":"Polish"}` |
| Portuguese | `27147` | `{"id":"27147","language":"Portuguese"}` |
| Romanian | `27148` | `{"id":"27148","language":"Romanian"}` |
| Russian | `27149` | `{"id":"27149","language":"Russian"}` |
| Serbian Latin | `27152` | `{"id":"27152","language":"Serbian Latin"}` |
| Slovak | `27150` | `{"id":"27150","language":"Slovak"}` |
| Slovenian | `27151` | `{"id":"27151","language":"Slovenian"}` |
| Spanish | `27129` | `{"id":"27129","language":"Spanish"}` |
| Spanish (Mexico) | `27130` | `{"id":"27130","language":"Spanish (Mexico)"}` |
| Swedish | `27153` | `{"id":"27153","language":"Swedish"}` |
| Thai | `27154` | `{"id":"27154","language":"Thai"}` |
| Turkish | `27155` | `{"id":"27155","language":"Turkish"}` |
| Ukrainian | `27156` | `{"id":"27156","language":"Ukrainian"}` |

> These `id` values belong to edition `3813` (action `Order: 2`). Languages you
> may miss (for example Indonesian, Malay, Persian or Vietnamese) are simply not
> offered for this edition. The most future-proof option is still to copy the
> whole `value` from the page as described above.

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
