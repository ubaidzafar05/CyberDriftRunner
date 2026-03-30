using UnityEngine;

public sealed class CreditPickup : MonoBehaviour
{
    [SerializeField] private int creditValue = 1;
    [SerializeField] private int scoreBonus = 5;
    [SerializeField] private float floatHeight = 0.35f;
    [SerializeField] private float floatSpeed = 3f;

    private PooledObject pooledObject;
    private Vector3 startPosition;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        pooledObject = pooledObject == null ? GetComponent<PooledObject>() : pooledObject;
        startPosition = transform.position;
    }

    private void Update()
    {
        float bobOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, startPosition.y + bobOffset, transform.position.z);
        ReturnWhenBehindPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null)
        {
            return;
        }

        GameManager.Instance.AddCredits(creditValue);
        GameManager.Instance.AddScore(scoreBonus);
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
