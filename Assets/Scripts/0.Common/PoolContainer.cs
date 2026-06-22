using UnityEngine;
using System.Collections.Generic;

public class PoolContainer
{
    private readonly string _poolName;
    private readonly GameObject _prefab;
    private readonly Transform _containerTransform;
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();

    public PoolContainer(string poolName, GameObject prefab, int initialCount, Transform containerTr)
    {
        _poolName = poolName;
        _prefab = prefab;
        _containerTransform = containerTr;

        // 초기 수량만큼 미리 생성
        for (int i = 0; i < initialCount; i++)
        {
            CreateNewObject(true);
        }
    }

    public GameObject Get()
    {
        GameObject obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
        }
        else
        {
            // 풀이 비어있다면 새로 생성합니다.
            obj = CreateNewObject(false);
        }

        obj.SetActive(true);
        obj.transform.SetParent(null);
        return obj;
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(_containerTransform);
        _pool.Enqueue(obj);
    }

    private GameObject CreateNewObject(bool initiallyInactive)
    {
        GameObject newObj = Object.Instantiate(_prefab, _containerTransform);

        if (!newObj.TryGetComponent<PoolObject>(out var poolObject))
        {
            poolObject = newObj.AddComponent<PoolObject>();
        }

        poolObject.poolName = _poolName;

        if (initiallyInactive)
        {
            newObj.SetActive(false);
            _pool.Enqueue(newObj);
        }

        return newObj;
    }
}