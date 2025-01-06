using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

[System.Serializable]
public class Chunk
{
    public Vector2Int ChunkLocation { get; private set; }

    public Tile[,,] Tiles = new Tile[WorldGeneration.CHUNK_SIZE, WorldGeneration.MaxTerrainHeight, WorldGeneration.CHUNK_SIZE];

    public static Action<Vector2Int> RefreshChunk = delegate { };
    public static Action<Vector2Int, TileInfo, TileInfo> OnTileUpdate = delegate { };

    public Action<CHUNK_STATUS> OnUpdatedChunkStatus = delegate { };
    public Action OnChunkRemoved = delegate { };

    public enum CHUNK_STATUS
    {
        UNGENERATED,
        GENERATING,
        GENERATED,
        SLEEPING
    }

    public CHUNK_STATUS ChunkStatus { get; private set; } = CHUNK_STATUS.UNGENERATED;

    public Chunk(Vector2Int chunkLocation)
    {
        ChunkStatus = CHUNK_STATUS.UNGENERATED;
        ChunkLocation = chunkLocation;
    }

    public void SetStatus(CHUNK_STATUS newStatus)
    {
        ChunkStatus = newStatus;
        OnUpdatedChunkStatus(newStatus);
    }

    #region ChunkTile Management
    private Tile GetTile(Vector3Int coord)
    {
        if (CheckOutOfChunkRange(coord))
        {
            Debug.Log($"Out of range {coord}");
            return null;
        }

        return Tiles[coord.x, coord.y, coord.z];
    }

    public void SetTile(TileData tileData, Vector3Int coord, COORD_TYPE coordType = COORD_TYPE.GLOBAL)
    {
        Vector3Int globalCoord = coordType == COORD_TYPE.GLOBAL ? coord : WorldUtils.TileCoordinateLocalToGlobal(coord, ChunkLocation);
        Vector3Int localCoord = coordType == COORD_TYPE.LOCAL ? coord : WorldUtils.TileCoordinateGlobalToLocal(coord);

        if (CheckOutOfChunkRange(localCoord))
        {
            Debug.Log($"Out of range {localCoord}");
            return;
        }

        TileInfo prevTileInfo = new(Tiles[localCoord.x, localCoord.y, localCoord.z]);

        Tiles[localCoord.x, localCoord.y, localCoord.z] = new(tileData, globalCoord);

        OnTileUpdate.Invoke(ChunkLocation, prevTileInfo, new TileInfo (Tiles[localCoord.x, localCoord.y, localCoord.z]));
    }

    public void ClearTile(Vector3Int coord)
    {
        coord = WorldUtils.TileCoordinateGlobalToLocal(coord);

        if (CheckOutOfChunkRange(coord))
        {
            Debug.Log($"Out of range {coord}");
            return;
        }

        TileInfo prevTileInfo = new(Tiles[coord.x, coord.y, coord.z]);

        Tiles[coord.x, coord.y, coord.z] = null;

        TileInfo newTileInfo = new TileInfo(Tiles[coord.x, coord.y, coord.z]);

        OnTileUpdate.Invoke(ChunkLocation, prevTileInfo, newTileInfo);
    }

    public bool GetTile(Vector3Int coordinate, out Tile returnTile)
    {
        returnTile = GetTile(coordinate);

        return returnTile != null;
    }

    private bool CheckOutOfChunkRange(Vector3Int coord)
    {
        if ((coord.x >= 0) && (coord.x < Tiles.GetLength(0)))
        {
            return false;
        }

        if ((coord.z >= 0) && (coord.z < Tiles.GetLength(2)))
        {
            return false;
        }

        if ((coord.y >= 0) && (coord.y < WorldGeneration.MaxTerrainHeight - 1))
        {
            return false;
        }

        return true;
    }
    #endregion
}
