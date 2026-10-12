# Windows 11 ISO Downloader

Windows 11 ISO Downloader es una pequeña herramienta multiplataforma que
descarga de forma programática la última **ISO oficial de Windows 11** directa
de los servidores de Microsoft. Controla un navegador Firefox real (en modo
headless) mediante [Playwright](https://playwright.dev/dotnet/) para recorrer la
página de descarga, obtiene el enlace temporal de descarga y después transmite
la ISO a disco con una barra de progreso en vivo.

Resulta útil en proyectos que construyen instaladores personalizados de
Windows 11 (por ejemplo
[`tiny11builder`](https://github.com/ntdevlabs/tiny11builder)), donde los pasos
manuales del navegador son la única parte que no se puede automatizar.

> **Idioma:** [English](readme.md) | [Español](readme.es.md)
> **Guía técnica:** [English](docs/TECHNICAL_GUIDE.md) | [Español](docs/TECHNICAL_GUIDE.es.md)

## Requisitos

- SDK de **.NET 10** (para compilar) o el runtime de .NET 10 (para ejecutar una
  compilación publicada).
- Acceso a Internet a `microsoft.com` y `software.download.prss.microsoft.com`.
- **Solo Linux:** las librerías del sistema que necesita la compilación de
  Firefox de Playwright. Instálalas con:
  ```bash
  ./bin/Release/net10.0/playwright install-deps firefox
  ```
  (requiere `sudo`; en Debian/Ubuntu el script usa `apt-get`).

No hace falta instalar ningún navegador manualmente: la herramienta descarga la
compilación de Firefox de Playwright correspondiente la primera vez que se
ejecuta.

## Compilación

```bash
dotnet build -c Release
```

El resultado de la compilación queda en `bin/Release/net10.0/`.

Para generar un ejecutable autocontenido para una plataforma concreta:

```bash
# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true

# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true
```

### 32 bits (x86) y otras arquitecturas

El proyecto se compila como **AnyCPU** con `Prefer32Bit` desactivado, así que un
mismo ensamblado gestionado funciona en runtimes de 32 y 64 bits; en Windows de
64 bits el proceso por defecto es de 64 bits. Compilaciones adicionales:

```bash
# Windows de 32 bits (x86)
dotnet publish -c Release -r win-x86 --self-contained true

# Linux ARM de 64 bits
dotnet publish -c Release -r linux-arm64 --self-contained true
```

> **Importante:** el trabajo pesado lo hace el motor del navegador. Playwright
> solo distribuye un driver de **64 bits** (`win32_x64`, `linux-x64`,
> `linux-arm64`, macOS). Por eso una compilación `win-x86` funciona en una
> instalación de Windows de 64 bits (el proceso de 32 bits arranca el driver de
> 64 bits incluido), pero no puede ejecutarse en un sistema Windows
> exclusivamente de 32 bits. Del mismo modo, .NET 10 no tiene runtime Linux x86
> de 32 bits (`linux-x86`) y Playwright no tiene driver para ARM de 32 bits, por
> lo que una compilación `linux-arm` compila pero el navegador no puede
> arrancar. Consulta la
> [guía técnica](docs/TECHNICAL_GUIDE.es.md#81-soporte-de-32-bits-x86-y-arm)
> para ver la tabla completa.

## Ejecución

La herramienta lee `config.json` desde la misma carpeta que el ejecutable (si
no lo encuentra, usa el directorio de trabajo actual).

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

La ISO se escribe en la carpeta configurada en `config.json` (por defecto el
directorio de trabajo actual, generando `./windows11.iso`).

## Configuración (`config.json`)

Todo el flujo está basado en datos, de modo que si Microsoft cambia su página
solo tienes que editar `config.json` en lugar del código fuente.

| Campo | Descripción |
| --- | --- |
| `DownloadFolder` | Carpeta donde se guarda la ISO. Se aceptan rutas de Windows (`C:\\isos`) y de Linux (`/home/usuario/isos`). Una ruta relativa (por defecto `.`) se resuelve respecto al directorio de trabajo. |
| `DownloadFilename` | Nombre del archivo ISO resultante. |
| `Actions` | Lista ordenada de acciones del navegador para llegar al enlace de descarga. |

Cada acción tiene:

| Campo | Descripción |
| --- | --- |
| `Order` | Orden de ejecución (ascendente). |
| `Kind` | Uno de `Goto`, `SelectOption`, `Click`, `QuerySelector`. |
| `WaitBeforeAction` | Retardo opcional en **milisegundos** antes de ejecutar la acción. |
| `Parameters` | Parámetros propios de la acción (`Url`, `Selector`, `Values`). |

Acciones soportadas:

- **`Goto`** – navega a `Parameters.Url`.
- **`SelectOption`** – selecciona `Parameters.Values` en el elemento que
  coincide con `Parameters.Selector`.
- **`Click`** – hace clic en el elemento que coincide con
  `Parameters.Selector`.
- **`QuerySelector`** – guarda el elemento que coincide con
  `Parameters.Selector`; la herramienta lee después su `href` para obtener el
  enlace de la ISO.

### Actualizar los valores de las opciones

Microsoft cambia periódicamente los identificadores internos de la edición y del
idioma. Puedes obtener los valores actuales directamente en la página:

1. Abre la [página de descarga de Windows 11](https://www.microsoft.com/en-us/software-download/windows11).
2. Abre las herramientas de desarrollador del navegador.
3. Id de edición: inspecciona el `<select>` `#product-edition`. El valor de la
   opción "multi-edition ISO for x64 devices" va en la acción `Order: 2`.
4. Idioma: elige la edición, pulsa **Confirm** y luego inspecciona el
   `<select>` `#product-languages`. Copia el `value` completo de tu idioma
   (tiene el formato `{"id":"27160","language":"English"}`) en la acción
   `Order: 4`.

La herramienta es resistente: después de ejecutar las acciones también busca
cualquier enlace `a[href*=".iso"]` en la página, por lo que pequeños cambios en
el marcado normalmente no la rompen.

### Idiomas disponibles

La edición **Windows 11 multi-edition ISO for x64 devices** (`3813`) ofrece los
siguientes 38 idiomas. Para descargar uno de ellos, sustituye el `Values` de la
acción `SelectOption` con `Order: 4` en `config.json`:

| Idioma | `id` | `Values` (pegar esto) |
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

> Estos `id` pertenecen a la edición `3813` (acción `Order: 2`). Los idiomas que
> puedas echar en falta (por ejemplo indonesio, malayo, persa o vietnamita) no
> se ofrecen para esta edición. La opción más a prueba de futuro sigue siendo
> copiar el `value` completo desde la página, como se describió antes.

## Códigos de salida

| Código | Significado |
| --- | --- |
| `0` | La ISO se descargó correctamente. |
| `-1` | Ocurrió un error (configuración, navegador, protección anti-bot o E/S). |

## Solución de problemas

- **`Could not launch Firefox` en Linux** – instala las dependencias del sistema
  de Playwright: `playwright install-deps firefox` (ver *Requisitos*).
- **`Download button was not found` / anti-bot** – Microsoft protege el
  conector de descarga con un mecanismo llamado *Sentinel* que puede rechazar
  una sesión del navegador, sobre todo con uso intensivo o automatizado. La
  herramienta ya reintenta hasta 3 veces con una sesión nueva; si sigue
  fallando, espera unos minutos e inténtalo de nuevo.
- **Errores de permisos** – asegúrate de que `DownloadFolder` sea escribible. En
  Windows ejecuta la terminal como administrador; en Linux revisa los permisos
  de la carpeta.

## Licencia

Consulta [LICENSE](LICENSE).
