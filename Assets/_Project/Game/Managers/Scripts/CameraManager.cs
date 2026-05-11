using UnityEngine;
using Unity.Cinemachine;

namespace Game.Managers
{

/// <summary>
/// Manager unificado de cámara: free-move WASD del rig, follow target, y zoom scroll
/// sobre la CinemachineCamera. Singleton MonoBehaviour con API pública estática para
/// que los consumidores (SelectionManager, PlayerUnit) no necesiten field serializado.
///
/// Sustituye a CameraFollowController + CameraZoom. Vive como GO standalone bajo
/// ---GAME--- y referencia al rig y a la vcam que viven bajo ---CAMERA---.
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

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>
    /// Asigna la unidad a la que seguir. Si <c>target</c> es null, libera el follow
    /// y vuelve al modo free-move WASD. No-op si la instancia no está activa.
    /// </summary>
    public static void SetFollowTarget(Transform target)
    {
        if (instance == null) return;
        instance.followTarget = target;
        if (target != null && instance.followRig != null)
        {
            Vector3 p = instance.followRig.position;
            p.x = target.position.x;
            p.y = target.position.y;
            instance.followRig.position = p;
        }
    }

    private void Update()
    {
        HandleFreeMove();
        HandleZoom();
    }

    private void LateUpdate()
    {
        if (followTarget == null || followRig == null) return;
        Vector3 p = followTarget.position;
        p.z = followRig.position.z;
        followRig.position = p;
    }

    private void HandleFreeMove()
    {
        if (followTarget != null) return;
        if (followRig == null) return;
        Vector2 dir = ReadWasd();
        followRig.position += (Vector3)(dir * freeMoveSpeed * Time.deltaTime);
    }

    private void HandleZoom()
    {
        if (vcam == null) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0f) return;
        var lens = vcam.Lens;
        lens.OrthographicSize -= scroll * zoomSpeed;
        lens.OrthographicSize = Mathf.Clamp(lens.OrthographicSize, minOrthoSize, maxOrthoSize);
        vcam.Lens = lens;
    }

    /// <summary>
    /// Helper de input WASD direccional, normalizado. Estático para que otros
    /// sistemas (p. ej. control de la unidad seleccionada) puedan reutilizarlo
    /// sin acoplarse a una instancia de cámara.
    /// </summary>
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
