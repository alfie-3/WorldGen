using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Static Entity", menuName = "Entities/Static")]
public class StaticEntity : EntityData
{
    [field: SerializeField] public EntityContainer EntityPrefab { get; private set; }

    public override void OnLoad()
    {
        base.OnLoad();
    }

    public override void OnUnload()
    {
        base.OnUnload();
    }
}
