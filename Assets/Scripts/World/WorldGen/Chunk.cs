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

    private Tile GetTile(Vector3Int coord)
    {
        if (CheckOutOfChunkRange(coord))
        {
            Debug.Log($"Out of range {coord}");
            return null;
        }

        return Tiles[coord.x, coord.y, coord.z];
    }

    /// <summary>
    /// Sets a tile within a specificed GLOBAL coordiate to the provided tile data.
    /// </summary>
    public void SetTile(TileData tileData, Vector3Int coord)
    {
        Vector3Int localCoord = WorldUtils.TileCoordinateGlobalToLocal(coord);

        if (CheckOutOfChunkRange(localCoord))
        {
            Debug.Log($"Out of range {localCoord}");
            return;
        }

        TileInfo prevTileInfo = new(Tiles[localCoord.x, coord.y, localCoord.z]);

        Tiles[localCoord.x, coord.y, localCoord.z] = new(tileData, coord);

        OnTileUpdate.Invoke(ChunkLocation, prevTileInfo, new TileInfo (Tiles[localCoord.x, coord.y, localCoord.z]));
    }

    /// <summary>
    /// Clears a tile within a specificed GLOBAL coordiate.
    /// </summary>
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

        OnTileUpdate.Invoke(ChunkLocation, prevTileInfo, new TileInfo(Tiles[coord.x, coord.y, coord.z]));
    }

    public enum CHUNK_STATUS
    {
        UNGENERATED,
        GENERATING,
        GENERATED,
        SLEEPING
    }

    public CHUNK_STATUS ChunkStatus = CHUNK_STATUS.UNGENERATED;

    public Chunk(Vector2Int chunkLocation)
    {
        ChunkStatus = CHUNK_STATUS.UNGENERATED;
        ChunkLocation = chunkLocation;
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
}
