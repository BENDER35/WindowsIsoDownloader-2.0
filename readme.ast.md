# Descargador d'ISO de Windows 10

Windows 10 ISO Downloader ye una ferramienta pequeña multipataforma que descarga
de forma programática la última **ISO oficial de Windows 10** direutamente de los
servidores de Microsoft. Controla un navegador Firefox real (en mou headless) con
[Playwright](https://playwright.dev/dotnet/) pa recorrer la páxina de descarga, recueye
l'enllaz temporal de descarga y darréu tresmite la ISO al discu con una barra de
progresu en vivu (tamañu, velocidá y tiempu restante). Les descargues son
**reanudables** —una execución atayada continúa dende un ficheru `.part`— y la
ferramienta ufierta una pequeña **interfaz de llinia de comandos** pa automatizar.

Resulta útil en proyeutos que construyen instaladores personalizaos de Windows 10
(por exemplu [`tiny11builder`](https://github.com/ntdevlabs/tiny11builder)), onde los pasos
manuales del navegador son la única parte que nun se puede automatizar.

El fluxu de trabayu ta basáu en datos (mira [`config.json`](#configuración-configjson)), asina que
la mesma ferramienta tamién puede baxar Windows 11 o otra edición camudiando unos pocos
valores; la configuración incluyida apunta a la **Windows 10 (multi-edition ISO)**.

> **Llingua:** [Asturianu](readme.ast.md) | [English](readme.md) | [Español](readme.es.md)
> **Guía téunica:** [English](docs/TECHNICAL_GUIDE.md) | [Español](docs/TECHNICAL_GUIDE.es.md)

> **Estáu del soporte de Windows 10:** el soporte ordinariu terminó el
> **14 d'ochobre de 2025**; el **ESU** de consumidores llega hasta'l
> **12 d'ochobre de 2027** y el ESU comercial/educativu hasta'l
> **10 d'ochobre de 2028**. Windows Update tamién dexará de funcionar en
> **Windows 7 hasta Windows 10 ensin ESU** cuando se roten los certificaos del
> serviciu d'actualizaciones (mayu/xunu de 2027). Mira [Fin del soporte de Windows 10 y actualizaciones de seguridá extendíes](#fin-del-soporte-de-windows-10-y-actualizaciones-de-seguridá-extendíes).

## Requisitos

- SDK de **.NET 10** (pa compilar) o'l runtime de .NET 10 (pa executar una compilación publicada).
- Accesu a Internet a `microsoft.com` y `software.download.prss.microsoft.com`.
- **Namái Linux:** les llibreríes del sistema que rique la compilación de
   Firefox de Playwright. Instálales con:
  ```bash
  ./bin/Release/net10.0/playwright install-deps firefox
  ```
   (riquiere `sudo`; en Debian/Ubuntu l'script usa `apt-get`).

Nun fai falta instalar nengún navegador manualmente: la ferramienta baxa la
compilación de Firefox de Playwright correspondiente la primer vez que s'executa.

## Compilación

```bash
dotnet build -c Release
```

El resultáu de la compilación queda en `bin/Release/net10.0/`.

La solución inclúi amás un proyeutu de pruebes xUnit (reanudación de descargues,
llectura de configuración, llinia de comandos, seleición d'enllaz, validación d'ISO, …):

```bash
dotnet test -c Release
```

Pa xenerar un executable autocontenido pa una plataforma concreta:

```bash
# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true

# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true
```

### 32 bits (x86) y otres arquitectures

El proyeutu compílase como **AnyCPU** con `Prefer32Bit` desactiváu, asina qu'un
mesmu ensambláu xestionáu funciona en runtimes de 32 y 64 bits; en Windows de
64 bits el procesu por defectu ye de 64 bits. Compilaciones adicionales:

```bash
# 32-bit Windows (x86)
dotnet publish -c Release -r win-x86 --self-contained true -p:PlaywrightPlatform=win

# 64-bit ARM Linux
dotnet publish -c Release -r linux-arm64 --self-contained true
```

El flag `-p:PlaywrightPlatform=win` indica a Playwright qué controlador incluyir: pa
un identificador de runtime non reconocíu como `win-x86`, de lo contrario copiaría'l
controlador del navegador de **toles** les plataformes (unos 575 MB).

> **Importante:** el trabayu pesáu lu fai'l motor del navegador. Playwright namái
> distribúi un controlador de **64 bits** (`win32_x64`, `linux-x64`, `linux-arm64`,
> macOS). Por eso una compilación `win-x86` funciona nuna instalación de Windows
> de 64 bits (el procesu de 32 bits arranca'l controlador de 64 bits incluyíu),
> pero nun puede executase nun sistema Windows puramente de 32 bits. Del mesmu
> mou, .NET 10 nun tien runtime Linux x86 de 32 bits (`linux-x86`) y Playwright
> nun tien controlador pa ARM de 32 bits, polo qu'una compilación `linux-arm`
> compila pero'l navegador nun puede arrancar. Mira la [guía téunica](docs/TECHNICAL_GUIDE.md#81-32-bit-x86-and-arm-support)
> pa ver la tabla completa.

## Execución

La ferramienta llee `config.json` de la mesma carpeta que l'executable (si
nun lu atopa, usa'l directoriu de trabayu actual).

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

La ISO escríbese na carpeta configurada en `config.json` (por defectu'l
directoriu de trabayu actual, xenerando `./windows10.iso`).

Si la ISO yá esiste, la ferramienta termina de momentu; usa `--force` pa
volver baxala. Si la execución s'ataya (corte de rede, `Ctrl+C`), los datos
parciales guárdense en `windows10.iso.part` y la siguiente execución retoma dende ellí.

### Opciones de llinia de comandos

| Opción | Descripción |
| --- | --- |
| `-c`, `--config <path>` | Usa un ficheru `config.json` concretu. |
| `-o`, `--output <path>` | Ruta completa de la ISO (anula `DownloadFolder`/`DownloadFilename`). |
| `-f`, `--force` | Sobrescribe una ISO esistente. |
| `--dry-run` | Llogra l'enllaz de descarga pero nun descarga. |
| `--arch <x64\|x86\|arm64\|any>` | Arquitectura d'imaxe preferida (por defectu `x64`). |
| `--headed` | Muestra la ventana del navegador (útil pa depurar). |
| `--headless` | Fuercia'l mou headless (por defectu). |
| `--slow-mo <ms>` | Ralentiza les aiciones del navegador el retardu indicáu. |
| `--list-languages` | Amuesa les llingües disponibles pa la edición configurada y sale. |
| `--experimental-sentinel` | Calentamientu best-effort del anti-bot de Microsoft. |
| `-v`, `--verbose` | Rexistru detalláu. |
| `-h`, `--help` | Amuesa l'ayuda. |

```bash
# List the languages available for the configured edition
./WindowsIsoDownloader --list-languages

# Download the 32-bit image to a specific path
./WindowsIsoDownloader --arch x86 -o /home/user/isos/windows10.iso

# Only check that a download link can be obtained
./WindowsIsoDownloader --dry-run
```

## Configuración (`config.json`)

Tol fluxu ta basáu en datos, asina que si Microsoft cambia la so páxina
namái tienes que editar `config.json` en llugar del códigu fonte.

| Campu | Descripción |
| --- | --- |
| `DownloadFolder` | Carpeta onde se guarda la ISO. Acéptense rutes de Windows (`C:\\isos`) y de Linux (`/home/user/isos`). Una ruta relativa (por defectu `.`) resuélvese respectu al directoriu de trabayu. |
| `DownloadFilename` | Nome del ficheru ISO resultante. Debe ser un nome simple (ensin separadores de ruta). |
| `PreferredArchitecture` | Imaxe preferida cuando la páxina ufierta varies: `X64` (por defectu), `X86`, `Arm64` o `Any`. |
| `Attempts` | Númberu de veces que s'intenta tol fluxu (por defectu `3`). |
| `RetryDelaySeconds` | Retardu base entre intentos, en segundos; crez de forma esponencial y llímase a 60 s (por defectu `5`). |
| `Headless` | Executa'l navegador ensin ventana visible (por defectu `true`). |
| `ExperimentalSentinel` | Activa'l calentamientu anti-bot esperimental (por defectu `false`). |
| `Actions` | Llista ordenada d'aiciones del navegador p'algamar l'enllaz de descarga. |

Cada aición tien:

| Campu | Descripción |
| --- | --- |
| `Order` | Orde d'execución (ascendente). |
| `Kind` | Unu de `Goto`, `SelectOption`, `Click`, `QuerySelector`, `WaitForSelector`. |
| `WaitBeforeAction` | Retardu opcional en **milisegundos** enantes d'executar la aición. |
| `Optional` | Cuando ye `true`, un fallu (por exemplu, l'elementu nun esiste) rexístrase y el fluxu sigue (por defectu `false`). |
| `Timeout` | Tiempu máximu opcional de la aición, en milisegundos. |
| `Parameters` | Parámetros propios de la aición (`Url`, `Selector`, `Values`). |

Aiciones soportaes:

- **`Goto`** – navega a `Parameters.Url`.
- **`SelectOption`** – selecciona `Parameters.Values` nel elementu que
   coincide con `Parameters.Selector`.
- **`Click`** – fai clic nel elementu que coincide con `Parameters.Selector`.
- **`QuerySelector`** – guarda l'elementu que coincide con `Parameters.Selector`; la
   ferramienta llee darréu'l so `href` pa llograr l'enllaz de la ISO.
- **`WaitForSelector`** – espera a que l'elementu que coincide con
   `Parameters.Selector` s'añada al DOM.

`config.json` declara `"$schema": "./config.schema.json"`, de modo que los
editores con soporte d'esquemes JSON ufierten autocompletáu y validación

### Actualizar los valores de les opciones

Microsoft camuda periódicamente los identificadores internos de la edición y de la
llingua. Pues llograr los valores actuales directamente na páxina:

1. Abre la [páxina de descarga de la ISO de Windows 10](https://www.microsoft.com/en-us/software-download/windows10ISO).
2. Abre les ferramientes de desarrollu del navegador.
3. Id d'edición: inspecciona l'elementu `<select>` `#product-edition`. El valor de la
   opción "Windows 10 (multi-edition ISO)" va na aición `SelectOption` con
   `Order: 3`.
4. Llingua: escueye la edición, pulsa **Confirm** y darréu inspecciona el
   `<select>` `#product-languages`. Copia'l `value` completu de la to llingua
   (tien la forma `{"id":"16067","language":"English"}`) na aición
   `SelectOption` con `Order: 5`.

La ferramienta ye resistente: dempués d'executar les aiciones tamién busca
cualquier enllaz `a[href*=".iso"]` na páxina, polo que pequeños camudos nel
marcáu normalmente nun la rompen. Cuando Microsoft ufierta más d'una imaxe
—Windows 10 publica una ISO de **32 bits** y otra de **64 bits**— la ferramienta
escueye automáticamente la que coincide con `PreferredArchitecture` (la de
**64 bits** por defectu; usa `--arch x86` pa la de 32 bits).

### Llingües disponibles

La edición **Windows 10 (multi-edition ISO)** (`2618`) ufierta les siguientes 38
llingües. Pa descargar una d'elles, sustitúi'l `Values` de la aición
`SelectOption` con `Order: 5` en `config.json`:

| Llingua | `id` | `Values` (pega esto) |
| --- | --- | --- |
| Arabic | `16058` | `{"id":"16058","language":"Arabic"}` |
| Brazilian Portuguese | `16059` | `{"id":"16059","language":"Brazilian Portuguese"}` |
| Bulgarian | `16060` | `{"id":"16060","language":"Bulgarian"}` |
| Chinese (Simplified) | `16061` | `{"id":"16061","language":"Chinese (Simplified)"}` |
| Chinese (Traditional) | `16062` | `{"id":"16062","language":"Chinese (Traditional)"}` |
| Croatian | `16063` | `{"id":"16063","language":"Croatian"}` |
| Czech | `16064` | `{"id":"16064","language":"Czech"}` |
| Danish | `16065` | `{"id":"16065","language":"Danish"}` |
| Dutch | `16066` | `{"id":"16066","language":"Dutch"}` |
| English | `16067` | `{"id":"16067","language":"English"}` |
| English International | `16068` | `{"id":"16068","language":"English International"}` |
| Estonian | `16069` | `{"id":"16069","language":"Estonian"}` |
| Finnish | `16070` | `{"id":"16070","language":"Finnish"}` |
| French | `16071` | `{"id":"16071","language":"French"}` |
| French Canadian | `16072` | `{"id":"16072","language":"French Canadian"}` |
| German | `16073` | `{"id":"16073","language":"German"}` |
| Greek | `16074` | `{"id":"16074","language":"Greek"}` |
| Hebrew | `16075` | `{"id":"16075","language":"Hebrew"}` |
| Hungarian | `16076` | `{"id":"16076","language":"Hungarian"}` |
| Italian | `16077` | `{"id":"16077","language":"Italian"}` |
| Japanese | `16078` | `{"id":"16078","language":"Japanese"}` |
| Korean | `16079` | `{"id":"16079","language":"Korean"}` |
| Latvian | `16080` | `{"id":"16080","language":"Latvian"}` |
| Lithuanian | `16081` | `{"id":"16081","language":"Lithuanian"}` |
| Norwegian | `16082` | `{"id":"16082","language":"Norwegian"}` |
| Polish | `16083` | `{"id":"16083","language":"Polish"}` |
| Portuguese | `16084` | `{"id":"16084","language":"Portuguese"}` |
| Romanian | `16085` | `{"id":"16085","language":"Romanian"}` |
| Russian | `16086` | `{"id":"16086","language":"Russian"}` |
| Serbian Latin | `16087` | `{"id":"16087","language":"Serbian Latin"}` |
| Slovak | `16088` | `{"id":"16088","language":"Slovak"}` |
| Slovenian | `16089` | `{"id":"16089","language":"Slovenian"}` |
| Spanish | `16090` | `{"id":"16090","language":"Spanish"}` |
| Spanish (Mexico) | `16091` | `{"id":"16091","language":"Spanish (Mexico)"}` |
| Swedish | `16092` | `{"id":"16092","language":"Swedish"}` |
| Thai | `16093` | `{"id":"16093","language":"Thai"}` |
| Turkish | `16094` | `{"id":"16094","language":"Turkish"}` |
| Ukrainian | `16095` | `{"id":"16095","language":"Ukrainian"}` |

> Estos valores `id` pertenecen a la edición de Windows 10 `2618` (la aición
> `SelectOption` con `Order: 3`).
> Les llingües que puedas echar en falta (por exemplu indonesiu, malayu, persa o
> vietnamita) nun s'ufierten pa esta edición. La opción más a prueba del futuru
> sigue siendo copiar el `value` completu de la páxina, como se describió enantes.

## Fin del soporte de Windows 10 y actualizaciones de seguridá extendíes

Windows 10 algamó el **fin del soporte el 14 d'ochobre de 2025**: yá nun recibe
actualizaciones de seguridá gratuites y namái'l programa **Extended Security
Updates (ESU)** lo mantién parcheáu.

| Etapa | Termina | Aplícase a |
| --- | --- | --- |
| Soporte ordinariu | **14 d'ochobre de 2025** | Toos |
| ESU – Añu 1 | **13 d'ochobre de 2026** | Comercial / educativu |
| ESU – Añu 2 | **12 d'ochobre de 2027** | Consumidores (fin del ESU de consumidores); Añu 2 pa comercial |
| ESU – Añu 3 | **10 d'ochobre de 2028** | Namái comercial / educativu |

- Los **consumidores** puen inscribise nel ESU —gratis nel Espaciu
  Económicu Européu y con una cuenta de Microsoft / puntos de Microsoft Rewards
  nel restu— y tán cubiertos hasta'l **12 d'ochobre de 2027**.
- Los clientes **comerciales / educativos** puen mercar hasta **tres años** de
  ESU: l'Añu 1 termina'l 13 d'ochobre de 2026, l'Añu 2 el 12 d'ochobre de
  2027 y l'Añu 3 el **10 d'ochobre de 2028**.
- L'ESU contién **namái** actualizaciones de seguridá crítiques ya importantes;
  nenguna carauterística nueva, correición non de seguridá nin cambéu de diseñu.

> **Alvertencia — rotación de certificaos de Windows Update.** Los
> certificaos que protexen la conexón con Windows Update caduquen en **2027**
> (los sos reemplazos deben instalarse **enantes del 17 de mayu de 2027** o **enantes
> del 19 de xunu de 2027**, según el productu). Los equipos que nunca reciban
> los certificaos de reemplazu —**Windows 7, Windows 8 / 8.1, cualquier
> instalación de Windows 10 ensin ESU** y les versiones de Windows Server fuera de
> soporte— **perderán l'accesu a Windows Update** y dexarán de recibir
> actualizaciones dafechu. Ye una rotación de los **certificaos raíz** del
> serviciu y nun tien rellación con esta ferramienta. Pa siguir actualizáu
> fai falta Windows 10 **con ESU** (instalar la actualización mensual, polo menos
> la de xunetu de 2026, enantes del 19 de xunu de 2027) o un sistema
> operativu soportáu (Windows 11 24H2 o posterior, Windows Server 2022 o
> posterior, o les versiones LTSC que Microsoft indica).
> Anunciu oficial:
> <https://techcommunity.microsoft.com/blog/windows-itpro-blog/prepare-for-windows-update-certificate-rotation-in-2027/4562463>

## Códigos de salida

| Códigu | Significáu |
| --- | --- |
| `0` | Éxitu (ISO baxada, o `--dry-run`/llistáu de llingües termináu). |
| `1` | Error inesperáu (llinia de comandos incorrecta). |
| `2` | Error de configuración o d'estructura de la páxina (nun s'atopó l'enllaz). |
| `3` | Error de rede mientres la descarga. |
| `4` | Refuga anti-bot (Sentinel). |
| `5` | Error de discu (permisos, E/S). |
| `6` | Error del navegador (Firefox nun pudo arrancar). |
| `130` | Canceláu con `Ctrl+C`. |

## Resolución de problemes

- **`Could not launch Firefox` en Linux** – instala les dependencies del sistema
  de Playwright: `playwright install-deps firefox` (mira *Requisitos*).
- **`Download button was not found` / anti-bot** – Microsoft protexe'l
  conector de descarga con un mecanismu llamáu *Sentinel* que pue refugar una
  sesión del navegador, sobremanera con usu intensivu o automatizáu. La
  ferramienta reintenta hasta `Attempts` veces (3 por defectu) con una sesión
  nueva y un retardu creciente; si sigue fallando, espera unos minutos y
  inténtalo de nuevu. Executar con `--headed` y `--verbose` amuesa lo que fai la páxina.
- **La descarga s'atayó** – nun se pierde nada: los datos parciales queden
  en `*.part` y la siguiente execución los retoma con una petición HTTP `Range`.
  Borra'l ficheru `.part` pa empezar de cero.
- **Errores de permisos** – asegúrate de que `DownloadFolder` se pueda escribir. En Windows
  executa la terminal como alministrador; en Linux revisa los permisos de la carpeta.
- **La páxina camudó** – usa `--dry-run --headed` pa ver l'automatización, guarda
  una captura/HTML de diagnósticu (escríbense en `diagnostics/` cuando una execución falla)
  y axusta `config.json` en consecuencia.

## Llicencia

Mira [LICENSE](LICENSE).




## Zarru de Windows Update — Notes práutiques

> Notes personales recoyíes pol mantenedor el 12 d'ochobre de 2026.
> Complementen el resumen oficial de riba con soluciones práutiques y les sos
> contrapartíes; nun son indicaciones oficiales de Microsoft.

ATENCIÓN, AVISU (12 d'ochobre de 2026): microsoft va cortar a partir del 19 de
xunu de 2027 l'accesu a windows update a tolos equipos que tengan windows 10
sin ESU y anteriores (el bloqueu anterior foi hasta windows vista-server 2008
sin R2), esto lóxicamente afecta a les versiones de servidor (server 2008 R2 hasta
server 2019, a partir de server 2022, el zarru de windows update nun los afecta),
esto ye pola caducidá de los certificaos raíz del serviciu d'actualizaciones,
que caduquen a partir d'esta fecha; si quies echar un vistazu al enllaz oficial
de la noticia de microsoft, equí lo tienes (usa la traducción del navegador, que ta n'inglés):
English):

https://techcommunity.microsoft.com/blog/windows-itpro-blog/prepare-for-windows-update-certificate-rotation-in-2027/4562463

recuerda que los certificaos raíz formen parte de la navegación por internet; una
de les carauterístiques del protocolu https ye que anguaño too lo que se tresmite
al router va cifrao (enantes nun yera asín), pero pa los programes usaos pa
analizar too lo que circula nuna rede doméstica, como wireshark, nun tengan efeutu
(nun se vea nengún datu sensible), los certificaos raíz tienen que tar bien y al día.
Si nun funcionan, da un error de certificáu nel navegador y los programes que se
conecten a internet dexen de funcionar. Tienes que tener esto en cuenta al usar un
windows antiguu instalao nel ordenador (por cierto, el zarru de windows update
tamién afecta a les máquines virtuales, ye dicir, a los PCs arrancaos dientro d'un
equipu físicu).

cuando llegue la fecha de caducidá del certificáu raíz, ye posible que los office
instalaos en windows ensin soporte den errores al comprobar actualizaciones (hasta
el 10 y server 2019, que son veceru y servidor respeutivamente, pues instalar el
2021-365, el 7 y el 8, hasta office 2016); esto resuélvese desactivando les
actualizaciones d'office (cuando llegue la fecha de caducidá d'esos certificaos raíz).

na ventana anterior (les actualizaciones automátiques impunxéronse a partir de
windows 2000) nun daba nengún fallu al arrancar el sistema operativu, namái
al abrir la páxina de microsoft update o al iniciar windows update.
(yo pasé pela ventana anterior de zarru de les actualizaciones).

nel casu de que seya windows 10 con ESU, windows 11 24H2, los servidores LTSC de
2019 y 2016 y server 2022, microsoft puso como métodu oficial pa resolver el
problema la actualización, ye dicir, tendrás que actualizar el segundu martes de
cada mes hasta'l 19 de xunu de 2027, en que yá... pasu a pasu (entenderás lo que
te digo).

si te topes con esti tipu d'equipos, estes son les víes que tienes:

-  mercar el ESU (windows 10) (nel casu de países de la unión europea, nun tien nengún costu)
-  parchear l'actualización pa acceder al ESU; l'únicu parche (y recomendáu pa windows 10) ye MAS (microsoft activation script)
-  instalar un parche non oficial pa actualizar los certificaos raíz, que ye legacy update
-  nel casu de versiones compatibles (anguaño nun ye obligatorio a partir de 11 24H2; pa windows 10 ensin ESU ye
   cuasi imprescindible) aceptar les actualizaciones a versiones mayores de windows per mediu d'update o facer una
   actualización dende los soportes d'instalación de windows.
-  caltener la instalación de windows y facer un dual boot con linux (nun ye aconsellable).
-  desinstalar windows ya instalar linux xunto con una máquina virtual de windows (equí tán permitees toles opciones, dende orixinales hasta modificaes)

si escueyes la opción sensata (la postrera), recuerda que tienes que montar
vmware o virtualbox en linux (instala linux nel ordenador, lóxicamente)
y la máquina virtual col windows que quieras (equí sí que puedes instalar
windows antiguos) de forma oficial hasta xunu de 2027; darréu nun va funcionar
y namái vas poder instalar y actualizar esti tipu de PCs por métodos
non oficiales.

si perdisti'l trabayu o'l cursu por haberte dichu esti
inconveniente, muncha suerte na to busca d'empleu o de cursu (espero que
lo atopes); con toles estes pistes que te digo, yá verás como de xuru al final
atopa'l trabayu o'l cursu que suañabas.
