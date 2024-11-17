using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TileBatch
{
    public List<Matrix4x4> matricies = new();

    public Matrix4x4[] matriciesArray;

    public bool dirty = true;

    public Matrix4x4[] GetMatricies()
    {
        if (!dirty) return matriciesArray;

        lock (matricies)
        matriciesArray = matricies.AsReadOnly().ToArray();

        dirty = false;

        return matriciesArray;
    }

    public TileData tileData;

    public TileBatch(TileData tileData)
    {
        this.tileData = tileData;
    }
}

public class WorldRenderer : MonoBehaviour
{
   static ConcurrentDictionary<Vector2Int, ConcurrentDictionary<string, TileBatch>> chunkBatchTiles = new();

    private void OnEnable()
    {
        WorldGeneration.ChunkReady += BatchTerrain;
    }

    private void BatchTerrain(Vector2Int chunkKey)
    {
        if (!chunkBatchTiles.TryAdd(chunkKey, new()))
        {
            chunkBatchTiles[chunkKey].Clear();
        }

        foreach (KeyValuePair<Vector3Int, Tile> tile in WorldGeneration.ChunkDict[chunkKey].Tiles)
        {
            TileData data = tile.Value.tileData;

            chunkBatchTiles[chunkKey].TryAdd(data.TileId, new(data));
            chunkBatchTiles[chunkKey][data.TileId].matricies.Add(Matrix4x4.Translate(tile.Value.tileLocation));
        }
    }

    public static void UpdateTile(Vector3Int tileLocation, TileData newTildData, TileData oldTileData)
    {
        Vector2Int chunkLoc = WorldUtils.GetChunkLocation(tileLocation);

        if (oldTileData != null) if (newTildData.TileId == oldTileData.TileId) return;

        if (chunkBatchTiles.TryGetValue(chunkLoc, out var tileBatches))
        {
            tileBatches.TryAdd(newTildData.TileId, new(newTildData));
            tileBatches[newTildData.TileId].matricies.Add(Matrix4x4.Translate(tileLocation));
            tileBatches[newTildData.TileId].dirty = true;

            if (oldTileData == null) { return; }

            if (tileBatches.TryGetValue(oldTileData.TileId, out TileBatch value))
            {
                lock (value.matricies) value.matricies.Remove(Matrix4x4.Translate(tileLocation));
                if (value.matricies.Count == 0) tileBatches.TryRemove(oldTileData.TileId, out TileBatch poo);
                value.dirty = true;
            }
        }
    }

    private void LateUpdate()
    {
        foreach (KeyValuePair<Vector2Int, ConcurrentDictionary<string, TileBatch>> chunkBatches in chunkBatchTiles)
        {
            if (Vector2Int.Distance(chunkBatches.Key, WorldUtils.GetChunkLocation(WorldGeneration.PlayerTransform.position)) > WorldGeneration.ChunkGenerationRange + 1) { continue; }

            foreach (KeyValuePair<string, TileBatch> chunkBatch in chunkBatches.Value)
            {
                RenderParams rp = new RenderParams(chunkBatch.Value.tileData.TileMaterial);
                Graphics.RenderMeshInstanced(rp, chunkBatch.Value.tileData.TileMesh, 0, chunkBatch.Value.GetMatricies());
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (WorldGeneration.ChunkDict == null) return;

        foreach (KeyValuePair<Vector2Int, Chunk> chunk in WorldGeneration.ChunkDict)
        {
            Gizmos.matrix = Matrix4x4.Translate(new(WorldGeneration.CHUNK_SIZE / 2, 0, WorldGeneration.CHUNK_SIZE / 2));

            switch (chunk.Value.ChunkStatus)
            {
                case Chunk.CHUNK_STATUS.GENERATED:
                    Gizmos.color = Color.green;
                    break;
                case Chunk.CHUNK_STATUS.GENERATING:
                    Gizmos.color = Color.yellow;
                    break;
                case Chunk.CHUNK_STATUS.UNGENERATED:
                    Gizmos.color = Color.red;
                    break;
                case Chunk.CHUNK_STATUS.SLEEPING:
                    Gizmos.color = Color.blue;
                    break;
            } //Change chunk boundary colour

            Gizmos.DrawWireCube(new Vector3(chunk.Value.ChunkLocation.x * WorldGeneration.CHUNK_SIZE - 0.5f, 0, chunk.Value.ChunkLocation.y * WorldGeneration.CHUNK_SIZE - 0.5f), Vector3.one * WorldGeneration.CHUNK_SIZE);
            Handles.Label(new Vector3(chunk.Value.ChunkLocation.x * WorldGeneration.CHUNK_SIZE, 0, chunk.Value.ChunkLocation.y * WorldGeneration.CHUNK_SIZE), chunk.Value.ChunkLocation.ToString());
        }
    }
}
