using UnityEngine;
using Unity.Cinemachine;

namespace Game.CameraSystem
{

public class CameraZoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minSize = 2f;
    public float maxSize = 15f;

    CinemachineCamera vcam;

    void Start()
    {
        vcam = GetComponent<CinemachineCamera>();
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            var lens = vcam.Lens;
            lens.OrthographicSize -= scroll * zoomSpeed;
            lens.OrthographicSize = Mathf.Clamp(lens.OrthographicSize, minSize, maxSize);
            vcam.Lens = lens;
        }
    }
}
}
