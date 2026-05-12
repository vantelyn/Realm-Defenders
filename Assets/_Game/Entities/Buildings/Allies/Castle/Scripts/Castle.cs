using System.Collections;
using UnityEngine;
using Game.Combat;
using Game.Targeting;
using Game.Managers;

namespace Game.Buildings
{

/// <summary>
/// Castillo unico. Transiciones de nivel envueltas en cinematica:
///   Lv1->Lv2 (manual con recursos): pausa tiempo, camara a castillo, SFX upgrade-sting + ApplyLevel(2), hold, camara vuelve, reanuda.
///   Lv2 (evolucionando): audioSource3D loop espacial (solo se oye cerca).
///   Lv2->Lv3: pausa tiempo, camara a castillo, SFX trueno + electrocute + ApplyLevel(3), hold, camara vuelve, reanuda y OnRoundWon.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(DamageReceiverBuilding))]
[RequireComponent(typeof(StrategicTarget))]
public class Castle : MonoBehaviour
{
    public static Castle Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private CastleUpgradeData data;

    [Header("Initial Level")]
    [SerializeField] private int startLevel = 1;

    [Header("Audio - global 2D")]
    [Tooltip("AudioSource 2D para SFX globales (upgrade sting, trueno). Si vacio se anade en Awake.")]
    [SerializeField] private AudioSource audioSource2D;
    [Tooltip("Sting al subir a Lv2 (one-shot, mientras la camara enfoca el castillo).")]
    [SerializeField] private AudioClip stage2UpgradeClip;
    [Tooltip("Trueno al subir a Lv3 (one-shot).")]
    [SerializeField] private AudioClip stage3ThunderClip;
    [Range(0f,1f)] [SerializeField] private float upgradeStingVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float thunderVolume = 1f;

    [Header("Audio - spatial 3D (cristal creciendo)")]
    [Tooltip("AudioSource 3D para el loop del cristal en evolucion. Solo audible cuando la camara/listener esta cerca. Si vacio se anade en Awake.")]
    [SerializeField] private AudioSource audioSource3D;
    [Tooltip("Loop espacial mientras el castillo esta evolucionando de Lv2 a Lv3.")]
    [SerializeField] private AudioClip stage2GrowingClip;
    [Range(0f,1f)] [SerializeField] private float growingVolume = 1f;
    [Tooltip("Distancia minima del rolloff (audible al 100% por dentro).")]
    [SerializeField] private float growingMinDistance = 3f;
    [Tooltip("Distancia maxima del rolloff (inaudible mas alla).")]
    [SerializeField] private float growingMaxDistance = 12f;

    [Header("Cinematica")]
    [Tooltip("Segundos del lerp de la camara (ida y vuelta cada uno).")]
    [SerializeField] private float cameraTravelTime = 1.6f;
    [Tooltip("Segundos que la camara aguanta enfocada despues de aplicar el upgrade.")]
    [SerializeField] private float cameraHoldTime = 3.2f;
    [Tooltip("timeScale durante la cinematica. 0 = pausa total. 0.05-0.15 = camara lenta.")]
    [Range(0f, 1f)] [SerializeField] private float cinematicTimeScale = 0.1f;
    [Tooltip("Orthographic size de la camara al enfocar el castillo. Mas bajo = mas zoom. 0 o negativo = no tocar el zoom.")]
    [SerializeField] private float focusZoomOrthoSize = 3.5f;
    [Tooltip("Pausa adicional (segundos) entre la llegada de la camara y el cambio de nivel del castillo. Da margen para anticipar el efecto.")]
    [SerializeField] private float arrivalDelay = 0.6f;

    [Header("Lv3 Electrocute")]
    [SerializeField] private bool electrocuteOnMaxLevel = true;

    private int currentLevel;
    private float evolutionTimer;
    private bool evolving;
    private bool cinematicActive;

    private Animator animator;
    private DamageReceiverBuilding health;
    private StrategicTarget strategicTarget;

    public int CurrentLevel => currentLevel;
    public bool IsMaxLevel => currentLevel >= (data != null ? data.MaxLevel : 1);
    public bool IsEvolving => evolving;
    public float EvolutionProgress => evolving && data != null ? Mathf.Clamp01(evolutionTimer / data.evolutionDuration) : 0f;
    public CastleUpgradeData Data => data;

    public event System.Action<int> OnLevelChanged;
    public static event System.Action<float> OnRoundWon;

    private float roundStartTime;
    public float RoundDuration => Time.time - roundStartTime;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        health = GetComponent<DamageReceiverBuilding>();
        strategicTarget = GetComponent<StrategicTarget>();
        EnsureAudioSources();
    }

    private void EnsureAudioSources()
    {
        if (audioSource2D == null)
        {
            audioSource2D = gameObject.AddComponent<AudioSource>();
            audioSource2D.playOnAwake = false; audioSource2D.spatialBlend = 0f;
        }
        if (audioSource3D == null)
        {
            audioSource3D = gameObject.AddComponent<AudioSource>();
            audioSource3D.playOnAwake = false;
            audioSource3D.spatialBlend = 1f;
            audioSource3D.rolloffMode = AudioRolloffMode.Linear;
            audioSource3D.minDistance = growingMinDistance;
            audioSource3D.maxDistance = growingMaxDistance;
        }
        else
        {
            audioSource3D.spatialBlend = 1f;
            audioSource3D.minDistance = growingMinDistance;
            audioSource3D.maxDistance = growingMaxDistance;
        }
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this) { Debug.LogWarning($"[Castle] Ya existe otra instancia. Destruyendo {name}."); Destroy(gameObject); return; }
        Instance = this;
        roundStartTime = Time.time;
        ApplyLevel(startLevel, isInitial: true);
    }

    private void OnDisable() { if (Instance == this) Instance = null; }

    private void Update()
    {
        if (!evolving) return;
        evolutionTimer += Time.deltaTime;
        if (evolutionTimer >= data.evolutionDuration)
        {
            evolving = false;
            evolutionTimer = 0f;
            StartCoroutine(Stage3Cinematic());
        }
    }

    public bool TryUpgrade(InventoryManager inventory)
    {
        if (data == null || inventory == null) return false;
        if (IsMaxLevel || evolving || cinematicActive) return false;
        int nextLevel = currentLevel + 1;
        if (!data.HasLevel(nextLevel)) return false;
        CastleUpgradeData.Level next = data.GetLevel(nextLevel);
        bool hasCost = next.woodCost > 0 || next.meatCost > 0 || next.moneyCost > 0;
        if (!hasCost) return false;
        if (!CanAfford(inventory, next)) return false;
        Pay(inventory, next);
        StartCoroutine(Stage2Cinematic());
        return true;
    }

    private IEnumerator Stage2Cinematic()
    {
        cinematicActive = true;
        float prevScale = Time.timeScale;
        Time.timeScale = cinematicTimeScale;
        bool done = false;
        CameraManager.FocusOn(transform, cameraTravelTime, cameraHoldTime, focusZoomOrthoSize, arrivalDelay,
            onArrived: () => {
                if (audioSource2D != null && stage2UpgradeClip != null) audioSource2D.PlayOneShot(stage2UpgradeClip, upgradeStingVolume);
                ApplyLevel(currentLevel + 1);
            },
            onComplete: () => { done = true; });
        while (!done) yield return null;
        Time.timeScale = prevScale;
        cinematicActive = false;

        // Arrancar fase 2->3 (auto-evolucion) si el siguiente nivel es sin coste
        int afterThis = currentLevel + 1;
        if (data.HasLevel(afterThis))
        {
            var auto = data.GetLevel(afterThis);
            bool autoHasCost = auto.woodCost > 0 || auto.meatCost > 0 || auto.moneyCost > 0;
            if (!autoHasCost) { evolving = true; evolutionTimer = 0f; }
        }
    }

    private IEnumerator Stage3Cinematic()
    {
        cinematicActive = true;
        float prevScale = Time.timeScale;
        Time.timeScale = cinematicTimeScale;
        bool done = false;
        CameraManager.FocusOn(transform, cameraTravelTime, cameraHoldTime, focusZoomOrthoSize, arrivalDelay,
            onArrived: () => {
                StopGrowingLoop();
                if (audioSource2D != null && stage3ThunderClip != null) audioSource2D.PlayOneShot(stage3ThunderClip, thunderVolume);
                if (electrocuteOnMaxLevel) ElectrocuteEnemiesAndDestroySpawners();
                ApplyLevel(currentLevel + 1);
            },
            onComplete: () => { done = true; });
        while (!done) yield return null;
        Time.timeScale = prevScale;
        cinematicActive = false;
        OnRoundWon?.Invoke(RoundDuration);
    }

    private void ApplyLevel(int newLevel, bool isInitial = false)
    {
        int prevLevel = currentLevel;
        currentLevel = Mathf.Clamp(newLevel, 1, data != null ? data.MaxLevel : 1);
        if (data == null) return;

        var lvl = data.GetLevel(currentLevel);
        if (health != null)
        {
            health.maxHealth = lvl.maxHealth;
            if (!isInitial) health.Heal(lvl.maxHealth);
            else health.ResetToFull(lvl.maxHealth);
        }
        if (strategicTarget != null) strategicTarget.SetThreatLevel(lvl.threatLevel);
        if (animator != null) animator.SetInteger("stage", currentLevel);
        OnLevelChanged?.Invoke(currentLevel);

        // Arrancar/parar growing loop 3D al entrar o salir de Lv2 (sin contar la asignacion inicial)
        if (!isInitial)
        {
            if (currentLevel == 2 && prevLevel < 2) StartGrowingLoop();
            else if (currentLevel != 2) StopGrowingLoop();
        }
    }

    private void StartGrowingLoop()
    {
        if (audioSource3D == null || stage2GrowingClip == null) return;
        audioSource3D.clip = stage2GrowingClip;
        audioSource3D.loop = true;
        audioSource3D.volume = growingVolume;
        audioSource3D.Play();
    }

    private void StopGrowingLoop()
    {
        if (audioSource3D == null) return;
        audioSource3D.loop = false;
        audioSource3D.Stop();
        audioSource3D.clip = null;
    }

    private void ElectrocuteEnemiesAndDestroySpawners()
    {
        var spawners = UnityEngine.Object.FindObjectsByType<Game.Enemies.EnemySpawner>(FindObjectsSortMode.None);
        for (int i = 0; i < spawners.Length; i++) if (spawners[i] != null) Destroy(spawners[i].gameObject);
        var enemies = UnityEngine.Object.FindObjectsByType<Game.AI.BaseEnemyAI>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i]; if (e == null) continue;
            var dr = e.GetComponent<DamageReceiver>();
            if (dr != null) dr.ApplyDamage(999999, false, false, Vector2.zero);
            else Destroy(e.gameObject);
        }
    }

    public bool CanAffordNextUpgrade(InventoryManager inventory)
    {
        if (data == null || inventory == null || IsMaxLevel || evolving || cinematicActive) return false;
        int nextLevel = currentLevel + 1;
        if (!data.HasLevel(nextLevel)) return false;
        var next = data.GetLevel(nextLevel);
        bool hasCost = next.woodCost > 0 || next.meatCost > 0 || next.moneyCost > 0;
        if (!hasCost) return false;
        return CanAfford(inventory, next);
    }

    public CastleUpgradeData.Level GetNextUpgradeLevel()
    {
        if (data == null || IsMaxLevel) return null;
        int nextLevel = currentLevel + 1;
        if (!data.HasLevel(nextLevel)) return null;
        return data.GetLevel(nextLevel);
    }

    private static bool CanAfford(InventoryManager inv, CastleUpgradeData.Level lvl)
        => inv.Wood >= lvl.woodCost && inv.Meat >= lvl.meatCost && inv.Money >= lvl.moneyCost;

    private static void Pay(InventoryManager inv, CastleUpgradeData.Level lvl)
    {
        if (lvl.woodCost > 0) inv.TrySpendWood(lvl.woodCost);
        if (lvl.meatCost > 0) inv.TrySpendMeat(lvl.meatCost);
        if (lvl.moneyCost > 0) inv.TrySpendMoney(lvl.moneyCost);
    }
}
}
