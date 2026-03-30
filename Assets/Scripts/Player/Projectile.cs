using UnityEngine;

public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.45f;
    [SerializeField] private float maxLifetime = 3f;

    private int damage;
    private float speed;
    private float despawnAt;
    private IDamageable target;
    private PooledObject pooledObject;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        pooledObject = pooledObject == null ? GetComponent<PooledObject>() : pooledObject;
        despawnAt = Time.time + maxLifetime;
    }

    private void Update()
    {
        if (target == null || !target.IsAlive || Time.time >= despawnAt)
        {
            ReturnToPool();
            return;
        }

        Transform targetTransform = ((MonoBehaviour)target).transform;
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetTransform.position, speed * Time.deltaTime);
        transform.position = nextPosition;
        transform.LookAt(targetTransform.position);

        if (Vector3.SqrMagnitude(transform.position - targetTransform.position) <= hitRadius * hitRadius)
        {
            target.TakeDamage(damage, transform.position);
            ReturnToPool();
        }
    }

    public void Launch(IDamageable nextTarget, float projectileSpeed, int projectileDamage)
    {
        target = nextTarget;
        speed = projectileSpeed;
        damage = projectileDamage;
        despawnAt = Time.time + maxLifetime;
    }

    private void ReturnToPool()
    {
        target = null;
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
