using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tile
{
    public TileData tileData;
    public Vector3Int tileLocation;

    public Tile(TileData tileData, Vector3Int location)
    {
        this.tileData = tileData;    
        tileLocation = location;
    }
}
