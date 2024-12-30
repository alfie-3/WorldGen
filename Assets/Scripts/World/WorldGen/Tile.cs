using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tile
{
    public bool DontDraw => tileData == null;

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

        tileTransform = Matrix4x4.Translate(globalTileLocation);

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
        Chunk.RefreshChunk.Invoke(WorldUtils.GetChunkLocation(globalTileLocation));
    }
}
