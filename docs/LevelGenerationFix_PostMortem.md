# Post-Mortem: Fix Generación Procedural (Vereda y Calles)

Este documento registra los cambios realizados, los errores de interpretación cometidos por la IA durante el proceso, y la solución final aplicada a los sistemas de generación del nivel modular (`LevelGeneratorEngine.cs` y `LevelStarter.cs`).

## El Problema Original
La generación procedural de niveles tenía un bug visual masivo: cuando la calle (Road) subía a un nivel superior, la vereda (Sidewalk) subía a un nivel aún más alto y además generaba bloques negros gigantes debajo ("Padding"), formando un "Tercer Nivel" flotante que tapaba completamente los edificios y el fondo del juego.

El usuario exigía:
1. Eliminar por completo esa plataforma flotante extra.
2. Evitar que se generaran los bloques negros de padding debajo del Sidewalk.
3. Mantener una separación vertical de 1.5 unidades visuales entre el Road y el Sidewalk, sin importar si estaban planos o en rampas.
4. Que el Sidewalk tuviera subidas y bajadas exactamente igual que la calle (Shadowing).

## Historial de Errores y "Estupideces" de la IA

### Error 1: Quitar los slopes del Sidewalk (Hacer el Sidewalk 100% plano)
**El Fallo:** Al interpretar que el usuario odiaba el "Tercer Nivel", la IA asumió equivocadamente que el usuario no quería que el Sidewalk tuviera _ninguna_ variación de altura. La IA modificó `LevelGeneratorEngine.cs` (Pass 3) para forzar que `nextH = 1` siempre, sin importar lo que hiciera el Road.
**Consecuencia:** El Sidewalk quedó como una línea recta perfecta de inicio a fin. Cuando el Road subía por una rampa, la distancia se acortaba visualmente. El usuario se quejó de que la distancia se redujo a "0.5 unidades" y recordó que el Sidewalk DEBE tener subidas y bajadas.

### Error 2: Devolver el Sidewalk al Nivel 2 (Regresar el bloque gigante)
**El Fallo:** Al escuchar que el usuario quería mantener la distancia de 1.5 unidades en las subidas, la IA pensó que el usuario quería volver a habilitar la elevación dinámica. La IA hizo un `git restore` de `LevelGeneratorEngine.cs` para traer de vuelta la vieja lógica.
**Consecuencia:** ¡El código antiguo fue el que originó el problema! Al restaurar el código, el Sidewalk volvió a saltar a `Y=7.5` generando otra vez la gigantesca plataforma flotante que tapaba el fondo. El usuario mandó una captura de pantalla tachando TODO con un "NO" masivo.

### Error 3: Hardcodear Y=4.5 en LevelStarter
**El Fallo:** La IA, desesperada por mantener la distancia, modificó la posición final a `segTransformY = baseY + 4.5f` fijos en `LevelStarter.cs`. 
**Consecuencia:** Esto nuevamente ignoraba las subidas y bajadas dinámicas del nivel, destruyendo la intención original del diseño.

### Error 4: Errores de sintaxis al intentar limpiar (Mismatched braces)
**El Fallo:** Al intentar borrar la lógica fallida del "Tercer Nivel" en `LevelGeneratorEngine.cs`, la IA borró mal las llaves `{}`, rompiendo el método `Pass3_CalculateSidewalkHeights` y generando errores masivos de compilación en cascada (`CS1519`, `CS1026`).

---

## La Solución Final y Correcta

Tras entender que el Sidewalk debía **copiar exactamente** el perfil de la calle pero manteniendo siempre la distancia (shadowing), se implementaron los siguientes cambios:

### 1. En `LevelGeneratorEngine.cs` (Shadowing del Road)
- Se eliminó la lógica aleatoria que permitía que el Sidewalk "saltara" a niveles más altos que el Road (`SidewalkRelativeLevel` al azar).
- Se modificó `Pass3_CalculateSidewalkHeights` para que iterara sobre el `RoadTrack` y el Sidewalk copiara **exactamente** la misma elevación `roadH_end`.
- Si el Road es un `IsSlope`, el Sidewalk ahora inserta un segmento `IsSlope` exactamente en la misma posición X y con la misma elevación lógica.

### 2. En `LevelStarter.cs` (Posicionamiento Visual y Padding)
- Se actualizó el cálculo en `RenderSidewalkTrack` para sumar `RoadElevationAtX` y `sidewalkYOffset`, lo que garantiza que la vereda siga la calle y se desplace +1.5 unidades en el eje Y de manera perfecta y constante.
- Se fijó `absH = 1` en el método `SpawnModularSlope` y `SpawnModularSegment` exclusivo para el Sidewalk y las Rampas Diagonales. 
- **¿Por qué `absH = 1`?:** `ModuleBuilder.cs` usa `absH` para contar cuántos bloques de `groundBlocksCount` (Padding) debe dibujar. Al forzarlo a `1`, `ModuleBuilder` desactiva completamente la generación del bloque negro ("Padding"), haciendo que el Sidewalk flote de forma limpia sin tapar el arte del fondo.

## Conclusión
La combinación de **Shadowing lógico exacto** en el Engine + **Posicionamiento visual estricto sin padding** en el Starter cumplió perfectamente los requisitos sin recurrir a cálculos engañosos ni hardcodeos. La arquitectura original se mantuvo, pero purgada del comportamiento aleatorio de "doble elevación".
