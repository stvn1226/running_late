# Level Generator Double Decker Rules

Notas de interpretacion antes de probar seed `2902957`.

## Flujo general

El generador trabaja en dos fases grandes:

1. `LevelGeneratorEngine` arma un `LevelBlueprint` con posiciones `X`, alturas logicas y prefabs.
2. `LevelStarter` renderiza ese blueprint instanciando modulos reales.

En modo `DualPath`, el orden esperado es:

1. `Pass1_SetAnchors`: fija casa y escuela.
2. `Pass2_GenerateRoad`: genera la carretera inferior completa.
3. `Pass3_GenerateSidewalk`: genera la vereda/piso superior sincronizada con la carretera y agrega las rampas diagonales grandes de conexion.
4. `Pass4_FinalizeObstacles`: limita obstaculos de carretera segun el clearance con la vereda superior.

Esto confirma la regla de diseno: primero existe el camino de abajo; luego se decide el camino de arriba usando la altura del camino de abajo.

## Reglas escritas en el codigo

### Carretera inferior

- La carretera empieza despues de `HouseAnchor`.
- Puede subir o bajar con slopes de carretera, pero tiene una zona final donde deja de hacer pendientes aleatorias.
- En la zona final intenta volver a altura `0` antes de la escuela.
- Al terminar, la posicion de la escuela se ajusta al final real de la carretera.

### Rampas grandes entre carretera y vereda

- La rampa inicial se calcula despues de una pieza chica de separacion desde la casa.
- La rampa final se calcula antes de una pieza chica de separacion hacia la escuela.
- La rampa diagonal debe conocer `ConnectorRoadElevation`, o sea la altura de la carretera exactamente donde aterriza.
- `LevelStarter` usa esa altura para poner la rampa en `baseY + ConnectorRoadElevation * groundBlockHeight`.
- La rampa diagonal se genera con `baselineH + 1`, porque debe conectar la carretera local con una vereda exactamente un nivel por encima.

Interpretacion: la rampa grande no debe asumir altura global fija; debe depender de la elevacion de la carretera en su punto de anclaje.

### Vereda superior

- La vereda empieza despues de la rampa inicial.
- Su altura inicial es `GetRoadElevationAt(currentX) + 1`.
- La altura objetivo antes de la rampa final es `GetRoadElevationAt(targetEndRampX) + 1`.
- En la zona final, si la vereda no esta a esa altura objetivo, el generador empieza a forzar slopes para llegar.
- Las pendientes de vereda intentan ponerse sobre carretera plana.
- Hay una excepcion: si ya no queda espacio, se puede saltar el chequeo de carretera plana para no fallar en llegar a la altura objetivo.

Interpretacion: la vereda superior no deberia "cortar" por decision aleatoria cerca del final. Cerca del final deberia estar dominada por el objetivo de conectar con la rampa grande.

## Contradiccion encontrada

Hay una regla documentada en `docs/TODO.md`:

> Unificar el ancho de las piezas de carretera plana (16.0) con las de rampas (19.06 / 3.38).

Los assets actuales confirman esa diferencia:

- Road flat: `16`, `32`, `48`.
- Sidewalk flat normal: `16`, `32`, `48`.
- Interlayer slope: `19.05`.
- Sidewalk slope: `19`.
- Road slope: `13`.
- Algunos assets Brick usan multiplos de `19.06`.

El problema logico era buscar una carretera plana con el mismo ancho exacto que la rampa grande. Esa condicion contradice los assets reales: una rampa de `19.05` no encaja por igualdad con una pieza plana de `16`, y tampoco es igual a una de `32` o `48`, aunque fisicamente si puede entrar dentro de una pieza de `32` o `48`.

La regla correcta no es "buscar una pieza plana del mismo ancho"; la regla correcta es "buscar un tramo plano suficientemente largo para contener la rampa".

## Riesgos que siguen abiertos

- La vereda final agrega la rampa en `currentX`, no necesariamente en el `targetEndRampX` original. Eso es correcto si `currentX` llega al anclaje calculado, pero puede ocultar overshoot si `GetBestFitSidewalk` mete una pieza minima cuando queda menos de `16`.
- `mustForceSlope` puede permitir una pendiente de vereda sobre una pendiente de carretera cuando ya no queda espacio. Es una salida de emergencia, no una regla ideal.
- `LevelStarter.RenderTrack` calcula `segTransformY` pero actualmente instancia con `baseY`. La ruta vieja parece depender de `Y_Elevation` absoluto mas `sidewalkYOffset`, asi que no parece el bug principal, pero es deuda tecnica porque el comment y la variable sugieren otra intencion.
- Existe una clase `InterLayerSlopeEntry` que documenta combinacion greedy de slopes por unidades de altura, pero el flujo actual de `LevelStarter` no parece usar esa lista para las rampas grandes del blueprint. Hoy las rampas grandes parecen asumir un solo nivel relativo.

## Conclusion antes de prueba

La correccion defendible es alinear las rampas grandes a un tramo plano que pueda contener su ancho, y calcular la altura de la rampa con la elevacion de carretera en ese punto.

El fallo original parece venir de una regla incompleta del generador: trataba el ancho de la rampa como si tuviera que ser igual al ancho de un modulo plano. Eso rompe especialmente cerca del final, porque si no encuentra el punto correcto, la vereda superior calcula su objetivo desde un `targetEndRampX` que puede caer en una zona incorrecta, y entonces empieza a tomar decisiones finales demasiado pronto o en una altura que no corresponde.
