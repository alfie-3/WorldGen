using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(fileName = "New Entity Tile Data", menuName = "Tiles/New Entity Tile")]
public class EntityTileData : TileData, IEntityData
{
    [field: SerializeField] public EntityData EntityData { get; private set; }

    public EntityData GetEntityData() => EntityData;

    public override TileData GetTileData(Vector3Int position, ref byte rotation)
    {
        System.Random random = new System.Random(position.sqrMagnitude);

        rotation = (byte)random.Next(3);
        return base.GetTileData(position, ref rotation);
    }
}
public interface IEntityData
{
    public EntityData GetEntityData();
}
