using UnityEngine;

public sealed class PooledObject : MonoBehaviour
{
    public ObjectPool Pool { get; private set; }

    public void AssignPool(ObjectPool pool)
    {
        Pool = pool;
    }

    public void ReturnToPool()
    {
        if (Pool == null)
        {
            gameObject.SetActive(false);
            return;
        }

        Pool.Release(gameObject);
    }
}
