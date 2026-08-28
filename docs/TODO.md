# Running Late — Roadmap & Lista de Tareas

Última actualización: Agosto 2026

---

## 🎯 Tareas Activas / Por Hacer

### 1. 🕹️ Tutorial / Overlay Interactivo de Controles (En progreso - A medio camino)
- [x] Construido el overlay visual utilizando el arte dedicado en capas de `Assets/Sprites/ControlsOverlay/` (`Controller Front/` y `Keyboard and Mouse/`).
- [x] Detección y cambio dinámico entre Mando y Teclado en tiempo real según la entrada activa.
- [x] Validación y retroalimentación interactiva (los botones se iluminan y validan las 4 acciones: Moverse, Saltar, Deslizarse, Patear).
- [x] Muestra solo en el primer inicio (persistencia en `PlayerPrefs`) y botón para omitir (`SALTAR [ESC]`).
- [x] Botón "Ver Tutorial de Controles" funcional en el panel de Ajustes (`SettingsPanel.cs`).
- [ ] Pulido final de integración, animación y flujo de cierre del tutorial.

### 2. 🌱 Verificación de Seeds Específicos
- [ ] **Seed `2230417`:** Probar de inicio a fin para confirmar generación procedural, pendientes de carretera, aceras elevadas y fachadas de edificios conformes.
- [ ] **Seed `1088325`:** Probar el punto específico de la acera elevada donde ocurría el error de *staggered + fall/dive* para comprobar que el fix de `PlayerOneWayPlatform` previene la caída accidental tras el knockback.

### 3. ✨ Efectos Visuales Adicionales para Power-Ups & Partículas
- [ ] Añadir auras / partículas dedicadas en el jugador mientras un power-up esté activo (ej. estela de velocidad, chicle alrededor, etc.).
- [ ] Pulido fino de partículas en saltos, caídas y pickups.

### 4. 🏢 Fachadas de Edificios Anchos (`Wide`) — *En espera de sprites*
- [ ] Crear los sets modulares de `BuildingThemeSet` para edificios anchos (`Wide`) cuando los assets estén disponibles.

### 5. 🛣️ Alineación Grid de Carretera / Aceras — *Postergado*
- [ ] Unificar el ancho de las piezas de carretera plana (16.0) con las de rampas (19.06 / 3.38).

---

## ✅ Tareas Completadas Recientemente

- [x] **Corrección de Bug de Sidewalk / Descolgamiento al Recibir Golpe (`PlayerOneWayPlatform.cs` & `PlayerStates.cs`)**:
  - `ForceStop()` y `OnDisable()` restauran de forma incondicional todas las colisiones ignoradas (`Physics2D.IgnoreCollision(..., false)`).
  - `StartStagger()` limpia `dropDownHeld`, `isDroppingDown` y cancela el traspaso de plataformas para evitar caídas automáticas tras un knockback.
- [x] **Sincronización Total de Sets Medianos (SingleDecker / DoubleDecker) — *Reporte de Elito Resuelto***:
  - Sincronizados los 6 sets de edificios (`GroundFloor`, `MidFloor`, `Roof`), sprites actualizados y assets `SetX.asset` entre `DoubleDecker` y `SingleDecker`.
  - Configurado `minFloorsOverride` y regla de 4 pisos mínimos en `StructureBuilder.cs` para Sets altos (Cine / Policía).
  - Corregidas las pendientes de carretera (`RoadSlopeLeft_2x2.prefab` y `RoadSlopeRight_2x2.prefab`).
- [x] **Auditoría de Compatibilidad AMD (Radeon / Ryzen / Steam Deck)**:
  - Shaders validados (sin divisiones por cero ni registros no inicializados), plugins nativos x86_64 compatibles y URP 2D verificado.
- [x] **UI de Power-Ups & Notificaciones**:
  - Reducido tamaño de tarjeta, reposicionada pegada al timer y curva de desvanecimiento optimizada (1/5 sólido, 4/5 fade con `CanvasGroup`).
- [x] **Trilogía de Features Steamworks & Mobile (`feature/steamworks-features`)**:
  - **Sistema Unificado de Logros (`AchievementsManager.cs` & `AchievementNotificationUI.cs`)**: Desbloqueo en Steam, guardado local, tracking analítico y banner emergente.
  - **Steam Rich Presence (`SteamRichPresence.cs`)**: Estado dinámico de amigos en tiempo real.
  - **Steam Leaderboards Bridge (`SteamLeaderboardBridge.cs`)**: Subida asíncrona de récords por milisegundos a tablas mundiales.
