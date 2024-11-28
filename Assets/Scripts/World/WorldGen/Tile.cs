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

    public Matrix4x4 tileTransform = Matrix4x4.identity;

    public Tile(TileData tileData, Vector3Int location)
    {
        BaseTileData = tileData;    
        tileLocation = location;
    }

    public void SetTile(TileData tileData)
    {
        BaseTileData = tileData;
        this.tileData = BaseTileData.GetTileData(tileLocation, ref tileTransform);
    }

    public void RefreshTile()
    {
        WorldManagement.SetTile(tileLocation, BaseTileData);
        WorldMeshBuilder.SetChunkDirty(WorldUtils.GetChunkLocation(tileLocation));
    }
}
