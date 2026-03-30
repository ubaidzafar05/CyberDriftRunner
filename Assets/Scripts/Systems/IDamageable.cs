using UnityEngine;

public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(int damage, Vector3 hitPoint);
}
