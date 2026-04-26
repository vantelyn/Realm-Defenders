using UnityEngine;

[DisallowMultipleComponent]
public class CameraFollowController : MonoBehaviour
{
    [SerializeField] private float freeMoveSpeed = 8f;

    private Transform followTarget;

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        if (target != null)
        {
            Vector3 p = transform.position;
            p.x = target.position.x;
            p.y = target.position.y;
            transform.position = p;
        }
    }

    private void Update()
    {
        if (followTarget != null) return;
        Vector2 dir = ReadWasd();
        transform.position += (Vector3)(dir * freeMoveSpeed * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;
        Vector3 p = followTarget.position;
        p.z = transform.position.z;
        transform.position = p;
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