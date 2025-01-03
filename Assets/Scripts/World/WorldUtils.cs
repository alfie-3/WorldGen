using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WorldUtils : MonoBehaviour
{
    public static bool TryGetChunk(Vector2Int chunkCoordinate, out Chunk returnChunk)
    {
        if (WorldGeneration.ChunkDict.TryGetValue(chunkCoordinate, out Chunk chunk))
        {
            returnChunk = chunk;
            return true;
        }

        returnChunk = null;
        return false;
    }

    public static bool IsChunkGenerated(Vector2Int chunkCoordinate)
    {
        if (WorldGeneration.ChunkDict.TryGetValue(chunkCoordinate, out Chunk chunk))
        {
            return chunk.ChunkStatus == Chunk.CHUNK_STATUS.GENERATED;
        }
        else
            return false;
    }

    public static bool TryGetTile(Vector3Int tileCoordinate, out Tile returnTile)
    {
        returnTile = null;
        if (!IsTileCoordinateValid(tileCoordinate)) return false;

        Vector2Int chunkLoc = new Vector2Int(RoundInt(tileCoordinate.x, WorldGeneration.CHUNK_SIZE), RoundInt(tileCoordinate.z, WorldGeneration.CHUNK_SIZE));

        if (TryGetChunk(chunkLoc, out Chunk returnChunk))
        {
            Vector3Int tileLoc = TileCoordinateGlobalToLocal(tileCoordinate);
            if (tileLoc.y < 0) return false;

            if (returnChunk.GetTile(tileLoc, out Tile tile))
            {
                returnTile = tile;
                return true;
            }

            else return false;
        }

        return false;
    }

    public static int CountValidTiles(Vector2Int chunkCoordinate)
    {
        int validTiles = 0;

        if (TryGetChunk(chunkCoordinate, out Chunk chunk))
        {
            foreach (Tile tile in chunk.Tiles)
            {
                if (tile == null) continue;
                if (tile.DontDraw) continue;

                validTiles++;
            }
        }

        return validTiles;
    }

    public static Vector3Int TileCoordinateGlobalToLocal(Vector3Int global)
    {
        return new(Math.Abs(global.x % WorldGeneration.CHUNK_SIZE), global.y, Math.Abs(global.z % WorldGeneration.CHUNK_SIZE));
    }

    public static Vector3Int TileCoordinateLocalToGlobal(Vector3Int local, Vector2Int chunkCoordinate)
    {
        return new Vector3Int(local.x + (chunkCoordinate.x * WorldGeneration.CHUNK_SIZE), local.y, local.z + (chunkCoordinate.y * WorldGeneration.CHUNK_SIZE));
    }

    public static bool IsTileCoordinateValid(Vector3Int coord)
    {
        if (coord.y > WorldGeneration.MaxTerrainGeneration) return false;

        return true;
    }

    /// <summary>
    /// Gets top tile using global tile coordinate
    /// </summary>
    /// <param name="sampleTileLoc"></param>
    /// <returns></returns>
    public static Vector3Int GetTopTileLocation(Vector3Int sampleTileLoc)
    {
        for (int i = WorldGeneration.MaxTerrainGeneration + 1; i > 0; i--)
        {
            if (TryGetTile(new(sampleTileLoc.x, i, sampleTileLoc.z), out Tile tile))
            {
                return tile.globalTileLocation;
            }
        }

        return sampleTileLoc;
    }

    public static Vector2Int GetChunkLocation(Vector3 location)
    {
        return Vector2Int.FloorToInt(new Vector2(location.x, location.z) / WorldGeneration.CHUNK_SIZE);
    }

    public static Vector3Int RoundVector3(Vector3 location)
    {
        return new((int)location.x, (int)location.y, (int)location.z);
    }

    public static int RoundInt(int a, int b)
    {
        int res = a / b;
        return (a < 0 && a != b * res) ? res - 1 : res;
    }
}
