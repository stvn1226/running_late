# Integración de Steam con Facepunch.Steamworks

**Estado:** 🔴 Bloqueado — falta `libsteam_api.dylib` (librería nativa de Valve)  
**Biblioteca:** [Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks)  
**App ID configurado:** `4724080`  
**Acceso a cuenta Steamworks Partner:** ❌ No disponible — solo se proporcionó el App ID

---

## 🔴 Bloqueante actual — Falta `libsteam_api.dylib`

El código está completo y el App ID está configurado, pero el SDK de Facepunch requiere una librería nativa de Valve (`libsteam_api.dylib` en Mac, `steam_api64.dll` en Windows) que **no viene incluida en los DLLs de Facepunch.Steamworks**.

Esta librería solo se obtiene del **Steamworks SDK oficial de Valve**, que requiere acceso a la cuenta de Steamworks Partner.

### Error actual en consola
```
[STEAM] Steam init failed: libsteam_api assembly:<unknown assembly> type:<unknown type> member:(null)
```

### Cómo resolver (necesita acceso a la cuenta de Steamworks Partner)

1. Iniciar sesión en [partner.steamgames.com](https://partner.steamgames.com/)
2. Descargar el **Steamworks SDK** desde la sección de Downloads
3. Del archivo ZIP, copiar los siguientes archivos al proyecto:

| Archivo en el SDK | Destino en Unity | Plataforma |
|---|---|---|
| `sdk/redistributable_bin/osx/libsteam_api.dylib` | `Assets/Plugins/Steamworks/` | Mac |
| `sdk/redistributable_bin/win64/steam_api64.dll` | `Assets/Plugins/x86_64/` | Windows 64-bit |
| `sdk/redistributable_bin/steam_api.dll` | `Assets/Plugins/x86/` | Windows 32-bit |

4. Volver a Unity — los archivos se importarán automáticamente
5. Darle Play y verificar el log: `[STEAM] Steam initialized OK`

> [!IMPORTANT]
> El `steam_appid.txt` con el App ID `4724080` ya está en la raíz del proyecto.
> El `SteamManager.cs` ya tiene todo el código listo.
> Solo faltan los archivos nativos de Valve.

---

## Estado actual de la integración

| Componente | Estado |
|---|---|
| DLLs de Facepunch.Steamworks | ✅ Instalados y configurados |
| `SteamManager.cs` | ✅ Completo |
| App ID `4724080` | ✅ Configurado |
| `steam_appid.txt` en raíz del proyecto | ✅ Creado |
| Script Execution Order | ✅ Configurado (-100) |
| `GameAnalytics.SetCustomId()` | ✅ Activado |
| `libsteam_api.dylib` (Mac) | ❌ **Falta — requiere cuenta Steamworks** |
| `steam_api64.dll` (Windows) | ❌ **Falta — requiere cuenta Steamworks** |

---

## Paso 1 — Instalar Facepunch.Steamworks en Unity

### Opción A: Via NuGet / DLL (recomendada para Unity)

1. Ve a la página de [Releases en GitHub](https://github.com/Facepunch/Facepunch.Steamworks/releases)
2. Descarga el archivo `.zip` de la versión más reciente (busca `Facepunch.Steamworks.Unity.zip` o similar)
3. Extrae y copia los `.dll` a `Assets/Plugins/Steamworks/`

Los archivos que necesitas son tres DLL y tienes que configurarlos por plataforma en Unity:

| DLL | Any Platform | Plataformas incluidas | CPU |
|---|---|---|---|
| `Facepunch.Steamworks.Posix.dll` | ❌ | Editor + Standalone | Any (OS: OSX + Linux) |
| `Facepunch.Steamworks.Win32.dll` | ✅ | (ninguna extra) | x86 (Windows) |
| `Facepunch.Steamworks.Win64.dll` | ✅ | (ninguna extra) | x86_64 (Windows) |

> En el Inspector de Unity, selecciona cada `.dll` y configura las plataformas en el tab "Plugin Inspector".

---

## Paso 2 — Crear el archivo `steam_appid.txt`

Steam necesita este archivo en la misma carpeta que el ejecutable `.exe` / `.app` para pruebas locales.

Crea un archivo de texto llamado exactamente `steam_appid.txt` (sin extensión adicional) con **solo tu App ID**:

```
TU_APP_ID_AQUÍ
```

Por ejemplo, si usas el App ID de prueba de Valve:
```
480
```

**Ubicación en Unity Editor:** colócalo en la raíz del proyecto (junto al `Assets/`).  
**Ubicación en builds:** debe estar junto al `.exe` o `.app`.

---

## Paso 3 — Crear el SteamManager

Crea el archivo `Assets/Scripts/Steam/SteamManager.cs`:

```csharp
using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }
    public static bool IsInitialized { get; private set; }

    // Reemplaza con tu App ID real cuando lo tengas
    private const uint APP_ID = 480; // ← CAMBIAR ESTO

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            SteamClient.Init(APP_ID, asyncCallbacks: true);
            IsInitialized = true;
            GameLogger.Log("STEAM", $"Steam initialized. Player: {SteamClient.Name} | SteamID: {SteamClient.SteamId}");
        }
        catch (System.Exception e)
        {
            IsInitialized = false;
            GameLogger.LogWarning("STEAM", $"Steam init failed (Steam not running?): {e.Message}");
        }
    }

    private void Update()
    {
        // Facepunch necesita que se procesen los callbacks manualmente si asyncCallbacks=false
        // Si usas asyncCallbacks=true (recomendado para Unity), puedes quitar esto.
        // SteamClient.RunCallbacks();
    }

    private void OnDestroy()
    {
        if (IsInitialized)
        {
            SteamClient.Shutdown();
        }
    }

    /// <summary>
    /// Devuelve el SteamID del jugador como string, o null si Steam no está disponible.
    /// </summary>
    public static string GetSteamId()
    {
        if (!IsInitialized) return null;
        return SteamClient.SteamId.ToString();
    }

    /// <summary>
    /// Devuelve el nombre de usuario de Steam, o null si Steam no está disponible.
    /// </summary>
    public static string GetPlayerName()
    {
        if (!IsInitialized) return null;
        return SteamClient.Name;
    }
}
```

---

## Paso 4 — Agregar SteamManager a la escena

1. En `MainMenu.unity`, crea un GameObject vacío llamado `SteamManager`
2. Añádele el script `SteamManager.cs`
3. Como es `DontDestroyOnLoad`, persistirá a todas las escenas

> [!WARNING]
> El `SteamManager` debe inicializarse **antes** que el `AnalyticsManager` para que el SteamID esté disponible.  
> Asegúrate de que el GameObject de Steam tenga mayor **Script Execution Order** en Project Settings → Script Execution Order.

---

## Paso 5 — Conectar SteamID con GameAnalytics

Una vez que el `SteamManager` esté funcionando, el `AnalyticsManager` ya está preparado para recibir el ID. Sólo debes llamar a esto después de que ambos estén inicializados.

En `AnalyticsBootstrap.cs` o en el `Start()` del `AnalyticsManager`, añade:

```csharp
// Conectar SteamID con GameAnalytics si Steam está disponible
if (SteamManager.IsInitialized)
{
    string steamId = SteamManager.GetSteamId();
    AnalyticsManager.Instance.SetUserProperty("id", steamId);
    GameLogger.Log("ANALYTICS", $"SteamID enviado a GameAnalytics: {steamId}");
}
```

> [!NOTE]
> En GameAnalytics, `SetCustomId()` debe llamarse **antes** de `GameAnalytics.Initialize()`.  
> Esto significa que la inicialización del `SteamManager` debe ocurrir antes de que el prefab de GameAnalytics haga su `Awake()`.  
> La solución más limpia es usar **Script Execution Order** para que Steam corra primero.

---

## Paso 6 — Logros de Steam (opcional, para más adelante)

Con Facepunch.Steamworks, los logros se gestionan así:

```csharp
// Desbloquear un logro
SteamUserStats.AddStat("ACH_FIRST_COMPLETE", 1);
await SteamUserStats.StoreStatsAsync();

// O más simple:
var ach = new Steamworks.Data.Achievement("ACH_FIRST_COMPLETE");
ach.Trigger();
```

Los IDs de los logros (ej: `"ACH_FIRST_COMPLETE"`) los defines tú en el panel de Steamworks en la web.

---

## Checklist de implementación

- [ ] Obtener Steam App ID de Steamworks Partner
- [ ] Descargar e instalar DLLs de Facepunch.Steamworks
- [ ] Configurar los 3 DLLs por plataforma en el Inspector de Unity
- [ ] Crear `steam_appid.txt` en la raíz del proyecto
- [ ] Reemplazar `APP_ID = 480` con el App ID real en `SteamManager.cs`
- [ ] Crear GameObject `SteamManager` en `MainMenu.unity`
- [ ] Ajustar Script Execution Order (Steam antes que Analytics)
- [ ] Conectar SteamID con GameAnalytics en el bootstrap
- [ ] Probar build con Steam abierto en el mismo PC
- [ ] Definir logros en el panel de Steamworks (si aplica)

---

## Notas importantes

- **El juego debe lanzarse desde Steam** (o con `steam_appid.txt` presente) para que `SteamClient.Init()` funcione. Si se lanza de otra forma, fallará silenciosamente.
- **Para la versión de itch.io**, el bloque de init fallará (Steam no estará presente) y el juego seguirá funcionando normalmente gracias al `try/catch`.
- **Para WebGL**: Steamworks no es compatible con WebGL. El `SteamManager` simplemente fallará en init y no afectará nada.
