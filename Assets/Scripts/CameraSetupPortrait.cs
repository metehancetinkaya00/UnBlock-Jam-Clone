using UnityEngine;

public class CameraSetupPortrait : MonoBehaviour
{
    public Transform lookAt;
    public Vector3 offset = new Vector3(0f, 0.8f, -10f);
    public float fov = 45f;

    void Start()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.fieldOfView = fov;

        if (lookAt != null)
        {
            cam.transform.position = lookAt.position + offset;
            cam.transform.LookAt(lookAt.position);
        }
    }
}
