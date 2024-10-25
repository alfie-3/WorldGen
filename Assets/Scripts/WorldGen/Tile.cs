using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tile
{
    public TileData tileData;

    public Tile(TileData tileData)
    {
        this.tileData = tileData;    
    }
}
