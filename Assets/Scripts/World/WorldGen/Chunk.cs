using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Chunk
{
    public Vector2Int ChunkLocation { get; private set; }
    public Vector2Int ChunkGlobalLocation => ChunkLocation * WorldGeneration.CHUNK_SIZE;
    public ConcurrentDictionary<Vector3Int, Tile> Tiles = new();
    public Mesh ChunkMesh;

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

    public void SetTile(TileData tileData, Vector3Int coordinate)
    {
        if (!Tiles.TryAdd(coordinate, new(tileData, coordinate)))
        {
            Tiles[coordinate].SetTile(tileData);
        }
        else
        {
            Tiles[coordinate].RefreshTile();
        }

        UpdateAdjacentTiles(coordinate);
    }

    public bool GetTile(Vector3Int coordinate, out Tile returnTile)
    {
        if (Tiles.TryGetValue(coordinate, out Tile tile))
        {
            returnTile = tile;

            return true;
        }

        returnTile = null;
        return false;
    }

    public void UpdateAdjacentTiles(Vector3Int coordinate)
    {
        for (int i = 0; i < RuleTileData.NeighbourPositions.Length; i++)
        {
            if (WorldUtils.GetTile(coordinate + RuleTileData.NeighbourPositions[i], out Tile tile))
            {
                tile.RefreshTile();
            }
        }
    }
}
