using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Entity Tile Data", menuName = "Tiles/New Entity Tile")]
public class EntityTileData : TileData, IEntityData
{
    [field: SerializeField] public EntityData EntityData { get; private set; }

    public EntityData GetEntityData() => EntityData;
}
public interface IEntityData
{
    public EntityData GetEntityData();
}
