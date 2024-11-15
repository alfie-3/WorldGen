using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tile
{
    public TileData BaseTileData {  get; private set; }
    public TileData tileData;

    //Tile location is GLOBAL not local to the chunk
    public Vector3Int tileLocation;

    public Tile(TileData tileData, Vector3Int location)
    {
        BaseTileData = tileData;    
        tileLocation = location;
    }

    public void RefreshTile()
    {
        tileData = BaseTileData.GetTileData(tileLocation);
    }
}
