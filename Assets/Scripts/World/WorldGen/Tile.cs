using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tile
{
    public TileData BaseTileData {  get; private set; }
    public TileData tileData;

    public Vector3Int tileLocation;
    public Vector3Int globalTileLocation;

    public Matrix4x4 tileTransform = Matrix4x4.identity;

    public Tile(TileData tileData, Vector3Int globalLocation)
    {
        BaseTileData = tileData;    

        //Tile needs to know its global location to perform certain checks
        tileLocation = WorldUtils.TileCoordinateGlobalToLocal(globalLocation);
        globalTileLocation = globalLocation;


        this.tileData = BaseTileData.GetTileData(globalTileLocation, ref tileTransform);
    }

    public void SetTile(TileData tileData)
    {
        BaseTileData = tileData;
        this.tileData = BaseTileData.GetTileData(globalTileLocation, ref tileTransform);
    }

    public void RefreshTile()
    {
        SetTile(BaseTileData);
        WorldMeshBuilder.SetChunkDirty(WorldUtils.GetChunkLocation(globalTileLocation));
    }
}
