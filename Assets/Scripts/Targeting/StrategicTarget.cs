using UnityEngine;

/// <summary>
/// Marca una entidad del jugador como objetivo estratégico que aparece en el
/// ThreatRegistry. Unidades y edificios lo llevan con distintos threatLevel.
/// </summary>
[DisallowMultipleComponent]
public class StrategicTarget : MonoBehaviour
{
    [Tooltip("Nivel de amenaza que este target genera para los enemigos. Los edificios clave suelen tener valores altos (10+), las unidades valores bajos (0.5-3).")]
    [SerializeField] private float threatLevel = 1f;

    public float ThreatLevel => threatLevel;
    public Transform Transform => transform;

    public void SetThreatLevel(float newLevel)
    {
        if (Mathf.Approximately(newLevel, threatLevel)) return;
        threatLevel = newLevel;
        ThreatRegistry.NotifyThreatLevelChanged(this);
    }

    private void OnEnable()
    {
        ThreatRegistry.Register(this);
    }

    private void OnDisable()
    {
        ThreatRegistry.Unregister(this);
    }
}