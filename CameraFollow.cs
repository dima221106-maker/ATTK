using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -70);

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPosition = Vector3.zero;
        targetPosition.x = player.position.x + offset.x;
        targetPosition.y = player.position.y + offset.y;
        targetPosition.z = offset.z;

        transform.position = targetPosition;
    }
}