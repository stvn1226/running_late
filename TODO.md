# Deuda Técnica / TODO List

## Físicas y Movimiento
- [x] **OneWayPlatforms (Sidewalks) - Colisiones laterales**: Al saltar en diagonal/horizontal hacia una plataforma `OneWayPlatform`, el personaje ocasionalmente choca de frente con el borde del collider. La experiencia debería ser 100% fluida (smooth). Se debe investigar ajustes en los `EdgeCollider2D`, el `SurfaceArc` del `PlatformEffector2D`, o añadir un material físico sin fricción en los bordes para que el jugador resbale hacia arriba en lugar de detenerse.
