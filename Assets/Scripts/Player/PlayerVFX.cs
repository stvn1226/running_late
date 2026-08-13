using UnityEngine;

public class PlayerVFX : MonoBehaviour
{
    [Header("Particle Systems (Hijos del Jugador)")]
    [Tooltip("Arrastra aquí los Particle Systems que configuraste como hijos del jugador. El script se encargará de mantenerlos en World Space y evitar que causen lag.")]
    public ParticleSystem runDust;
    public ParticleSystem jumpDust;
    public ParticleSystem landDust;
    public ParticleSystem doubleJumpDust;
    public ParticleSystem toastBreak;

    private Vector3 runDustOffset;
    private Vector3 jumpDustOffset;
    private Vector3 landDustOffset;
    private Vector3 doubleJumpDustOffset;
    private Vector3 toastBreakOffset;

    private void Awake()
    {
        // Configuramos cada sistema de partículas para que sea ultra-eficiente
        // y se quede en World Space sin destruirse (lo que causaba el lag/errores).
        SetupParticleSystem(runDust, ref runDustOffset);
        SetupParticleSystem(jumpDust, ref jumpDustOffset);
        SetupParticleSystem(landDust, ref landDustOffset);
        SetupParticleSystem(doubleJumpDust, ref doubleJumpDustOffset);
        SetupParticleSystem(toastBreak, ref toastBreakOffset);
    }

    private void SetupParticleSystem(ParticleSystem ps, ref Vector3 offset)
    {
        if (ps == null) return;

        // Guardar la posición (offset) exacta que le diste en el editor
        offset = ps.transform.localPosition;

        // Desemparentarlo para que no sufra las rotaciones o escalas del jugador (World Space real)
        ps.transform.SetParent(null);

        // Forzar configuración segura para evitar el lag y la auto-destrucción
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // ¡CRÍTICO! Evita que el sistema se destruya a sí mismo después de jugar, lo cual causaba errores nulos y lag masivo
        if (main.stopAction == ParticleSystemStopAction.Destroy)
        {
            main.stopAction = ParticleSystemStopAction.None;
        }
    }

    private void PlayParticle(ParticleSystem ps, Vector3 localOffset)
    {
        if (ps != null)
        {
            // Mover el emisor a la posición actual del jugador + el offset guardado
            ps.transform.position = transform.TransformPoint(localOffset);
            
            // Emitir las partículas. Usamos Play para respetar los Bursts configurados.
            ps.Play();
        }
    }

    public void PlayRunDust()
    {
        PlayParticle(runDust, runDustOffset);
    }

    public void PlayJumpDust()
    {
        PlayParticle(jumpDust, jumpDustOffset);
    }

    public void PlayLandDust()
    {
        PlayParticle(landDust, landDustOffset);
    }

    public void PlayDoubleJumpDust()
    {
        PlayParticle(doubleJumpDust, doubleJumpDustOffset);
    }

    public void PlayToastBreak()
    {
        PlayParticle(toastBreak, toastBreakOffset);
    }
}
