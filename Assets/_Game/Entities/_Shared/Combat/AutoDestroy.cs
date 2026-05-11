using UnityEngine;

namespace Game.Combat
{

/// <summary>
/// Auto-destruye el GameObject tras `lifetime` segundos desde su Start.
/// Usado para remains, efectos visuales y otros props efimeros que no
/// merecen un sistema de pooling completo. Si el profiler senala spikes
/// de GC/alloc por destruccion frecuente, considerar pooling.
/// </summary>
public class AutoDestroy : MonoBehaviour
{
    [Tooltip("Tiempo desde Start hasta que se destruye el GameObject.")]
    [SerializeField] private float lifetime = 15f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
}
