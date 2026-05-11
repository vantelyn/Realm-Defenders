using UnityEngine;

using Game.Managers;
namespace Game.Targeting
{

/// <summary>
/// Marca una entidad del jugador como objetivo estrat�gico que aparece en el
/// ThreatManager. Unidades y edificios lo llevan con distintos threatLevel.
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
        ThreatManager.NotifyThreatLevelChanged(this);
    }

    private void OnEnable()
    {
        ThreatManager.Register(this);
    }

    private void OnDisable()
    {
        ThreatManager.Unregister(this);
    }
}
}
