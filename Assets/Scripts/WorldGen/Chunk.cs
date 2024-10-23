using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Chunk
{
    public Vector2Int ChunkLocation {  get; private set; }
    public Dictionary<Vector3Int, Tile> Tiles;

    public enum GenerationStatus
    {
        UNGENERATED,
        GENERATING,
        GENERATED
    }

    public GenerationStatus generationStatus;

    public Chunk(Vector2Int chunkLocation)
    {
        Tiles = new();
        generationStatus = GenerationStatus.UNGENERATED;
        ChunkLocation = chunkLocation;
    }

    public void SetGenerationStatus(GenerationStatus generationStatus)
    {
        this.generationStatus = generationStatus;
    }

    public void AddTile(Tile tile, Vector3Int coordinate)
    {
        Tiles.Add(coordinate, tile);
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
