using UnityEngine;

public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.45f;
    [SerializeField] private float maxLifetime = 3f;

    private int _damage;
    private float _speed;
    private float _despawnAt;
    private IDamageable _target;
    private MonoBehaviour _targetBehaviour;
    private PooledObject _pooledObject;

    private void Awake()
    {
        _pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        _pooledObject = _pooledObject == null ? GetComponent<PooledObject>() : _pooledObject;
        _despawnAt = Time.time + maxLifetime;
    }

    private void Update()
    {
        if (_targetBehaviour == null || !_target.IsAlive || Time.time >= _despawnAt)
        {
            ReturnToPool();
            return;
        }

        Transform targetTransform = _targetBehaviour.transform;
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetTransform.position, _speed * Time.deltaTime);
        transform.position = nextPosition;
        transform.LookAt(targetTransform.position);

        if (Vector3.SqrMagnitude(transform.position - targetTransform.position) <= hitRadius * hitRadius)
        {
            _target.TakeDamage(_damage, transform.position);
            ReturnToPool();
        }
    }

    public void Launch(IDamageable nextTarget, float projectileSpeed, int projectileDamage)
    {
        _target = nextTarget;
        _targetBehaviour = nextTarget as MonoBehaviour;
        _speed = projectileSpeed;
        _damage = projectileDamage;
        _despawnAt = Time.time + maxLifetime;
    }

    private void ReturnToPool()
    {
        _target = null;
        _targetBehaviour = null;
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
