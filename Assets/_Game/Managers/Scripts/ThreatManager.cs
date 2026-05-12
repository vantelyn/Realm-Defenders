using System.Collections.Generic;
using UnityEngine;
using Game.Buildings;
using Game.Targeting;

namespace Game.Managers
{

/// <summary>
/// Registro central de StrategicTarget vivos. La amenaza total efectiva esta
/// capada dinamicamente: hasta que el Castle alcanza fase 2 el cap es 99,
/// despues 100. Polls cada frame para detectar cruces de los umbrales criticos
/// (15 = activacion de portales / war drums; 100 = activacion de bosses).
/// </summary>
[DefaultExecutionOrder(-1000)]
public class ThreatManager : MonoBehaviour
{
    private static ThreatManager instance;
    public static ThreatManager Instance => instance;

    private readonly List<StrategicTarget> targets = new List<StrategicTarget>();

    [Header("Thresholds")]
    [Tooltip("Amenaza minima para activar spawn de enemigos normales y disparar OnLowThresholdCrossed (war drums).")]
    [SerializeField] private float lowThreshold = 15f;
    [Tooltip("Amenaza a la que se activan los bosses y dispara OnHighThresholdCrossed (boss music). Requiere Castle en fase 2.")]
    [SerializeField] private float highThreshold = 100f;
    [Tooltip("Cap efectivo de amenaza antes de que el Castle alcance fase 2.")]
    [SerializeField] private float preStage2Cap = 99f;
    [Tooltip("Cap efectivo de amenaza una vez el Castle esta en fase 2 o superior.")]
    [SerializeField] private float stage2Cap = 100f;

    [Header("Audio Stings")]
    [Tooltip("AudioSource global para los stings al cruzar umbrales. Si esta vacio, se anade uno en Awake.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Clip al cruzar el umbral bajo (tambores de guerra).")]
    [SerializeField] private AudioClip lowThresholdClip;
    [Tooltip("Clip al cruzar el umbral alto (bossfight music).")]
    [SerializeField] private AudioClip highThresholdClip;
    [Tooltip("Volumen base con el que se reproducen los stings."), Range(0f, 1f)]
    [SerializeField] private float stingVolume = 1f;

    private bool crossedLow;
    private bool crossedHigh;

    public static event System.Action OnRegistryChanged;
    public static event System.Action OnLowThresholdCrossed;
    public static event System.Action OnHighThresholdCrossed;

    public static IReadOnlyList<StrategicTarget> All =>
        instance != null ? (IReadOnlyList<StrategicTarget>)instance.targets : System.Array.Empty<StrategicTarget>();
    public static int Count => instance != null ? instance.targets.Count : 0;

    /// <summary>Cap actual segun fase del Castle.</summary>
    public static float CurrentCap
    {
        get
        {
            if (instance == null) return 99f;
            bool stage2 = Castle.Instance != null && Castle.Instance.CurrentLevel >= 2;
            return stage2 ? instance.stage2Cap : instance.preStage2Cap;
        }
    }

    public static float LowThreshold => instance != null ? instance.lowThreshold : 15f;
    public static float HighThreshold => instance != null ? instance.highThreshold : 100f;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f; // 2D global
        }
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        instance.targets.Clear();
        instance = null;
    }

    private void Update()
    {
        float effective = GetTotalThreat();
        // Cruce ascendente del umbral bajo
        if (!crossedLow && effective >= lowThreshold)
        {
            crossedLow = true;
            PlaySting(lowThresholdClip);
            if (OnLowThresholdCrossed != null) OnLowThresholdCrossed();
        }
        else if (crossedLow && effective < lowThreshold) crossedLow = false;

        // Cruce ascendente del umbral alto (solo posible con Castle en fase 2 por el cap)
        if (!crossedHigh && effective >= highThreshold)
        {
            crossedHigh = true;
            PlaySting(highThresholdClip);
            if (OnHighThresholdCrossed != null) OnHighThresholdCrossed();
        }
        else if (crossedHigh && effective < highThreshold) crossedHigh = false;
    }

    private void PlaySting(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, stingVolume);
    }

    public static void Register(StrategicTarget target)
    {
        if (instance == null || target == null) return;
        if (instance.targets.Contains(target)) return;
        instance.targets.Add(target);
        OnRegistryChanged?.Invoke();
    }

    public static void Unregister(StrategicTarget target)
    {
        if (instance == null || target == null) return;
        if (instance.targets.Remove(target)) OnRegistryChanged?.Invoke();
    }

    public static void NotifyThreatLevelChanged(StrategicTarget target)
    {
        if (instance == null || target == null) return;
        if (!instance.targets.Contains(target)) return;
        OnRegistryChanged?.Invoke();
    }

    public static StrategicTarget GetTopThreat()
    {
        if (instance == null) return null;
        var list = instance.targets;
        float bestLevel = float.NegativeInfinity;
        int tieCount = 0;
        for (int i = 0; i < list.Count; i++)
        {
            StrategicTarget t = list[i];
            if (t == null) continue;
            if (t.ThreatLevel > bestLevel) { bestLevel = t.ThreatLevel; tieCount = 1; }
            else if (Mathf.Approximately(t.ThreatLevel, bestLevel)) tieCount++;
        }
        if (tieCount == 0) return null;
        int pick = Random.Range(0, tieCount);
        int seen = 0;
        for (int i = 0; i < list.Count; i++)
        {
            StrategicTarget t = list[i];
            if (t == null) continue;
            if (Mathf.Approximately(t.ThreatLevel, bestLevel))
            {
                if (seen == pick) return t;
                seen++;
            }
        }
        return null;
    }

    /// <summary>Amenaza total efectiva (suma bruta capada por el limite actual segun fase del Castle).</summary>
    public static float GetTotalThreat()
    {
        return Mathf.Min(GetRawThreat(), CurrentCap);
    }

    /// <summary>Suma bruta sin cap. Util para diagnostico/logging.</summary>
    public static float GetRawThreat()
    {
        if (instance == null) return 0f;
        float total = 0f;
        var list = instance.targets;
        for (int i = 0; i < list.Count; i++) if (list[i] != null) total += list[i].ThreatLevel;
        return total;
    }

    public static void Clear()
    {
        if (instance == null) return;
        instance.targets.Clear();
        OnRegistryChanged?.Invoke();
    }
}
}
