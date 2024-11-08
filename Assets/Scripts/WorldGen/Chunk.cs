using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Chunk
{
    public Vector2Int ChunkLocation {  get; private set; }
    public Vector2Int ChunkGlobalLocation => ChunkLocation * WorldGeneration.CHUNK_SIZE;
    public Dictionary<Vector3Int, Tile> Tiles;
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
        Tiles = new();
        ChunkStatus = CHUNK_STATUS.UNGENERATED;
        ChunkLocation = chunkLocation;
    }

    public void SetTile(TileData tileData, Vector3Int coordinate)
    {
        if (!Tiles.TryAdd(coordinate, new(tileData, coordinate)))
        {
            Tiles[coordinate] = new(tileData, coordinate);
        }
    }

    public Tile GetTile(Vector3Int coordinate)
    {
        if (Tiles.TryGetValue(coordinate, out Tile tile))
        {
            return tile;
        }
        else
        {
            Debug.Log($"Invalid Tile in chunk {ChunkLocation} at {coordinate}");
            return default;
        }
    }
}
