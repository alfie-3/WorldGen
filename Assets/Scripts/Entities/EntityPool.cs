using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

public class EntityPool
{
    public ObjectPool<EntityContainer> Pool { get; private set; }

    EntityContainer entityPrefab;

    public void Init(EntityContainer entityPrefab)
    {
        Pool = new(CreateEntity, OnTakeEntity, OnReturnToPool, OnDestroyEntity);
        this.entityPrefab = entityPrefab;
    }

    private EntityContainer CreateEntity()
    {
        EntityContainer container = GameObject.Instantiate(entityPrefab).GetComponent<EntityContainer>();

        return container;
    }

    private void OnTakeEntity(EntityContainer container)
    {
        container.gameObject.SetActive(true);
    }

    private void OnReturnToPool(EntityContainer container)
    {
        container.gameObject.SetActive(false);
    }

    private void OnDestroyEntity(EntityContainer container)
    {
        GameObject.Destroy(container.gameObject);
    }
}