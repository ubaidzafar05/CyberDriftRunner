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

        IDamageable target = FindNearestTarget();
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

    private IDamageable FindNearestTarget()
    {
        IDamageable bestTarget = FindNearestDrone();
        float bestDistance = GetTargetDistance(bestTarget);
        BossController boss = BossController.ActiveBoss;
        if (boss == null || !boss.IsAlive)
        {
            return bestTarget;
        }

        float bossDistance = GetTargetDistance(boss);
        return bossDistance < bestDistance ? boss : bestTarget;
    }

    private IDamageable FindNearestDrone()
    {
        EnemyDrone bestTarget = null;
        float bestDistance = targetRange * targetRange;

        foreach (EnemyDrone drone in EnemyDrone.ActiveDrones)
        {
            if (drone == null || !drone.IsAlive)
            {
                continue;
            }

            float distance = GetTargetDistance(drone);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestTarget = drone;
        }

        return bestTarget;
    }

    private float GetTargetDistance(IDamageable target)
    {
        MonoBehaviour targetBehaviour = target as MonoBehaviour;
        if (targetBehaviour == null)
        {
            return float.PositiveInfinity;
        }

        Vector3 offset = targetBehaviour.transform.position - transform.position;
        if (offset.z < -1f)
        {
            return float.PositiveInfinity;
        }

        float distance = offset.sqrMagnitude;
        return distance <= targetRange * targetRange ? distance : float.PositiveInfinity;
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
