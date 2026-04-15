using UnityEngine;

public class CameraScaler : MonoBehaviour
{
    [Header("📐 Настройки")]
    [SerializeField] private float referenceWidth = 1920f;
    [SerializeField] private float referenceHeight = 1080f;
    [SerializeField] private float orthographicSize = 5f;

    void Start()
    {
        AdjustCamera();
    }

    void AdjustCamera()
    {
        float targetAspect = referenceWidth / referenceHeight;
        float windowAspect = (float)Screen.width / (float)Screen.height;

        float scaleHeight = windowAspect / targetAspect;

        Camera camera = GetComponent<Camera>();

        if (camera.orthographic)
        {
            if (scaleHeight < 1.0f)
            {
                camera.orthographicSize = orthographicSize / scaleHeight;
            }
            else
            {
                camera.orthographicSize = orthographicSize;
            }
        }
    }
}
