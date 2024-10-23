using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Tile
{
    public string TileType;

    public Tile(string tileType)
    {
        TileType = tileType;    
    }
}
