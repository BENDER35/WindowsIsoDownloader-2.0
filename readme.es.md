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
