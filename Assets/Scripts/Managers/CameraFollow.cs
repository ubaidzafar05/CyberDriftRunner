using UnityEngine;

public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -9f);
    [SerializeField] private float followSharpness = 8f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSharpness * Time.unscaledDeltaTime);
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
