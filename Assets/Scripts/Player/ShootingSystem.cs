using UnityEngine;

public enum WeaponType
{
    Pulse,
    Burst,
    Spread,
    Plasma
}

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
    [SerializeField] private WeaponType startingWeapon = WeaponType.Pulse;
    [SerializeField] private float burstWeaponDistance = 550f;
    [SerializeField] private float spreadWeaponDistance = 1250f;
    [SerializeField] private float plasmaWeaponDistance = 2200f;

    private ObjectPool projectilePool;
    private float nextFireTime;
    private WeaponType currentWeapon;
    private Color projectileTint = new Color(0.15f, 0.95f, 1f);
    private readonly System.Collections.Generic.List<IDamageable> targetBuffer = new System.Collections.Generic.List<IDamageable>(4);

    public WeaponType CurrentWeapon => currentWeapon;

    private void Awake()
    {
        currentWeapon = startingWeapon;
        ApplyShopCosmetics();
        EnsurePool();
    }

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        WeaponType nextWeapon = ResolveWeaponByDistance(GameManager.Instance.Distance);
        if (nextWeapon != currentWeapon)
        {
            currentWeapon = nextWeapon;
            AudioManager.Instance?.PlayPowerUp();
            ScreenFlash.Instance?.FlashPowerUp();
            Debug.Log($"[ShootingSystem] Weapon upgraded to {currentWeapon}");
        }
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

        targetBuffer.Clear();
        CollectTargets(targetBuffer, currentWeapon == WeaponType.Spread ? 3 : 1);
        if (targetBuffer.Count == 0)
        {
            return false;
        }

        float cooldown = GetWeaponCooldown();
        nextFireTime = Time.time + cooldown;
        switch (currentWeapon)
        {
            case WeaponType.Burst:
                FireProjectile(targetBuffer[0], projectileSpeed * 1.1f, projectileDamage, 0.9f, new Vector3(-0.18f, 0.04f, 0f), GetWeaponTint(currentWeapon, 0));
                FireProjectile(targetBuffer[0], projectileSpeed * 1.2f, projectileDamage, 0.82f, new Vector3(0.18f, -0.04f, 0f), GetWeaponTint(currentWeapon, 1));
                return true;
            case WeaponType.Spread:
                for (int i = 0; i < targetBuffer.Count; i++)
                {
                    float laneOffset = (i - 1) * 0.32f;
                    FireProjectile(targetBuffer[i], projectileSpeed * 0.95f, projectileDamage, 0.8f, new Vector3(laneOffset, 0f, 0f), GetWeaponTint(currentWeapon, i));
                }
                return true;
            case WeaponType.Plasma:
                FireProjectile(targetBuffer[0], projectileSpeed * 0.9f, projectileDamage * 3, 1.35f, new Vector3(0f, 0.05f, 0f), GetWeaponTint(currentWeapon, 0));
                return true;
            default:
                FireProjectile(targetBuffer[0], projectileSpeed, projectileDamage, 1f, Vector3.zero, GetWeaponTint(currentWeapon, 0));
                return true;
        }
    }

    public string GetWeaponDisplayName()
    {
        switch (currentWeapon)
        {
            case WeaponType.Burst:
                return "Burst Caster";
            case WeaponType.Spread:
                return "Spread Arc";
            case WeaponType.Plasma:
                return "Plasma Cutter";
            default:
                return "Pulse Pistol";
        }
    }

    private void FireProjectile(IDamageable target, float speed, int damage, float scale, Vector3 localOffset, Color tint)
    {
        if (target == null || muzzle == null)
        {
            return;
        }

        GameObject projectileObject = projectilePool.Acquire(muzzle.TransformPoint(localOffset), Quaternion.identity);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(target, speed, damage, currentWeapon, scale, tint);
    }

    private Color GetWeaponTint(WeaponType weaponType, int shotIndex)
    {
        Color baseTint = projectileTint;
        switch (weaponType)
        {
            case WeaponType.Burst:
                return Color.Lerp(baseTint, shotIndex % 2 == 0 ? new Color(1f, 0.42f, 0.2f) : new Color(1f, 0.1f, 0.76f), 0.42f);
            case WeaponType.Spread:
                if (shotIndex == 0) return Color.Lerp(baseTint, new Color(0.16f, 1f, 0.8f), 0.32f);
                if (shotIndex == 1) return Color.Lerp(baseTint, new Color(0.28f, 0.9f, 1f), 0.22f);
                return Color.Lerp(baseTint, new Color(0.78f, 0.34f, 1f), 0.4f);
            case WeaponType.Plasma:
                return Color.Lerp(baseTint, new Color(1f, 0.42f, 0.18f), 0.58f);
            default:
                return Color.Lerp(baseTint, new Color(0.15f, 0.95f, 1f), 0.12f);
        }
    }

    private float GetWeaponCooldown()
    {
        switch (currentWeapon)
        {
            case WeaponType.Burst:
                return fireCooldown * 1.15f;
            case WeaponType.Spread:
                return fireCooldown * 1.35f;
            case WeaponType.Plasma:
                return fireCooldown * 1.6f;
            default:
                return fireCooldown;
        }
    }

    private WeaponType ResolveWeaponByDistance(float distance)
    {
        if (distance >= plasmaWeaponDistance)
        {
            return WeaponType.Plasma;
        }

        if (distance >= spreadWeaponDistance)
        {
            return WeaponType.Spread;
        }

        if (distance >= burstWeaponDistance)
        {
            return WeaponType.Burst;
        }

        return startingWeapon;
    }

    private void CollectTargets(System.Collections.Generic.List<IDamageable> targets, int maxTargets)
    {
        IDamageable nearest = FindNearestTarget();
        if (nearest == null)
        {
            return;
        }

        targets.Add(nearest);
        if (maxTargets <= 1)
        {
            return;
        }

        foreach (EnemyDrone drone in EnemyDrone.ActiveDrones)
        {
            if (targets.Count >= maxTargets)
            {
                return;
            }

            if (drone == null || !drone.IsAlive || ReferenceEquals(drone, nearest))
            {
                continue;
            }

            if (float.IsInfinity(GetTargetDistance(drone)))
            {
                continue;
            }

            targets.Add(drone);
        }

        BossController boss = BossController.ActiveBoss;
        if (targets.Count < maxTargets && boss != null && boss.IsAlive && !ReferenceEquals(boss, nearest))
        {
            if (!float.IsInfinity(GetTargetDistance(boss)))
            {
                targets.Add(boss);
            }
        }
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

        float actualRange = targetRange;
        if (UpgradeSystem.Instance != null)
        {
            actualRange *= UpgradeSystem.Instance.GetMultiplier(UpgradeSystem.UpgradeType.TargetRange);
        }

        Vector3 offset = targetBehaviour.transform.position - transform.position;
        if (offset.z < -1f)
        {
            return float.PositiveInfinity;
        }

        float distance = offset.sqrMagnitude;
        return distance <= actualRange * actualRange ? distance : float.PositiveInfinity;
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

    private void ApplyShopCosmetics()
    {
        string selectedWeaponSkin = ShopSystem.Instance != null ? ShopSystem.Instance.SelectedWeaponSkinId : "weapon_default";
        projectileTint = ShopCosmeticPalette.GetWeaponColor(selectedWeaponSkin);
    }
}
