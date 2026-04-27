using UnityEngine;

public sealed class CreditPickup : MonoBehaviour
{
    [SerializeField] private int creditValue = 1;
    [SerializeField] private int scoreBonus = 5;
    [SerializeField] private float floatHeight = 0.35f;
    [SerializeField] private float floatSpeed = 3f;
    [SerializeField] private float spinSpeed = 120f;
    [SerializeField] private Vector3 spinAxis = new Vector3(0.2f, 0.4f, 0.8f);
    [SerializeField] private float pulseAmplitude = 0.11f;

    private PooledObject _pooledObject;
    private float _baseY;
    private bool _baseYSet;
    private Vector3 _baseScale;
    private float _pulseSeed;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
        FlatActorFacade.EnsurePickupFacade(gameObject, new Color(0.92f, 0.78f, 0.24f), new Color(1f, 0.92f, 0.58f), false);
    }

    private void OnEnable()
    {
        _pooledObject = _pooledObject == null ? GetComponent<PooledObject>() : _pooledObject;
        _baseYSet = false;
        _baseScale = transform.localScale;
        _pulseSeed = Random.Range(0f, Mathf.PI * 2f);
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
        Vector3 normalizedSpinAxis = spinAxis.sqrMagnitude > 0.001f ? spinAxis.normalized : Vector3.forward;
        transform.Rotate(normalizedSpinAxis, spinSpeed * Time.deltaTime, Space.Self);
        transform.localScale = _baseScale * (1f + (Mathf.Sin((Time.time * 5f) + _pulseSeed) * pulseAmplitude));
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
        FloatingTextManager.Instance?.SpawnCredits(transform.position, creditValue);
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
