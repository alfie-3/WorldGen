using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tile
{
    public bool DontDraw => tileData == null;

    public TileData BaseTileData {  get; private set; }
    public TileData tileData;

    public byte[] tileLocation = new byte[3];
    public byte rotation = 0;

    public Vector3Int TileLocationVect3 => Vector3Byte.ByteToVector3(tileLocation);

    public Tile(TileData tileData, Vector3Int globalLocation)
    {
        BaseTileData = tileData;
        Vector3Byte.Vector3ToByte(WorldUtils.TileCoordinateGlobalToLocal(globalLocation), ref tileLocation);

        this.tileData = BaseTileData.GetTileData(globalLocation, ref rotation);

    }

    public void SetTile(TileData tileData, Vector3Int globalLocation)
    {
        BaseTileData = tileData;
        this.tileData = BaseTileData.GetTileData(globalLocation, ref rotation);
    }

    public void RefreshTile(Vector3Int globalLocation)
    {
        SetTile(BaseTileData, globalLocation);
    }

    public static Quaternion GetRotation(byte rotationByte)
    {
        float angle = (float)rotationByte * 90;
        return Quaternion.Euler(new(0, angle, 0));
    }
}

public enum COORD_TYPE
{
    LOCAL,
    GLOBAL
}

public struct TileInfo {
    public Vector3Int TileLocation;
    public byte rotation;

    public TileData TileData;

    public static readonly TileInfo Empty = new TileInfo(null);

    public TileInfo(Vector3Int tileLoc, byte rotation, TileData tileData) {
        TileLocation = tileLoc;
        this.rotation = rotation;
        this.TileData = tileData;
    }

    public TileInfo(Tile tile) {
        if (tile == null) {
            TileLocation = Vector3Int.zero;
            rotation = 0;
            TileData = null;
            return;
        }

        this.TileData = tile.tileData;

        TileLocation = Vector3Byte.ByteToVector3(tile.tileLocation);
        rotation = tile.rotation;
    }
}
