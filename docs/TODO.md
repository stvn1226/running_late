# Running Late — Roadmap & Lista de Tareas

Última actualización: Agosto 2026

---

## 🎯 Tareas Activas / Por Hacer

### 1. 🕹️ Tutorial / Overlay Interactivo de Controles (Siguiente Prioridad)
- [ ] Overlay interactivo antes del countdown inicial del nivel.
- [ ] Minijuego / preview rápido para probar las teclas y controles principales.
- [ ] Mostrar solo la primera vez que se juega (guardado en `PlayerPrefs`).
- [ ] Opción para skippear en cualquier momento.
- [ ] Enviar evento de analytic a GameAnalytics al completar o skippear.

### 2. 🏢 Orden y Ajuste de Fachadas de Edificios Medianos (`Medium`)
- [ ] Confirmar el orden exacto de los sprites para los 5 sets de edificios medianos:
  - Set 1: Cine 1
  - Set 2: Cine 2
  - Set 3: Depato
  - Set 4: Police / Koban
  - Set 5: Tea House
- [ ] Ajustar `FloorAssetPicker` / prefabs según la distribución aprobada.

### 3. ✨ Efectos Visuales Adicionales para Power-Ups & Partículas
- [ ] Añadir auras / partículas dedicadas en el jugador mientras un power-up esté activo (ej. estela de velocidad, chicle alrededor, etc.).
- [ ] Pulido fino de partículas en saltos, caídas y pickups.

### 4. 🏢 Fachadas de Edificios Anchos (`Wide`) — *En espera de sprites*
- [ ] Crear los sets modulares de `BuildingThemeSet` para edificios anchos (`Wide`) cuando los assets estén disponibles.

### 5. 🛣️ Alineación Grid de Carretera / Aceras — *Postergado*
- [ ] Unificar el ancho de las piezas de carretera plana (16.0) con las de rampas (19.06 / 3.38).

---

## ✅ Tareas Completadas Recientemente

- [x] **Centralización de Configuración de Edificios en `FloorPlan`**:
  - `allowedThemes`, `colorMaterials`, `powerUps`, `roofObstaclePrefabs`, `obstacleRate`, `powerUpRate` ahora viven en ScriptableObjects (`SlimBuildingConfig.asset`, `MediumBuildingConfig.asset`, `WideBuildingConfig.asset`).
  - SingleDecker y DoubleDecker comparten configuración sin duplicar datos en prefabs.
- [x] **Anti-Repeat en SingleDecker**: Asignado `BuildingPoolConfig.asset` a todos los módulos de SingleDecker (`Brick`, `Grass`, `Ground`).
- [x] **Escena de Pruebas Standalone (`PowerUpTesting.unity`)**:
  - Pista de pruebas plana continua desacoplada de generadores procedurales.
  - Script `PowerUpTester.cs` con atajos numéricos `1..8`, `R` para reset y panel GUI en pantalla.
- [x] **UI de Power-Ups de Alto Contraste y Animación Pop-In**:
  - Animación elástica *EaseOutBack* al aparecer o refrescar tarjeta.
  - Textos en Navy Oscuro (`#1E2438`) y cápsula con opacidad del 88% para máxima legibilidad.
  - Los 8 power-ups ahora se registran y muestran correctamente en la UI.
- [x] **Efecto de Recolección en el Mundo (*Float & Fade*)**:
  - Al recoger un power-up, se eleva suavemente y se desvanece antes de destruirse.
- [x] **Corrección de Error de Curvas de Partículas**:
  - Unificados los modos de curva de `VelocityModule` a `minMaxState: 3` en `RunDust` y `LandDust`.
- [x] **Punto de Detección de la Escuela (`School.prefab`)**:
  - `victoryDelay` optimizado a `0.1s` para disparo inmediato al llegar a la meta.
- [x] **Sorting Order Progresivo en Pisos**:
  - Implementado `sortingOrder -= floorIndex` para layering isométrico natural.
- [x] **Integración de Analytics y SteamWorks**:
  - GameAnalytics con claves por plataforma y eventos de progresión/acciones.
  - Facepunch.Steamworks con App ID `4724080`.
