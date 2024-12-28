using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

[System.Serializable]
public class Chunk
{
    public Vector2Int ChunkLocation { get; private set; }

    public Tile[,,] Tiles = new Tile[WorldGeneration.CHUNK_SIZE, WorldGeneration.MaxTerrainHeight + 1, WorldGeneration.CHUNK_SIZE];

    private Tile GetTile(Vector3Int coord)
    {
        if (CheckOutOfChunkRange(coord))
        {
            Debug.Log($"Out of range {coord}");
            return null;
        }

        return Tiles[coord.x, coord.y, coord.z];
    }

    public void SetTile(TileData tileData, Vector3Int coord)
    {
        Vector3Int localCoord = WorldUtils.TileCoordinateGlobalToLocal(coord);

        if (CheckOutOfChunkRange(localCoord))
        {
            Debug.Log($"Out of range {localCoord}");
        }

        Tiles[localCoord.x, coord.y, localCoord.z] = new(tileData, coord);
    }

    public void ClearTile(Vector3Int coord)
    {
        if (CheckOutOfChunkRange(coord))
        {
            Debug.Log($"Out of range {coord}");
        }

        Tiles[coord.x, coord.y, coord.z] = null;
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

        if ((coord.z >= 0) && (coord.x < Tiles.GetLength(2)))
        {
            return false;
        }

        return true;
    }
}
