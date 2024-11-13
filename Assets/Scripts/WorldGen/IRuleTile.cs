using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRuleTile
{
    public TileData GetTileData(Chunk chunk, Vector3Int location);
}
