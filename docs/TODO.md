# Running Late — TODO de lanzamiento

Última actualización: Julio 2026

---

## ✅ Analytics (GameAnalytics)

- [x] Arquitectura de analytics creada (`IAnalyticsProvider`, `AnalyticsManager`, `AnalyticsConfig`)
- [x] `GameAnalyticsProvider` implementado y conectado
- [x] SDK instalado vía OpenUPM en `manifest.json`
- [x] Claves configuradas por plataforma (Windows, Mac, WebGL)
- [x] Prefab de GameAnalytics en `MainMenu.unity`
- [x] Eventos de progresión: `TrackLevelStart`, `TrackLevelComplete`, `TrackLevelFail`
- [x] Parámetros enriquecidos: `seed`, `level_type` (DualPath/SingleSidewalk), `completion_time`, `best_time`
- [x] Eventos de acción del jugador: jump, slide, wallrun, kick, stomp
- [x] Eventos de UI: pause, resume
- [x] Verificado que llegan eventos al dashboard de GameAnalytics ✅

---

## 🔧 Steam / Facepunch.Steamworks

- [x] Guía de integración documentada en `docs/steamworks_integration.md`
- [x] DLLs de Facepunch.Steamworks descargados
- [x] **DLLs copiados al proyecto Unity** → `Assets/Plugins/Steamworks/`
- [x] **Configurar los DLLs en Unity** (por plataforma: Posix=Mac/Linux/Editor, Win32=x86, Win64=x64)
- [x] `SteamManager.cs` creado en `Assets/Scripts/Steam/`
- [x] GameObject `SteamManager` agregado a la escena con el script asignado
- [x] **Script Execution Order** configurado (`SteamManager` = -100, antes que `AnalyticsManager`)
- [x] **Steam App ID** — `4724080` configurado
- [x] `steam_appid.txt` en la raíz del proyecto
- [x] `APP_ID = 4724080` en `SteamManager.cs`
- [x] `GameAnalytics.SetCustomId()` activado
- [ ] **🔴 BLOQUEADO — `libsteam_api.dylib` y `steam_api64.dll` (librerías nativas de Valve)**
  - Requieren acceso a la cuenta de Steamworks Partner
  - No disponible actualmente — solo se proporcionó el App ID
  - Quien tenga acceso a la cuenta debe descargar el **Steamworks SDK** y copiar:
    - `sdk/redistributable_bin/osx/libsteam_api.dylib` → `Assets/Plugins/Steamworks/`
    - `sdk/redistributable_bin/win64/steam_api64.dll` → `Assets/Plugins/x86_64/`
  - Ver instrucciones completas en `docs/steamworks_integration.md`

---

## 🐛 Bugs resueltos en esta sesión

- [x] `LevelSavingParams` — Error de `ScriptableObject` creado con `new` (3 archivos corregidos)
- [x] Conflicto de GUID con `ExternalDependencyManager` duplicado
- [x] Nombres inválidos en Design Events de GA (`_` → `:`)
- [x] Delegación de eventos hacia los providers corregida en `AnalyticsManager`
- [x] Error de tipo `TimeRecord` → `float` en `AnalyticsManager`

---

## 📋 Próximos pasos inmediatos (en orden)

1. **Copiar los DLLs** de Facepunch de donde los descargaste a `Assets/Plugins/Steamworks/`
2. **Configurar cada DLL** en el Inspector de Unity según la tabla de la guía
3. **Conseguir el App ID** de Steam (esto requiere registrarse en Steamworks Partner)
4. Cuando tengas el App ID: seguir la guía paso a paso

---

## 📝 Pendiente / Futuro

- [ ] Logros de Steam (definirlos en el panel de Steamworks)
- [ ] Build para Windows y test de analytics en esa plataforma
- [ ] Agregar plataforma Android en GameAnalytics si se lanza en móvil
- [ ] Relleno de nubes en niveles (aviso de `Cannot generate clouds`)
- [ ] Renombrar el juego "Running Late - Editor" en GA dashboard a "Running Late - Mac"
