using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRuleTile
{
    public TileData GetTileData(Vector3Int location);
}
