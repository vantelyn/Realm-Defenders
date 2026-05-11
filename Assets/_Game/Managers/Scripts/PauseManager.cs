using UnityEngine;

namespace Game.Managers
{

/// <summary>
/// Pausa el juego: gestiona el menú de pausa y el Time.timeScale. Fusiona la
/// responsabilidad antes repartida entre GameManager (Escape) y UIManager
/// (toggle del menú + timeScale).
///
/// Participa en el modal stack del input: consume Escape solo si nadie más lo
/// ha consumido este frame (BuildingManager en placement, SelectionManager con
/// unidad seleccionada). Resetea el flag en LateUpdate, último del frame.
/// </summary>
[DefaultExecutionOrder(100)]
public class PauseManager : MonoBehaviour
{
    private static PauseManager instance;
    public static PauseManager Instance => instance;

    [Header("Refs")]
    [SerializeField] private GameObject pauseMenu;

    private bool isPaused;
    public static bool IsPaused => instance != null && instance.isPaused;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (InputArbiter.EscapeConsumed) return;
        InputArbiter.EscapeConsumed = true;
        Toggle();
    }

    private void LateUpdate()
    {
        InputArbiter.EscapeConsumed = false;
    }

    public static void Pause()
    {
        if (instance == null) return;
        if (instance.pauseMenu != null)
        {
            // Asegurar que el menu de pausa renderiza encima de todos los demas elementos
            // UI del Canvas y por tanto bloquea sus raycasts (hover/click no llegan a counters,
            // botones, tooltips, threat bar, etc. mientras esta activo).
            instance.pauseMenu.transform.SetAsLastSibling();
            instance.pauseMenu.SetActive(true);
        }
        Time.timeScale = 0f;
        instance.isPaused = true;
    }

    public static void Resume()
    {
        if (instance == null) return;
        if (instance.pauseMenu != null) instance.pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        instance.isPaused = false;
    }

    public static void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }
}

}
