using UnityEngine;

public sealed class CreditPickup : MonoBehaviour
{
    [SerializeField] private int creditValue = 1;
    [SerializeField] private int scoreBonus = 5;
    [SerializeField] private float floatHeight = 0.35f;
    [SerializeField] private float floatSpeed = 3f;

    private PooledObject _pooledObject;
    private float _baseY;
    private bool _baseYSet;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        _pooledObject = _pooledObject == null ? GetComponent<PooledObject>() : _pooledObject;
        _baseYSet = false;
    }

    private void Update()
    {
        if (!_baseYSet)
        {
            _baseY = transform.position.y;
            _baseYSet = true;
        }

        float bobOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(transform.position.x, _baseY + bobOffset, transform.position.z);
        ReturnWhenBehindPlayer();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == null)
        {
            return;
        }

        GameManager.Instance?.AddCredits(creditValue);
        GameManager.Instance?.AddScore(scoreBonus);
        ComboSystem.Instance?.RegisterPickup();
        HapticFeedback.Instance?.VibrateOnCollect();
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
        if (_pooledObject != null)
        {
            _pooledObject.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
