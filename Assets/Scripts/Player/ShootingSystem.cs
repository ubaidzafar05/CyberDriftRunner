using UnityEngine;

public sealed class ShootingSystem : MonoBehaviour
{
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectilePoolRoot;
    [SerializeField] private float targetRange = 28f;
    [SerializeField] private float fireCooldown = 0.2f;
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private int preloadCount = 12;

    private ObjectPool projectilePool;
    private float nextFireTime;

    private void Awake()
    {
        EnsurePool();
    }

    public void Configure(Transform newMuzzle, GameObject newProjectilePrefab, Transform newProjectilePoolRoot)
    {
        muzzle = newMuzzle;
        projectilePrefab = newProjectilePrefab;
        projectilePoolRoot = newProjectilePoolRoot;
        projectilePool = null;
        EnsurePool();
    }

    public bool TryShootNearestTarget()
    {
        if (Time.time < nextFireTime || projectilePool == null)
        {
            return false;
        }

        EnemyDrone target = FindNearestTarget();
        if (target == null)
        {
            return false;
        }

        nextFireTime = Time.time + fireCooldown;
        GameObject projectileObject = projectilePool.Acquire(muzzle.position, Quaternion.identity);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(target, projectileSpeed, projectileDamage);
        return true;
    }

    private EnemyDrone FindNearestTarget()
    {
        EnemyDrone bestTarget = null;
        float bestDistance = targetRange * targetRange;

        foreach (EnemyDrone drone in EnemyDrone.ActiveDrones)
        {
            if (drone == null || !drone.IsAlive)
            {
                continue;
            }

            Vector3 offset = drone.transform.position - transform.position;
            if (offset.z < -1f)
            {
                continue;
            }

            float distance = offset.sqrMagnitude;
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestTarget = drone;
        }

        return bestTarget;
    }

    private void EnsurePool()
    {
        if (muzzle == null)
        {
            muzzle = transform;
        }

        if (projectilePoolRoot == null)
        {
            projectilePoolRoot = new GameObject("ProjectilePool").transform;
            projectilePoolRoot.SetParent(transform, false);
        }

        if (projectilePrefab != null && projectilePool == null)
        {
            projectilePool = new ObjectPool(projectilePrefab, preloadCount, projectilePoolRoot);
        }
    }
}
