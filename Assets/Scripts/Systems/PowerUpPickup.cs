using UnityEngine;

public sealed class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private PowerUpType powerUpType = PowerUpType.Shield;
    [SerializeField] private float duration = 5f;
    [SerializeField] private int scoreBonus = 20;
    [SerializeField] private float spinSpeed = 180f;

    private PooledObject pooledObject;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        pooledObject = pooledObject == null ? GetComponent<PooledObject>() : pooledObject;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        ReturnWhenBehindPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            return;
        }

        player.PowerUps.ApplyPowerUp(powerUpType, duration);
        GameManager.Instance?.AddScore(scoreBonus);
        AudioManager.Instance?.PlayPowerUp();
        player.GetComponent<PlayerVfxController>()?.OnPowerUp();
        ScreenFlash.Instance?.FlashPowerUp();
        ReturnToPool();
    }

    private void ReturnWhenBehindPlayer()
    {
        if (GameManager.Instance?.Player == null)
        {
            return;
        }

        if (transform.position.z < GameManager.Instance.Player.transform.position.z - 8f)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (pooledObject != null)
        {
            pooledObject.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
