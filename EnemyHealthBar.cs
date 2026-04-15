using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Привязка")]
    [SerializeField] public Transform target;

    [Header("Позиция")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);

    [Header("Блокировка вращения")]
    [SerializeField] private bool lockYRotation = true;
    [SerializeField] private bool lockZRotation = true;
    [SerializeField] private bool lockXRotation = false;

    [Header("Настройки")]
    [SerializeField] private int maxSegments = 5;
    [SerializeField] private Image fillImage;


    void Awake()
    {
        if (fillImage == null)
        {
            Image[] allImages = GetComponentsInChildren<Image>(true);
            foreach (Image img in allImages)
            {
                if (img.type == Image.Type.Filled)
                {
                    fillImage = img;
                    break;
                }
            }
        }

        if (fillImage != null)
        {
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;

        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);

            Vector3 currentRotation = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(
                lockXRotation ? 0 : currentRotation.x+42,
                lockYRotation ? 0 : currentRotation.y,
                lockZRotation ? 0 : currentRotation.z
            );
        }
    }

    public void UpdateHealth(int current, int max)
    {
        if (fillImage == null) return;

        float percent = (float)current / max;
        fillImage.fillAmount = percent;
    }

    public void SetMaxSegments(int newMax)
    {
        maxSegments = newMax;
    }
}
