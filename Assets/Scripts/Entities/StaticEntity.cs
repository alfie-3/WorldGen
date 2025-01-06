using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName ="New Static Entity", menuName = "Entities/Static")]
public class StaticEntity : EntityData
{
    [field: SerializeField] public EntityContainer EntityPrefab { get; private set; }

    public EntityPool EntityPool {  get
        {
            if (entityPool == null)
            {
                entityPool = new EntityPool();
                entityPool.Init(EntityPrefab);
            }

            return entityPool;
        }
    }

    private EntityPool entityPool;

    public override void OnLoad()
    {
        base.OnLoad();
    }

    public override void OnUnload()
    {
        base.OnUnload();
    }
}
