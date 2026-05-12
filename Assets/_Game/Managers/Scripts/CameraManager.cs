using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace Game.Managers
{

/// <summary>
/// Manager unificado de camara: free-move WASD, follow target, zoom scroll y
/// secuencias cinematicas (FocusOn). Durante una cinematica WASD/zoom y follow
/// quedan suspendidos. Usa unscaled time, asi que funciona con timeScale=0.
/// </summary>
[DefaultExecutionOrder(-900)]
public class CameraManager : MonoBehaviour
{
    private static CameraManager instance;
    public static CameraManager Instance => instance;

    [Header("References")]
    [SerializeField] private Transform followRig;
    [SerializeField] private CinemachineCamera vcam;

    [Header("Free Move")]
    [SerializeField] private float freeMoveSpeed = 8f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minOrthoSize = 2f;
    [SerializeField] private float maxOrthoSize = 15f;

    private Transform followTarget;
    private bool isCinematic;
    public static bool IsCinematic => instance != null && instance.isCinematic;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnDestroy() { if (instance == this) instance = null; }

    public static void SetFollowTarget(Transform target)
    {
        if (instance == null) return;
        instance.followTarget = target;
        if (target != null && instance.followRig != null)
        {
            Vector3 p = instance.followRig.position;
            p.x = target.position.x; p.y = target.position.y;
            instance.followRig.position = p;
        }
    }

    /// <summary>
    /// Cinematica: lerp suave del rig hacia 'target', invoca onArrived, espera
    /// holdTime, vuelve a la posicion original, invoca onComplete. Usa unscaled
    /// time. Mientras dura, WASD/zoom/follow quedan bloqueados.
    /// </summary>
    /// <summary>FocusOn sin zoom: mantiene el orthographic size actual.</summary>
    public static Coroutine FocusOn(Transform target, float travelTime, float holdTime, System.Action onArrived, System.Action onComplete)
        => FocusOn(target, travelTime, holdTime, -1f, onArrived, onComplete);

    /// <summary>FocusOn con zoom: durante el lerp, vcam.Lens.OrthographicSize tweenea
    /// hacia targetOrthoSize. A la vuelta, se restaura al valor original. Si
    /// targetOrthoSize <= 0, el zoom no se toca.</summary>
    public static Coroutine FocusOn(Transform target, float travelTime, float holdTime, float targetOrthoSize, System.Action onArrived, System.Action onComplete)
        => FocusOn(target, travelTime, holdTime, targetOrthoSize, 0f, onArrived, onComplete);

    /// <summary>Variante con arrivalDelay: pausa adicional entre el fin del lerp y onArrived
    /// (en segundos unscaled). Da margen para que el jugador asimile la llegada antes del SFX.</summary>
    public static Coroutine FocusOn(Transform target, float travelTime, float holdTime, float targetOrthoSize, float arrivalDelay, System.Action onArrived, System.Action onComplete)
    {
        if (instance == null || instance.followRig == null || target == null) { onArrived?.Invoke(); onComplete?.Invoke(); return null; }
        return instance.StartCoroutine(instance.FocusRoutine(target, travelTime, holdTime, targetOrthoSize, arrivalDelay, onArrived, onComplete));
    }

    private IEnumerator FocusRoutine(Transform target, float travelTime, float holdTime, float targetOrthoSize, float arrivalDelay, System.Action onArrived, System.Action onComplete)
    {
        isCinematic = true;
        var brain = Camera.main != null ? Camera.main.GetComponent<Unity.Cinemachine.CinemachineBrain>() : null;
        bool prevIgnoreTimeScale = false;
        if (brain != null) { prevIgnoreTimeScale = brain.IgnoreTimeScale; brain.IgnoreTimeScale = true; }

        Vector3 startPos = followRig.position;
        Vector3 endPos = new Vector3(target.position.x, target.position.y, startPos.z);
        float startOrtho = vcam != null ? vcam.Lens.OrthographicSize : 0f;
        float endOrtho = targetOrthoSize > 0f ? targetOrthoSize : startOrtho;

        yield return TweenRigAndZoom(startPos, endPos, startOrtho, endOrtho, travelTime);

        // Espera tras la llegada antes de invocar onArrived (efecto/SFX del castillo)
        float ad = 0f;
        while (ad < arrivalDelay) { ad += Time.unscaledDeltaTime; yield return null; }

        if (onArrived != null) onArrived();

        float t = 0f;
        while (t < holdTime) { t += Time.unscaledDeltaTime; yield return null; }

        yield return TweenRigAndZoom(followRig.position, startPos, endOrtho, startOrtho, travelTime);
        if (brain != null) brain.IgnoreTimeScale = prevIgnoreTimeScale;
        if (onComplete != null) onComplete();
        isCinematic = false;
    }

    private IEnumerator TweenRigAndZoom(Vector3 fromPos, Vector3 toPos, float fromOrtho, float toOrtho, float duration)
    {
        if (duration <= 0.0001f) {
            followRig.position = toPos;
            if (vcam != null && !Mathf.Approximately(fromOrtho, toOrtho)) { var l = vcam.Lens; l.OrthographicSize = toOrtho; vcam.Lens = l; }
            yield break;
        }
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float s = k * k * (3f - 2f * k);
            followRig.position = Vector3.LerpUnclamped(fromPos, toPos, s);
            if (vcam != null && !Mathf.Approximately(fromOrtho, toOrtho))
            {
                var l = vcam.Lens; l.OrthographicSize = Mathf.LerpUnclamped(fromOrtho, toOrtho, s); vcam.Lens = l;
            }
            yield return null;
        }
        followRig.position = toPos;
        if (vcam != null && !Mathf.Approximately(fromOrtho, toOrtho)) { var l = vcam.Lens; l.OrthographicSize = toOrtho; vcam.Lens = l; }
    }

    private void Update()
    {
        if (isCinematic) return;
        HandleFreeMove();
        HandleZoom();
    }

    private void LateUpdate()
    {
        if (isCinematic) return;
        if (followTarget == null || followRig == null) return;
        Vector3 p = followTarget.position;
        p.z = followRig.position.z;
        followRig.position = p;
    }

    private void HandleFreeMove()
    {
        if (followTarget != null || followRig == null) return;
        Vector2 dir = ReadWasd();
        followRig.position += (Vector3)(dir * freeMoveSpeed * Time.unscaledDeltaTime);
    }

    private void HandleZoom()
    {
        if (vcam == null) return;
        if (PauseManager.IsPaused) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;
        var lens = vcam.Lens;
        lens.OrthographicSize -= scroll * zoomSpeed;
        lens.OrthographicSize = Mathf.Clamp(lens.OrthographicSize, minOrthoSize, maxOrthoSize);
        vcam.Lens = lens;
    }

    public static Vector2 ReadWasd()
    {
        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.W)) v += 1f;
        if (Input.GetKey(KeyCode.S)) v -= 1f;
        if (Input.GetKey(KeyCode.A)) h -= 1f;
        if (Input.GetKey(KeyCode.D)) h += 1f;
        Vector2 dir = new Vector2(h, v);
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }
}
}
