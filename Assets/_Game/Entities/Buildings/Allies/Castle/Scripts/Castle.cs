using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Combat;
using Game.Targeting;
using Game.Managers;

namespace Game.Buildings
{

/// <summary>
/// Castillo unico. Transiciones cinematicas:
///   Lv1->Lv2: focus castillo + zoom + SFX upgrade + ApplyLevel(2) + retorno.
///   Lv2 evolucionando: loop 3D del cristal.
///   Lv2->Lv3: secuencia completa:
///     1) focus castillo + thunder + ApplyLevel(3)
///     2) flash blanco pantalla
///     3) focus a un enemigo + VFX simultaneo sobre TODOS + matar todos
///     4) por cada portal: focus + VFX explosion + destruir
///     5) retorno al castillo + musica de victoria + VictoryScreen.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(DamageReceiverBuilding))]
[RequireComponent(typeof(StrategicTarget))]
public class Castle : MonoBehaviour
{
    public static Castle Instance { get; private set; }

    [Header("Data")]
    [SerializeField] private CastleUpgradeData data;
    [SerializeField] private int startLevel = 1;

    [Header("Audio - global 2D")]
    [SerializeField] private AudioSource audioSource2D;
    [SerializeField] private AudioClip stage2UpgradeClip;
    [SerializeField] private AudioClip stage3ThunderClip;
    [SerializeField] private AudioClip victoryMusicClip;
    [Tooltip("SFX al matar a los enemigos electrocutados (uno solo, no uno por enemigo).")]
    [SerializeField] private AudioClip enemyDeathClip;
    [Tooltip("SFX al destruir cada portal.")]
    [SerializeField] private AudioClip portalDestroyClip;
    [Range(0f,1f)] [SerializeField] private float upgradeStingVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float thunderVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float victoryMusicVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float enemyDeathVolume = 1f;
    [Range(0f,1f)] [SerializeField] private float portalDestroyVolume = 1f;

    [Header("Audio - spatial 3D (cristal creciendo)")]
    [SerializeField] private AudioSource audioSource3D;
    [SerializeField] private AudioClip stage2GrowingClip;
    [Range(0f,1f)] [SerializeField] private float growingVolume = 1f;
    [SerializeField] private float growingMinDistance = 3f;
    [SerializeField] private float growingMaxDistance = 12f;

    [Header("Cinematica - tiempos")]
    [SerializeField] private float cameraTravelTime = 4.0f;
    [SerializeField] private float cameraHoldTime = 4.0f;
    [Range(0f, 1f)] [SerializeField] private float cinematicTimeScale = 0.05f;
    [SerializeField] private float focusZoomOrthoSize = 3.5f;
    [SerializeField] private float arrivalDelay = 0.6f;

    [Header("Lv3 - VFX")]
    [Tooltip("VFX que aparece sobre cada enemigo electrocutado (todos a la vez).")]
    [SerializeField] private GameObject electrocuteVfxPrefab;
    [Tooltip("VFX que aparece al destruir cada portal (uno tras otro).")]
    [SerializeField] private GameObject portalExplosionVfxPrefab;
    [Tooltip("Color del flash global de pantalla tras el trueno (alpha tweenea 0->peak->0).")]
    [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Pico de alpha del flash.")]
    [Range(0f,1f)] [SerializeField] private float flashPeakAlpha = 0.85f;
    [Tooltip("Segundos de fade-in del flash.")]
    [SerializeField] private float flashIn = 0.15f;
    [Tooltip("Segundos de hold del flash en pico.")]
    [SerializeField] private float flashHold = 0.1f;
    [Tooltip("Segundos de fade-out del flash.")]
    [SerializeField] private float flashOut = 0.5f;
    [Tooltip("Delay entre el final del cinematic Lv3 y el inicio del flash.")]
    [SerializeField] private float postThunderDelay = 0.3f;

    [Header("Lv3 - secuencia")]
    [Tooltip("Travel/hold de la camara al enfocar un enemigo durante la electrocucion.")]
    [SerializeField] private float enemyFocusTravel = 4.0f;
    [SerializeField] private float enemyFocusHold = 4.5f;
    [SerializeField] private float enemyFocusZoom = 3f;
    [SerializeField] private float enemyArrivalDelay = 3.5f;
    [Tooltip("Travel/hold por cada portal.")]
    [SerializeField] private float portalFocusTravel = 5.0f;
    [SerializeField] private float portalFocusHold = 4.5f;
    [SerializeField] private float portalFocusZoom = 3f;
    [SerializeField] private float portalArrivalDelay = 4.0f;
    [Tooltip("Delay entre destruccion de un portal y enfoque del siguiente.")]
    [SerializeField] private float betweenPortalsDelay = 1.5f;
    [Tooltip("Tras spawn VFX + SFX, segundos antes de destruir efectivamente los enemigos.")]
    [SerializeField] private float enemyVfxToDeathDelay = 1.0f;
    [Tooltip("Tras spawn VFX + SFX, segundos antes de destruir efectivamente el portal.")]
    [SerializeField] private float portalVfxToDeathDelay = 1.0f;
    [Tooltip("Delay tras retorno al castillo antes de mostrar la pantalla de victoria.")]
    [SerializeField] private float preVictoryDelay = 0.6f;

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
        if (audioSource2D == null) { audioSource2D = gameObject.AddComponent<AudioSource>(); audioSource2D.playOnAwake = false; audioSource2D.spatialBlend = 0f; }
        if (audioSource3D == null) { audioSource3D = gameObject.AddComponent<AudioSource>(); audioSource3D.playOnAwake = false; audioSource3D.spatialBlend = 1f; audioSource3D.rolloffMode = AudioRolloffMode.Linear; }
        audioSource3D.spatialBlend = 1f;
        audioSource3D.minDistance = growingMinDistance;
        audioSource3D.maxDistance = growingMaxDistance;
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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
        var next = data.GetLevel(nextLevel);
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

        int afterThis = currentLevel + 1;
        if (data.HasLevel(afterThis))
        {
            var auto = data.GetLevel(afterThis);
            bool autoHasCost = auto.woodCost > 0 || auto.meatCost > 0 || auto.moneyCost > 0;
            if (!autoHasCost) { evolving = true; evolutionTimer = 0f; }
        }
    }

    // ========== Lv3 secuencia completa ==========

    private IEnumerator Stage3Cinematic()
    {
        cinematicActive = true;
        float prevScale = Time.timeScale;
        Time.timeScale = cinematicTimeScale;
        var tok = CameraManager.BeginCinematic();

        // Fase 1: focus castillo + thunder + ApplyLevel(3) (sin retorno)
        bool done = false;
        CameraManager.FocusTo(transform, cameraTravelTime, cameraHoldTime, focusZoomOrthoSize, arrivalDelay,
            onArrived: () => {
                StopGrowingLoop();
                if (audioSource2D != null && stage3ThunderClip != null) audioSource2D.PlayOneShot(stage3ThunderClip, thunderVolume);
                ApplyLevel(currentLevel + 1);
            },
            onComplete: () => { done = true; });
        while (!done) yield return null;

        // Fase 2: delay + flash de pantalla
        yield return WaitUnscaled(postThunderDelay);
        yield return ScreenFlash();

        // Snapshot de enemigos y spawners
        var enemies = new List<Game.AI.BaseEnemyAI>(UnityEngine.Object.FindObjectsByType<Game.AI.BaseEnemyAI>(FindObjectsSortMode.None));
        enemies.RemoveAll(e => e == null);
        var spawners = new List<Game.Enemies.EnemySpawner>(UnityEngine.Object.FindObjectsByType<Game.Enemies.EnemySpawner>(FindObjectsSortMode.None));
        spawners.RemoveAll(s => s == null);

        // Fase 3: focus a un enemigo, matar todos a la vez (sin retorno)
        if (enemies.Count > 0)
        {
            var anchor = enemies[Random.Range(0, enemies.Count)].transform;
            done = false;
            CameraManager.FocusTo(anchor, enemyFocusTravel, enemyFocusHold, enemyFocusZoom, enemyArrivalDelay,
                onArrived: () => {
                    // Fase A: VFX + SFX inmediato sobre todos los enemigos. La muerte se difiere.
                    if (audioSource2D != null && enemyDeathClip != null) audioSource2D.PlayOneShot(enemyDeathClip, enemyDeathVolume);
                    foreach (var e in enemies) {
                        if (e == null) continue;
                        SpawnVfx(electrocuteVfxPrefab, e.transform.position);
                    }
                    StartCoroutine(KillAfterDelay(enemies, enemyVfxToDeathDelay));
                },
                onComplete: () => { done = true; });
            while (!done) yield return null;
        }

        // Fase 4: portales encadenados uno a uno (sin retorno entre ellos)
        for (int i = 0; i < spawners.Count; i++)
        {
            var sp = spawners[i]; if (sp == null) continue;
            var anchor = sp.transform;
            int idx = i;
            float tStart = Time.realtimeSinceStartup;
            Debug.Log($"[Castle] Portal {idx} FocusTo START at t={tStart:F2}, travel={portalFocusTravel}, arrivalDelay={portalArrivalDelay}");
            done = false;
            CameraManager.FocusTo(anchor, portalFocusTravel, portalFocusHold, portalFocusZoom, portalArrivalDelay,
                onArrived: () => {
                    Debug.Log($"[Castle] Portal {idx} onArrived (VFX) at t={Time.realtimeSinceStartup:F2}, elapsed={Time.realtimeSinceStartup - tStart:F2}");
                    if (audioSource2D != null && portalDestroyClip != null) audioSource2D.PlayOneShot(portalDestroyClip, portalDestroyVolume);
                    SpawnVfx(portalExplosionVfxPrefab, anchor.position);
                    StartCoroutine(DestroyAfterDelay(sp != null ? sp.gameObject : null, portalVfxToDeathDelay));
                },
                onComplete: () => { done = true; Debug.Log($"[Castle] Portal {idx} onComplete at t={Time.realtimeSinceStartup:F2}, total={Time.realtimeSinceStartup - tStart:F2}"); });
            while (!done) yield return null;
            yield return WaitUnscaled(betweenPortalsDelay);
        }

        // Fase 5: vuelta a la posicion original (con zoom original) + musica + victoria
        yield return CameraManager.ReturnToCinematicStart(tok, cameraTravelTime);
        yield return WaitUnscaled(preVictoryDelay);
        if (audioSource2D != null && victoryMusicClip != null) audioSource2D.PlayOneShot(victoryMusicClip, victoryMusicVolume);

        CameraManager.EndCinematic(tok);
        Time.timeScale = prevScale;
        cinematicActive = false;
        OnRoundWon?.Invoke(RoundDuration);
    }

    /// <summary>Instancia un VFX y compensa el timeScale actual escalando los Animator/
    /// ParticleSystem hijos, para que la animacion se vea a velocidad normal aunque el
    /// juego este en slow-motion durante la cinematica.</summary>
    private GameObject SpawnVfx(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, pos, Quaternion.identity);
        float ts = Mathf.Max(0.01f, Time.timeScale);
        float compensation = 1f / ts;
        foreach (var anim in go.GetComponentsInChildren<Animator>(true)) anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true)) { var main = ps.main; main.useUnscaledTime = true; }
        return go;
    }

    private IEnumerator KillAfterDelay(System.Collections.Generic.List<Game.AI.BaseEnemyAI> targets, float delay)
    {
        yield return WaitUnscaled(delay);
        if (targets == null) yield break;
        foreach (var e in targets) {
            if (e == null) continue;
            var dr = e.GetComponent<DamageReceiver>();
            if (dr != null) dr.ApplyDamage(999999, false, false, Vector2.zero);
            else Destroy(e.gameObject);
        }
    }

    private IEnumerator DestroyAfterDelay(GameObject go, float delay)
    {
        yield return WaitUnscaled(delay);
        if (go != null) Destroy(go);
    }

    private IEnumerator WaitUnscaled(float seconds)
    {
        float t = 0f;
        while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
    }

    /// <summary>Crea un overlay full-screen blanco bajo el Canvas mas alto y tweenea su alpha.
    /// 0 -> peak (flashIn) -> hold (flashHold) -> 0 (flashOut). Luego se destruye.</summary>
    private IEnumerator ScreenFlash()
    {
        Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) yield break;
        var go = new GameObject("CastleStage3Flash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        Color c = flashColor; c.a = 0f; img.color = c;

        // Fade in
        float t = 0f;
        while (t < flashIn) { t += Time.unscaledDeltaTime; float k = Mathf.Clamp01(t / flashIn); c.a = k * flashPeakAlpha; img.color = c; yield return null; }
        // Hold
        t = 0f;
        while (t < flashHold) { t += Time.unscaledDeltaTime; yield return null; }
        // Fade out
        t = 0f;
        while (t < flashOut) { t += Time.unscaledDeltaTime; float k = Mathf.Clamp01(t / flashOut); c.a = (1f - k) * flashPeakAlpha; img.color = c; yield return null; }
        Destroy(go);
    }

    // ========== Niveles y SFX cristal ==========

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
