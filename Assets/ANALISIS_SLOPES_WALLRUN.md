# 🔍 ANÁLISIS DETALLADO: Slopes y WallRun - Branch Viejo vs Nuevo

**Fecha:** 2026-01-21  
**Propósito:** Documentar diferencias críticas entre sistema de Animator Controller (funciona) y animación por código (bugs)

---

## 📋 ÍNDICE

1. [Problema 1: Slopes Mostrando JumpFall](#slopes)
2. [Problema 2: WallRun Ghosting](#wallrun)
3. [Comparación de Arquitecturas](#arquitectura)
4. [Recomendaciones](#recomendaciones)

---

<a name="slopes"></a>
## 🏔️ PROBLEMA 1: SLOPES MOSTRANDO JUMPFALL

### **Síntoma**
- **Branch Viejo (Animator Controller):** Al caminar por slopes → muestra **Run/Idle** ✅
- **Branch Nuevo (Código):** Al caminar por slopes → muestra **JumpRise/JumpFall** ❌

---

### **BRANCH VIEJO - PlayerAnimatorController.cs (FUNCIONA)**

```csharp
// Líneas 70-79
private void UpdateAnimator()
{
    float yVelocityToSet = rb.linearVelocity.y;
    
    // 🔑 SLOPE DETECTION!
    if (states.isOnSlope)
    {
        yVelocityToSet = 0f;  // ← FORZAR Y = 0 en slopes
    }
    else if (states.isSliding)
    {
        yVelocityToSet = 0f;  // ← También en slide
    }
    
    animator.SetFloat(YVelocity, yVelocityToSet);
}
```

**Cómo funciona:**
1. Detecta `states.isOnSlope = true`
2. **Fuerza `yVelocity = 0f`** para el Animator
3. Animator Controller usa transitions basadas en `yVelocity`:
   - Si `yVelocity == 0` → queda en **Run/Idle** (locomotion)
   - Si `yVelocity > 0.5` → transiciona a **JumpRise**
   - Si `yVelocity < -0.5` → transiciona a **JumpFall**

**Resultado:** ✅ Player en slope muestra **Run** aunque `rb.linearVelocity.y != 0`

---

### **BRANCH NUEVO - PlayerAnimator.cs (BUG)**

```csharp
// Líneas 164-183 (CÓDIGO ACTUAL - YA TIENE FIX PARCIAL)
if (states.isOnSlope && states.isGrounded)
{
    if (states.isSliding) return AnimHash.Slide;
    if (states.isCrouching) return AnimHash.Crouch;
    
    float slopeAbsVelX = Mathf.Abs(rb.linearVelocity.x);
    if (slopeAbsVelX > runSpeedThreshold) return AnimHash.Run;
    
    return AnimHash.Idle;
}

// Líneas 185-201
if (!states.isGrounded)
{
    float velY = rb.linearVelocity.y;

    if (velY > riseThreshold) return AnimHash.JumpRise;
    else if (velY < fallThreshold) return AnimHash.JumpFall;
    else return AnimHash.JumpMid;
}
```

**Nota:** El código actual YA tiene el check de `isOnSlope && isGrounded` (líneas 164-183).

**¿Por qué sigue habiendo bug reportado?**

Posibles causas:
1. **Orden de evaluación:** El check de slope está DESPUÉS de otros checks de mayor prioridad
2. **isGrounded flickering:** Hay frames donde `isGrounded = false` temporalmente en slopes
3. **Edge case:** Cuando player transiciona entre slope y aire

---

### **VERIFICACIÓN DEL CÓDIGO ACTUAL**

Veamos el orden completo de prioridades en `DetermineTargetAnimation()`:

```csharp
private int DetermineTargetAnimation()
{
    if (states.isStaggered) return AnimHash.HitDamage;                    // 1
    if (states.isKicking && !wasKicking) return GetRandomKickVariation(); // 2
    if (states.isKicking) return currentStateHash;                        // 3
    if (states.isDiving) return AnimHash.GroundSlam;                      // 4
    if (states.isRolling) return AnimHash.Roll;                           // 5
    if (states.isVaultingWall) return AnimHash.WallJump;                  // 6
    if (states.isSlidingOnCable) return AnimHash.Slide;                   // 7
    
    if (states.isWallRunning)                                             // 8
    {
        float absVelY = Mathf.Abs(rb.linearVelocity.y);
        if (absVelY > velocityDeadzone) return AnimHash.WallClimb;
        else return AnimHash.WallSlide;
    }
    
    if (states.isOnSlope && states.isGrounded)                            // 9 ✅
    {
        if (states.isSliding) return AnimHash.Slide;
        if (states.isCrouching) return AnimHash.Crouch;
        float slopeAbsVelX = Mathf.Abs(rb.linearVelocity.x);
        if (slopeAbsVelX > runSpeedThreshold) return AnimHash.Run;
        return AnimHash.Idle;
    }
    
    if (!states.isGrounded)                                               // 10
    {
        float velY = rb.linearVelocity.y;
        if (velY > riseThreshold) return AnimHash.JumpRise;
        else if (velY < fallThreshold) return AnimHash.JumpFall;
        else return AnimHash.JumpMid;
    }
    
    if (wasGrounded != states.isGrounded && states.isGrounded) return AnimHash.Land; // 11
    if (states.isSliding) return AnimHash.Slide;                          // 12
    if (states.isCrouching && !states.isSliding) return AnimHash.Crouch;  // 13
    
    float absVelX = Mathf.Abs(rb.linearVelocity.x);
    if (absVelX > runSpeedThreshold) return AnimHash.Run;                 // 14
    
    return AnimHash.Idle;                                                 // 15
}
```

**El slope check (prioridad 9) está ANTES de air states (prioridad 10)** → Debería funcionar.

---

### **HIPÓTESIS: isGrounded Flickering**

El problema probablemente es:

```
Frame N:
  states.isOnSlope = true
  states.isGrounded = true
  → Retorna AnimHash.Run ✅

Frame N+1:
  states.isOnSlope = true
  states.isGrounded = FALSE  ← Flicker por un frame
  → Skip check #9 (requires isGrounded)
  → Entra a check #10 (!states.isGrounded)
  → velY = +2.0 (subiendo slope)
  → Retorna AnimHash.JumpRise ❌ BUG

Frame N+2:
  states.isGrounded = true (recuperado)
  → Vuelve a AnimHash.Run
```

**Solución:** Modificar check #9 para NO requerir `isGrounded`:

```csharp
// ANTES (línea 164)
if (states.isOnSlope && states.isGrounded)

// DESPUÉS
if (states.isOnSlope)  // ← Sin requerir isGrounded
{
    // Prioridad: si está en slope, SIEMPRE usar locomotion
    // (incluso si isGrounded flickers temporalmente)
    
    if (states.isSliding) return AnimHash.Slide;
    if (states.isCrouching) return AnimHash.Crouch;
    
    float slopeAbsVelX = Mathf.Abs(rb.linearVelocity.x);
    if (slopeAbsVelX > runSpeedThreshold) return AnimHash.Run;
    
    return AnimHash.Idle;
}
```

---

<a name="wallrun"></a>
## 🧗 PROBLEMA 2: WALLRUN GHOSTING

### **Síntoma**
- **Branch Viejo (Animator Controller):** Al soltar pared → transiciona **inmediatamente** a JumpFall ✅
- **Branch Nuevo (Código):** Al soltar pared → WallSprint **persiste 2-3 frames** (ghosting) ❌

---

### **BRANCH VIEJO - Animator Controller (FUNCIONA)**

```csharp
// PlayerAnimatorController.cs - línea 85
private void UpdateAnimator()
{
    animator.SetBool(IsWallRunning, states.isWallRunning);
}
```

**Animator Controller (configuración de transiciones):**
```
WallSprint → JumpFall
Conditions:
  - isWallRunning == false
  - IsGrounded == false
  
Settings:
  - Has Exit Time: FALSE  ← CRÍTICO
  - Transition Duration: 0.0-0.1s
  - Interruption Source: Current State
```

**Flujo:**
1. Player suelta pared → `states.isWallRunning = false`
2. `LateUpdate` → `animator.SetBool(IsWallRunning, false)`
3. **Animator Controller evalúa inmediatamente**
4. Transition se dispara **SIN esperar Exit Time**
5. CrossFade a JumpFall con duration ~0.05s
6. **Resultado:** Transición instantánea (0-1 frame)

---

### **BRANCH NUEVO - PlayerAnimator.cs (BUG)**

```csharp
// Líneas 89-101
private void UpdateAnimation()
{
    int targetHash = DetermineTargetAnimation();

    if (targetHash != currentStateHash)
    {
        // 🔴 PROBLEMA: Cooldown de 50ms entre cambios
        float timeSinceLastChange = Time.time - lastAnimationChangeTime;
        if (timeSinceLastChange >= MIN_ANIMATION_CHANGE_INTERVAL)  // 0.05f
        {
            PlayAnimation(targetHash);
            lastAnimationChangeTime = Time.time;
        }
        // Si < 0.05s → NO ejecuta PlayAnimation() → GHOSTING
    }
}
```

```csharp
// Línea 62
private const float MIN_ANIMATION_CHANGE_INTERVAL = 0.05f;  // 50ms
```

**Flujo del bug:**
1. Player en WallSprint → último cambio en `t = 10.0s`
2. Player suelta pared en `t = 10.03s` → `states.isWallRunning = false`
3. `DetermineTargetAnimation()` retorna `JumpFall`
4. `timeSinceLastChange = 0.03s < 0.05s` → **NO ejecuta PlayAnimation()**
5. `currentStateHash` sigue siendo `WallSlide` → **GHOSTING**
6. Frame siguiente (`t = 10.04s`): aún bloqueado
7. Frame siguiente (`t = 10.05s`): **finalmente ejecuta transición**

**Resultado:** Delay de 2-3 frames (~33-50ms) causando ghosting visible

---

### **SOLUCIÓN: Critical Transitions Bypass**

```csharp
private void UpdateAnimation()
{
    int targetHash = DetermineTargetAnimation();

    if (targetHash != currentStateHash)
    {
        // 🔑 NUEVO: Bypass cooldown para transiciones críticas
        bool isCritical = IsCriticalTransition(currentStateHash, targetHash);
        
        float timeSinceLastChange = Time.time - lastAnimationChangeTime;
        if (isCritical || timeSinceLastChange >= MIN_ANIMATION_CHANGE_INTERVAL)
        {
            PlayAnimation(targetHash);
            lastAnimationChangeTime = Time.time;
        }
    }
}

private bool IsCriticalTransition(int fromHash, int toHash)
{
    // Wall run exit → SIEMPRE inmediato
    if (fromHash == AnimHash.WallSlide || fromHash == AnimHash.WallClimb)
        return true;
    
    // Landing → SIEMPRE inmediato
    if (toHash == AnimHash.Land)
        return true;
    
    // Stagger → SIEMPRE inmediato
    if (toHash == AnimHash.HitDamage)
        return true;
    
    // Kick end en aire → SIEMPRE inmediato
    if ((fromHash == AnimHash.KickA || fromHash == AnimHash.KickB || fromHash == AnimHash.KickC) 
        && toHash == AnimHash.JumpFall)
        return true;
    
    return false;
}
```

---

<a name="arquitectura"></a>
## 🏗️ COMPARACIÓN DE ARQUITECTURAS

### **Branch Viejo: Animator Controller (State Machine)**

```
Código (PlayerAnimatorController)
  ↓
Actualiza PARÁMETROS
  ├── SetFloat(xVelocity)
  ├── SetFloat(yVelocity)
  ├── SetBool(isGrounded)
  ├── SetBool(isOnSlope)
  └── SetBool(isWallRunning)
  ↓
Animator Controller (Unity State Machine)
  ├── Evalúa CONDITIONS en transitions
  ├── Decide qué animación reproducir
  └── Ejecuta transiciones (con Exit Time, Duration, etc)
```

**Ventajas:**
- ✅ Transiciones configurables visualmente
- ✅ Exit Time control (inmediato vs suave)
- ✅ Blend trees automáticos
- ✅ NO requiere cooldowns programáticos

**Desventajas:**
- ❌ Menos control programático
- ❌ Debugging difícil (no hay logs)
- ❌ Cambios requieren editar asset

---

### **Branch Nuevo: Animación por Código**

```
Código (PlayerAnimator)
  ↓
DetermineTargetAnimation()
  ├── Árbol de if/else con prioridades
  ├── Lee estados directamente
  └── Retorna hash de animación
  ↓
PlayAnimation(hash)
  ├── animator.CrossFade(hash, duration, 0)
  └── O animator.Play(hash, 0, 0f)
```

**Ventajas:**
- ✅ Control total programático
- ✅ Logs claros de decisiones
- ✅ Debugging fácil
- ✅ Cambios rápidos (solo código)

**Desventajas:**
- ❌ Requiere cooldowns para evitar flickering
- ❌ Cooldowns causan delays en transiciones críticas
- ❌ Más código y complejidad

---

<a name="recomendaciones"></a>
## ✅ RECOMENDACIONES

### **DEBUGGING IMPLEMENTADO**

He agregado logging detallado en `PlayerAnimator.cs` que te mostrará exactamente qué está pasando:

**Logs que verás en la consola:**

1. **`[SLOPE DETECTED]` (Cyan):** Cuando detecta slope correctamente y usa locomotion
   ```
   [SLOPE DETECTED] isOnSlope=TRUE | isGrounded=TRUE | slopeAngle=35.2° | velY=+1.5 → Using LOCOMOTION
   ```

2. **`[SLOPE BUG]` (Rojo):** Cuando detecta slope PERO isGrounded=false (el bug real)
   ```
   [SLOPE BUG] isOnSlope=TRUE but isGrounded=FALSE! | slopeAngle=35.2° | velY=+2.0 → Will show JUMP!
   ```

3. **`[WALLRUN EXIT]` (Magenta):** Cuando player sale de wallrun
   ```
   [WALLRUN EXIT] wasWallRun=TRUE → isWallRun=FALSE | isGrounded=false | velY=-1.2 | timeSinceLastChange=0.032s
   ```

4. **`[ANIM BLOCKED]` (Amarillo):** Cuando el cooldown bloquea una transición
   ```
   [ANIM BLOCKED] WallSprint → JumpFall | Cooldown: 0.032s < 0.05s | isGrounded=false | velY=-1.5
   ```

---

### **PASOS PARA DEBUGGEAR:**

1. **Entra en Play Mode** en el branch de código
2. **Prueba caminar por un slope** (subiendo o bajando)
   - Si ves logs `[SLOPE BUG]` → Confirma que `isGrounded` flickers
   - Si NO ves logs → El problema está en otro lugar

3. **Prueba wall run y suelta la pared**
   - Si ves logs `[WALLRUN EXIT]` seguidos de `[ANIM BLOCKED]` → Confirma el cooldown ghosting
   - Fíjate en el `timeSinceLastChange` value

4. **Copia los logs** de la consola y pégalos aquí para que pueda analizarlos

---

## ✅ SOLUCIÓN IMPLEMENTADA - SLOPES

### **Fix Aplicado en PlayerAnimator.cs (línea 184)**

```csharp
// ANTES (INCORRECTO):
if (states.isOnSlope && states.isGrounded)  // ← Requería AMBOS

// DESPUÉS (CORRECTO):
if (states.isOnSlope)  // ← Solo verifica slope
```

**¿Por qué funciona?**

Los logs confirmaron que:
- `isOnSlope` permanece `TRUE` correctamente
- `isGrounded` **flickers a FALSE** durante 1-3 frames
- Cuando `isGrounded=false`, el código saltaba el check de slope
- Resultado: Mostraba JumpRise/JumpFall incorrectamente

**Ahora:**
- Si `isOnSlope=TRUE` → **SIEMPRE** usa locomotion (Run/Idle)
- No importa si `isGrounded` flickers temporalmente
- El sistema de slopes tiene su propia detección confiable

---

### **Testing:**

Prueba ahora:
1. Camina por slopes (subiendo/bajando)
2. Ya NO deberías ver `[SLOPE BUG]` warnings rojos
3. Deberías ver SOLO `[SLOPE DETECTED]` cyan con `isGrounded=true/false` variando
4. La animación debe permanecer en **Run** consistentemente

---

### **1. Fix para Slopes** ✅ COMPLETADO

```csharp
// PlayerAnimator.cs - línea 164
// CAMBIAR:
if (states.isOnSlope && states.isGrounded)

// POR:
if (states.isOnSlope)  // Sin requerir isGrounded
```

**Razón:** Evita flickering cuando `isGrounded` cambia temporalmente a false en slopes.

---

### **2. Fix para WallRun Ghosting** ✅ COMPLETADO

**PROBLEMA DESCUBIERTO:**
- Inicial: Cooldown bloqueaba transición a WallSprint (vault) ❌
- Fix 1: Bypass cooldown → Animación muestra pero dura ~0.5s extra ❌
- Fix 2: Reducir duración a 100ms → Todavía 2-3 frames extra ❌
- **Root cause final:** Player sale de la pared ANTES de que termine el timer del vault. Necesita detección de wall contact en tiempo real.

**SOLUCIÓN FINAL APLICADA:**

**1. Bypass cooldown para vault (PlayerAnimator.cs):**
```csharp
private bool IsCriticalTransition(int fromHash, int toHash)
{
    // Wall vault/jump ENTRY → ALWAYS immediate
    if (toHash == AnimHash.WallJump)
        return true;
    
    // Wall run exit → ALWAYS immediate
    if (fromHash == AnimHash.WallSlide || fromHash == AnimHash.WallClimb)
        return true;
    
    // ... otros casos críticos
}
```

**2. Early vault exit cuando sale de la pared (PlayerStates.cs):**
```csharp
private bool IsStillTouchingWall(int direction)
{
    if (config == null || playerCollider == null) return false;

    Vector2 boxCenter = (Vector2)transform.position + playerCollider.offset;
    Vector2 boxSize = new Vector2(config.wallCheckDistance, playerCollider.size.y);
    Vector2 checkDirection = direction > 0 ? Vector2.right : Vector2.left;

    RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, checkDirection, 
                                         config.wallCheckDistance, config.wallLayer);
    return hit.collider != null;
}

// En Update():
if (isVaultingWall)
{
    vaultTimer -= Time.deltaTime;
    
    // End vault early if no longer touching wall
    if (!IsStillTouchingWall(wallDirection))
    {
        Debug.Log($"[VAULT EARLY END] Left wall contact");
        EndVault();
    }
    else if (vaultTimer <= 0f)
    {
        EndVault();
    }
}
```

**3. Duración configurada (PlayerConfig):**
```csharp
wallVaultDuration = 0.15f   // 150ms - con early-exit por wall contact
```

**Archivos modificados:**
- `/Assets/Scripts/Player/PlayerStates.cs` - Agregado `Awake()`, `IsStillTouchingWall()`, early vault exit
- `/Assets/Scripts/Player/PlayerConfig.asset` - `wallVaultDuration = 0.15`
- `/Assets/Scripts/Player/DefaultPlayerConfig.asset` - `wallVaultDuration = 0.15`
- `/Assets/Configs/Player/WallClimbConfig.asset` - `wallVaultDuration = 0.15`

**Resultado:**
- Wall vault entry → **Transición inmediata** ✅
- Wall vault duration → **Termina cuando sale de la pared** (detección física) ✅
- NO frames extras después de dejar el edificio ✅
- Duración máxima ~150ms si permanece en wall contact todo el tiempo ✅

---

### **Testing:**

Prueba ahora y verifica los logs:
1. Haz wall run y **trepa la pared** (vault al llegar arriba)
   - Animación debe mostrarse inmediatamente (sin `[ANIM BLOCKED]`)
   - Debe terminar cuando sales de la pared
   - Verifica log: `[VAULT EARLY END] Left wall contact`
2. Haz wall run y suelta la pared en medio
   - También debe terminar inmediatamente al salir de la pared
3. **NO debe haber frames con animación de wallrun después de salir del edificio**

---

### **3. Testing Checklist**

Agregar método `IsCriticalTransition()` y bypass de cooldown:

```csharp
private void UpdateAnimation()
{
    int targetHash = DetermineTargetAnimation();
    if (targetHash != currentStateHash)
    {
        bool isCritical = IsCriticalTransition(currentStateHash, targetHash);
        float timeSinceLastChange = Time.time - lastAnimationChangeTime;
        
        if (isCritical || timeSinceLastChange >= MIN_ANIMATION_CHANGE_INTERVAL)
        {
            PlayAnimation(targetHash);
            lastAnimationChangeTime = Time.time;
        }
    }
}
```

**Razón:** Transiciones críticas (wall run exit, landing, stagger) necesitan ser inmediatas.

---

### **3. Testing Checklist**

**Slopes:**
- [ ] Caminar subiendo slope → debe mostrar **Run**, NO JumpRise
- [ ] Caminar bajando slope → debe mostrar **Run**, NO JumpFall
- [ ] Crouch en slope → debe mostrar **Crouch**
- [ ] Saltar desde slope → debe mostrar **JumpRise** correctamente

**WallRun:**
- [ ] Wall run → soltar pared → debe transicionar a **JumpFall inmediatamente** (0-1 frame)
- [ ] Wall run rápido → NO debe haber ghosting de WallSprint

---

## 📝 ISSUES PENDIENTES

### **🔴 NUEVO: Downhill Slide Momentum**

**Reportado:** 2026-01-21  
**Estado:** 📌 Anotado para implementación futura

**Problema:**
- El slide en bajada (downhill) actualmente usa la lógica genérica de cualquier slide
- **Debería:** Sumar momentum adicional por la pendiente (efecto gravedad)
- **Debería:** Ser más extenso/largo que un slide normal en plano
- Similar al comportamiento de juegos de momentum como Tony Hawk Pro Skater o SSX

**Archivos a investigar:**
- `/Assets/Scripts/Player/PlayerMovement.cs` → Lógica actual de slide
- `/Assets/Scripts/Player/PlayerPhysics.cs` → Detección de slope angle durante slide
- Branch viejo → Verificar si tiene lógica especial para downhill slides

**Implementación sugerida:**
```csharp
// Pseudo-código
if (states.isSliding && states.isOnSlope && states.slopeAngle > minAngleForBoost)
{
    // Boost basado en ángulo de pendiente
    float slopeBonus = Mathf.Abs(states.slopeAngle) / maxSlopeAngle;
    float momentumBoost = baseSlideForce * (1f + slopeBonus * downhillMultiplier);
    
    // Extender duración del slide en bajada
    slideTimer += slopeBonus * extraSlideDuration;
    
    // Aplicar fuerza adicional en dirección de la pendiente
    rb.AddForce(slopeTangent * momentumBoost, ForceMode2D.Force);
}
```

**Testing checklist para implementación futura:**
- [ ] Slide en plano → Comportamiento normal (sin cambios)
- [ ] Slide en bajada leve (10-20°) → Pequeño boost de momentum
- [ ] Slide en bajada pronunciada (30-45°) → Boost significativo + slide más largo
- [ ] Slide en subida → Debería frenar/cancelar más rápido
- [ ] Transición slide → jump desde downhill → Debería conservar momentum horizontal

---
- [ ] Landing desde wallrun → debe mostrar **Land** inmediatamente

---

## 📊 RESUMEN EJECUTIVO

| **Aspecto** | **Branch Viejo** | **Branch Nuevo (sin fix)** | **Branch Nuevo (con fix)** |
|-------------|------------------|----------------------------|----------------------------|
| **Slopes** | Run/Idle ✅ | JumpRise/Fall ❌ | Run/Idle ✅ |
| **WallRun exit** | Inmediato ✅ | 2-3 frames delay ❌ | Inmediato ✅ |
| **Timing** | Animator (0-1 frame) | Cooldown (33-50ms) | Critical bypass (0-1 frame) |
| **Arquitectura** | State Machine | if/else tree | if/else tree + bypass |

---

**FIN DEL ANÁLISIS**
