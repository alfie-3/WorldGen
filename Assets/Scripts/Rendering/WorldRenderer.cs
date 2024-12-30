using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class WorldRenderer : MonoBehaviour
{
    ConcurrentDictionary<Vector2Int, ChunkRenderData> chunkRenderDataDict = new();

    public class ChunkRenderData
    {
        public bool dirty;
        public ConcurrentDictionary<TileData, TileBatchData> tileBatches = new();
    }

    public class TileBatchData
    {
        public List<Matrix4x4> tileMatricies = new();

        public Matrix4x4[] MatriciesArray
        {
            get
            {
                if (cachedMatricies == null)
                {
                    cachedMatricies = tileMatricies.ToArray();
                    return cachedMatricies;
                }
                return cachedMatricies;
            }
        }
        Matrix4x4[] cachedMatricies;

    }

    private void OnEnable()
    {
        WorldGeneration.ChunkReady += StartAsyncChunkBatch;
        WorldGeneration.ChunkReleased += ReleaseBatch;
        Chunk.RefreshChunk += UpdateChunk;
    }

    private void LateUpdate()
    {
        DrawWorld();
    }

    public async void StartAsyncChunkBatch(Vector2Int coord)
    {
        await UniTask.RunOnThreadPool(() =>
        {
            CreateBatches(coord);
        }
        );
    }

    private void CreateBatches(Vector2Int chunkLoc)
    {
        if (WorldUtils.GetChunk(chunkLoc, out Chunk chunk))
        {
            if (!chunkRenderDataDict.TryAdd(chunkLoc, new()))
            {
                chunkRenderDataDict[chunkLoc].dirty = false;
                chunkRenderDataDict[chunkLoc].tileBatches.Clear();
            }

            foreach (Tile tile in chunk.Tiles)
            {
                if (tile == null) continue;
                if (tile.tileData == null) continue;

                chunkRenderDataDict[chunkLoc].tileBatches.TryAdd(tile.tileData, new());
                chunkRenderDataDict[chunkLoc].tileBatches[tile.tileData].tileMatricies.Add(tile.tileTransform);
            }
        }
    }

    private void ReleaseBatch(Vector2Int coord)
    {
        chunkRenderDataDict.Remove(coord, out _);
    }

    private void UpdateChunk(Vector2Int location)
    {
        if (chunkRenderDataDict.TryGetValue(location, out ChunkRenderData data))
        {
            data.dirty = true;
        }
    }

    public void DrawWorld()
    {
        foreach(KeyValuePair<Vector2Int, ChunkRenderData> chunkRenderData in chunkRenderDataDict)
        {
            if (Vector2Int.Distance(chunkRenderData.Key, WorldUtils.GetChunkLocation(WorldGeneration.PlayerTransform.position)) > WorldGeneration.ChunkGenerationRange + 1) { continue; }
            if (chunkRenderData.Value.dirty) { StartAsyncChunkBatch(chunkRenderData.Key); }

            foreach (KeyValuePair<TileData, TileBatchData> batch in chunkRenderData.Value.tileBatches)
            {
                for (int i = 0; i < batch.Key.TileMaterials.Length; i++)
                {
                    RenderParams rp = new RenderParams(batch.Key.TileMaterials[i]);
                    Graphics.RenderMeshInstanced(rp, batch.Key.TileMesh, i, batch.Value.MatriciesArray);
                }
            }
        }
    }
}
