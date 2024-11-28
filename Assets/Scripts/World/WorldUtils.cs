using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WorldUtils : MonoBehaviour
{
    public static bool GetChunk(Vector2Int chunkCoordinate, out Chunk returnChunk)
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

    public static bool GetTile(Vector3Int tileCoordinate, out Tile returnTile)
    {
        returnTile = null;
        Vector2Int chunkLoc = new Vector2Int(RoundFloatToInt(tileCoordinate.x, WorldGeneration.CHUNK_SIZE), RoundFloatToInt(tileCoordinate.z, WorldGeneration.CHUNK_SIZE));

        if (GetChunk(chunkLoc, out Chunk returnChunk))
        {
            if (returnChunk.GetTile(tileCoordinate, out Tile tile))
            {
                returnTile = tile;
                return true;
            }

            else return false;
        }

        return false;
    }

    public static Vector3Int GetTopTileLocation(Vector3Int sampleTileLoc)
    {
        for (int i = WorldGeneration.MaxTerrainHeight; i > 0; i--)
        {
            if (GetTile(new(sampleTileLoc.x, i, sampleTileLoc.z), out Tile tile))
            {
                return tile.tileLocation;
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

    public static int RoundFloatToInt(int a, int b)
    {
        int res = a / b;
        return (a < 0 && a != b * res) ? res - 1 : res;
    }
}
