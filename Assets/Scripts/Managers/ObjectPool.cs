using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool
{
    private readonly Queue<GameObject> availableObjects = new Queue<GameObject>();
    private readonly GameObject prefab;
    private readonly Transform root;

    public ObjectPool(GameObject prefab, int preloadCount, Transform root)
    {
        this.prefab = prefab;
        this.root = root;

        for (int i = 0; i < preloadCount; i++)
        {
            availableObjects.Enqueue(CreateInstance());
        }
    }

    public GameObject Acquire(Vector3 position, Quaternion rotation)
    {
        GameObject instance = availableObjects.Count > 0 ? availableObjects.Dequeue() : CreateInstance();
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        return instance;
    }

    public void Release(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(root, false);
        availableObjects.Enqueue(instance);
    }

    private GameObject CreateInstance()
    {
        GameObject instance = Object.Instantiate(prefab, root);
        instance.name = prefab.name;
        instance.SetActive(false);

        PooledObject pooledObject = instance.GetComponent<PooledObject>();
        if (pooledObject == null)
        {
            pooledObject = instance.AddComponent<PooledObject>();
        }

        pooledObject.AssignPool(this);
        return instance;
    }
}
