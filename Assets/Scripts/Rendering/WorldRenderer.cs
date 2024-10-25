using System.Collections.Generic;
using UnityEngine;

public class TileBatch
{
    public List<Matrix4x4> matricies = new();
    public Matrix4x4[] matriciesArray;

    public Matrix4x4[] GetMatricies()
    {
        if (matriciesArray != null) return matriciesArray;
        
        matriciesArray = matricies.ToArray();
        matricies = null;

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
    public WorldGeneration WorldGeneration;

    Dictionary<Vector2Int, Dictionary<string, TileBatch>> chunkBatchTiles = new();

    private void OnEnable()
    {
        WorldGeneration.chunkReady += BatchTerrain;
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
            chunkBatchTiles[chunkKey][data.TileId].matricies.Add(Matrix4x4.Translate(tile.Key));
        }
    }

    private void LateUpdate()
    {
        foreach (KeyValuePair<Vector2Int, Dictionary<string, TileBatch>> chunkBatches in chunkBatchTiles)
        {
            foreach (KeyValuePair<string, TileBatch> chunkBatch in chunkBatches.Value)
            {
                RenderParams rp = new RenderParams(chunkBatch.Value.tileData.TileMaterial);
                Graphics.RenderMeshInstanced(rp, chunkBatch.Value.tileData.TileMesh, 0, chunkBatch.Value.GetMatricies());
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (WorldGeneration == null) return;
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

            Gizmos.DrawWireCube(new Vector3(chunk.Value.ChunkLocation.x * WorldGeneration.CHUNK_SIZE, 0, chunk.Value.ChunkLocation.y * WorldGeneration.CHUNK_SIZE), Vector3.one * WorldGeneration.CHUNK_SIZE);
        }
    }
}
