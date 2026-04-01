using UnityEngine;

public sealed class LoopingTrafficProp : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float startZ = 12f;
    [SerializeField] private float endZ = 240f;
    [SerializeField] private float laneX = 10f;

    public void Configure(float propSpeed, float start, float end, float xPosition)
    {
        speed = propSpeed;
        startZ = start;
        endZ = end;
        laneX = xPosition;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = laneX;
        position.z -= speed * Time.deltaTime;
        if (position.z < startZ)
        {
            position.z = endZ;
        }

        transform.position = position;
    }
}
