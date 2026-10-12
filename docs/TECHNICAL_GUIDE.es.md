# Guía técnica — Windows 11 ISO Downloader

Este documento explica **cómo funciona internamente la herramienta**, su
arquitectura, el modelo de configuración y el razonamiento detrás de las
principales decisiones de implementación. Está dirigido a desarrolladores que
necesiten mantener, ampliar o auditar el proyecto.

> Idioma: [English](TECHNICAL_GUIDE.md) | [Español](TECHNICAL_GUIDE.es.md)
> Documentación principal: [readme.es.md](../readme.es.md)

## 1. Propósito y contexto

Microsoft publica las ISO de Windows 11 únicamente a través de una página web
interactiva. El enlace de descarga es una URL firmada y de corta duración que la
propia página genera tras seleccionar una edición y un idioma. No existe una API
pública oficial para esto.

Esta herramienta automatiza esa interacción con **Playwright para .NET** y luego
descarga la URL generada con `HttpClient`. El resultado es un único comando
repetible que un pipeline de compilación (por ejemplo
[`tiny11builder`](https://github.com/ntdevlabs/tiny11builder)) puede invocar para
obtener la ISO.

## 2. Pila tecnológica

| Capa | Tecnología |
| --- | --- |
| Runtime | .NET 10 (`net10.0`) |
| Lenguaje | C# con instrucciones de nivel superior y tipos de referencia anulables |
| Automatización del navegador | `Microsoft.Playwright` (Firefox headless) |
| Análisis de JSON | `System.Text.Json` |
| Descarga HTTP | `System.Net.Http.HttpClient` |
| Plataformas objetivo | Windows, Linux y macOS |

## 3. Estructura del proyecto

```
WindowsIsoDownloader-2.0/
├── WindowsIsoDownloader.csproj   # Proyecto estilo SDK, net10.0, referencia a Playwright
├── WindowsIsoDownloader.sln      # Solución de Visual Studio
├── WindowsIsoDownloader.cs       # Punto de entrada + todo el flujo (nivel superior)
├── Config.cs                     # POCOs que reflejan config.json
├── config.json                   # El script de acciones (editable sin recompilar)
├── Extension/
│   ├── HttpClientExtensions.cs   # DownloadAsync: descarga en streaming + progreso
│   └── StreamExtensions.cs       # CopyToAsync: copia por búferes con progreso
├── readme.md / readme.es.md      # Documentación de usuario (EN / ES)
└── docs/
    ├── TECHNICAL_GUIDE.md        # Este archivo (EN)
    └── TECHNICAL_GUIDE.es.md     # Versión en español
```

## 4. Modelo de configuración

`config.json` se deserializa en las clases de `Config.cs`:

```
Config
 ├── DownloadFolder : string
 ├── DownloadFilename : string
 └── Actions : List<BrowserAction>
        ├── Order : int
        ├── Kind : string            // Goto | SelectOption | Click | QuerySelector
        ├── WaitBeforeAction : int?  // milisegundos
        └── Parameters : BrowserActionParameters
               ├── Url : string?
               ├── Selector : string?
               └── Values : string?
```

`JsonSerializerOptions` habilita `PropertyNameCaseInsensitive`,
`ReadCommentHandling = Skip` y `AllowTrailingCommas`, de modo que el archivo
tolera mayúsculas/minúsculas, comentarios y comas finales. Esto facilita su
edición manual.

Las acciones se ordenan por `Order` y las interpreta un pequeño intérprete
(`RunActionsAsync`) cuyo conjunto de acciones es intencionadamente mínimo:

| `Kind` | Efecto en la página |
| --- | --- |
| `Goto` | `page.GotoAsync(url)` |
| `SelectOption` | `page.SelectOptionAsync(selector, values)` |
| `Click` | `page.ClickAsync(selector)` |
| `QuerySelector` | `page.QuerySelectorAsync(selector)`, guardando el elemento |

Mantener el flujo en datos (en lugar de codificado) es la decisión de diseño
central: cuando Microsoft renumera una opción, es un cambio de una línea en
`config.json`.

## 5. Flujo de ejecución

```
Inicio
  │
  ├─ 1. Fijar la codificación de salida en UTF-8 (glifos de la barra)   [best effort]
  ├─ 2. Leer y deserializar config.json  ── ¿inválido? ──► salir -1
  ├─ 3. Validar config (carpeta, nombre de archivo, acciones no vacías)
  ├─ 4. Crear DownloadFolder; calcular la ruta absoluta de salida
  ├─ 5. Instalar dependencias del navegador Playwright (firefox) ── ¿falla? ──► salir -1
  ├─ 6. Playwright.CreateAsync() + Firefox.LaunchAsync()
  │
  ├─ 7. Bucle de reintentos (hasta 3 intentos, contexto nuevo cada vez):
  │        a. Nuevo contexto (Locale = en-US) + nueva página
  │        b. RunActionsAsync(page, actions)   → quizá un handle de QuerySelector
  │        c. ResolveIsoUrlAsync(page, handle):
  │              - usa el href del handle si existe
  │              - si no, sondea cualquier `a[href*=".iso"]` (10 intentos / 1 s)
  │        d. Si se encontró enlace → dejar de reintentar
  │           si no, esperar 5 s y reintentar
  │
  ├─ 8. Si no hay enlace → salir -1
  ├─ 9. HttpClient.DownloadAsync(isoUrl, FileStream, progress)  → escribe la ISO
  ├─ 10. Mostrar éxito y salir 0
```

### 5.1 Por qué existe el bucle de reintentos

El conector de descarga de Microsoft está protegido por un mecanismo anti-bot
interno que responde:

```json
{"Errors":[{"Key":"ErrorSettings.SentinelReject",
           "Value":"Sentinel marked this request as rejected.","Type":8}]}
```

cuando desconfía de una sesión de navegador (habitual con acceso automatizado o
repetido). Un contexto de navegador completamente nuevo suele obtener una sesión
aceptada, así que la herramienta reintenta hasta tres veces. El fallo no lo
provoca el código, es una decisión del servidor, y la herramienta degrada con
elegancia mostrando un mensaje claro.

### 5.2 Resolución del enlace

El punto históricamente frágil era el selector CSS del botón de descarga
(`#card-info-content .button`). Microsoft cambió la clase en su marcado, así que
ahora el código:

1. Usa el elemento de la acción `QuerySelector` configurada, si existe.
2. Si no, recurre a cualquier ancla cuyo `href` contenga `.iso`, sondeando unos
   segundos porque la sección de descarga se renderiza de forma asíncrona.

La URL final se valida (`Contains(".iso")`) antes de descargarla.

## 6. Descarga con progreso

`HttpClientExtensions.DownloadAsync` solicita la URL con
`HttpCompletionOption.ResponseHeadersRead` para que la respuesta se transmita
en streaming en vez de almacenarse en búfer, y delega en
`StreamExtensions.CopyToAsync`. Ese método copia el flujo en bloques de 80 KiB y
notifica el número total de bytes leídos mediante un `IProgress<long>`.
`DownloadAsync` convierte los bytes en una proporción `0..1` y la notifica como
`IProgress<float>`, que el programa dibuja como una barra de 100 columnas.

Las llamadas de `Progress<T>` se despachan en el contexto de sincronización
capturado; como el valor es un `float` simple y las escrituras a consola son
baratas, esto es suficiente. El renderizador de progreso protege
`Console.SetCursorPosition` con `!Console.IsOutputRedirected` y un
`try/catch (IOException)`, de modo que una salida redirigida (logs de CI,
archivos) no bloquee la herramienta.

## 7. Notas de multiplataforma

El proyecto original apuntaba a .NET 6 y asumía Windows (`"DownloadFolder":
"C:\\"`). Los siguientes cambios lo hacen funcionar también en Linux/macOS:

- **Framework de destino** elevado a `net10.0`.
- **Playwright** actualizado a una versión reciente con compilaciones de
  navegador al día para Linux.
- **Carpeta por defecto en `config.json`** cambiada a `"."` (relativa al
  directorio de trabajo) en vez de `C:\`.
- **Tratamiento de rutas** con `Path.Combine` + `Path.GetFullPath`, de modo que
  funcionan tanto rutas con `/` como con `\`.
- **Salida de consola** fijada a UTF-8 para que se vean los glifos `█`/`░`, y
  protegida la recolocación del cursor cuando la salida está redirigida.
- **Mensajes de error** que mencionan el paso
  `playwright install-deps firefox` de Linux.

El binario de Firefox y sus dependencias del sistema son el único requisito
específico del sistema operativo. En .NET, Playwright incluye automáticamente
los drivers propios de Windows, Linux y macOS a través del paquete NuGet.

## 8. Compilar, ejecutar y publicar

```bash
# Compilación de desarrollo
dotnet build -c Release

# Ejecución dependiente del framework
dotnet bin/Release/net10.0/WindowsIsoDownloader.dll

# Publicación autocontenida en una carpeta (ejemplo: Linux x64)
dotnet publish -c Release -r linux-x64 --self-contained true

# Publicación autocontenida en un único archivo
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true
```

`config.json` se copia a la carpeta de salida (`CopyToOutputDirectory=Always`).
En tiempo de ejecución la herramienta busca primero `config.json` junto al
ejecutable y, si no lo encuentra, en el directorio de trabajo actual.

## 9. Códigos de salida

| Código | Significado |
| --- | --- |
| `0` | ISO descargada correctamente. |
| `-1` | Error de configuración, fallo de Playwright/navegador, rechazo anti-bot o error de E/S de archivos. |

## 10. Lista de mantenimiento

Si la herramienta deja de encontrar el enlace:

1. Abre la [página de descarga](https://www.microsoft.com/en-us/software-download/windows11)
   e inspecciona `#product-edition`; actualiza el `Values` de `Order: 2`.
2. Confirma la edición y luego inspecciona `#product-languages`; actualiza el
   `Values` de `Order: 4` con el valor JSON completo de tu idioma.
3. Verifica el selector final (por defecto `#card-info-content a`) contra el
   ancla de descarga generada.
4. Si la página se reestructuró, ajusta la secuencia de acciones/selectores.
5. Actualiza periódicamente el paquete NuGet de Playwright para que las
   compilaciones del navegador estén al día y pasen las comprobaciones de
   compatibilidad del sitio.

## 11. Notas de seguridad y legales

- La herramienta descarga únicamente las ISO públicas y oficiales de Microsoft;
  no elude ninguna licencia.
- Las URL de descarga firmadas caducan; se usan de inmediato y nunca se
  almacenan.
- Nunca incluyas un token de acceso personal (PAT) de GitHub en archivos de
  código fuente; usa un credential helper o una variable de entorno.
