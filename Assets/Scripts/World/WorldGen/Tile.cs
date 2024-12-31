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
        TileInfo prevTileInfo = new(this);

        SetTile(BaseTileData);

        Chunk.OnTileSet.Invoke(WorldUtils.GetChunkLocation(globalTileLocation), prevTileInfo, new TileInfo(this));
        Chunk.RefreshChunk.Invoke(WorldUtils.GetChunkLocation(globalTileLocation));
    }
}

public class TileInfo {
    public Vector3Int tileLocation;
    public Matrix4x4 tileTransform;
    public TileData tiledata;

    public TileInfo(Vector3Int tileLoc, Matrix4x4 tileTransform, TileData tileData) {
        tileLocation = tileLoc;
        this.tileTransform = tileTransform;
        this.tiledata = tileData;
    }

    public TileInfo(Tile tile) {
        tileLocation = tile.tileLocation;
        tileTransform = tile.tileTransform;
        tiledata = tile.tileData;
    }
}
