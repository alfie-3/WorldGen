using Cysharp.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldGeneration : MonoBehaviour
{
    //Tile stuff TEMP
    [SerializeField] TileData grass;

    //Noise Layers
    [SerializeField] SO_FastNoiseLiteGenerator noiseGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator noiseIsland;

    CancellationTokenSource tokenSource = new CancellationTokenSource();

    //Chunk Generation
    public const int CHUNK_SIZE = 16;
    public static int ChunkGenerationRange = 5;
    public static int ChunkReleaseRange = 20;

    //Terrain Params
    public const int MaxTerrainHeight = 4;

    public static ConcurrentDictionary<Vector2Int, Chunk> ChunkDict { get; private set; } = new();

    public static Transform PlayerTransform;

    public static Action<Vector2Int> ChunkReady = delegate { };
    public static Action<Vector2Int> ChunkReleased = delegate { };

    public enum CoordinateMode
    {
        Local,
        Global
    }

    public void Awake()
    {
        PlayerTransform = Camera.main.transform;
    }

    // Start is called before the first frame update
    async UniTaskVoid Start()
    {
        await GenerateChunks();
    }

    public async UniTaskVoid Update()
    {
        await GenerateChunks();

        CheckReleaseChunks();
    }

    [ContextMenu("Regenerate")]
    async UniTaskVoid Regenerate()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        ChunkDict.Clear();

        noiseGenerator.Seed = (int)DateTime.Now.Ticks;
        noiseIsland.Seed = (int)DateTime.Now.Ticks;

        await GenerateChunks();
    }

    async UniTask GenerateChunks()
    {
        Vector2Int playerChunkLoc = WorldUtils.GetChunkLocation(PlayerTransform.position);

        for (int x = playerChunkLoc.x - ChunkGenerationRange; x < playerChunkLoc.x + ChunkGenerationRange; x++)
        {
            for (int y = playerChunkLoc.y - ChunkGenerationRange; y < playerChunkLoc.y + ChunkGenerationRange; y++)
            {
                Vector2Int chunkLoc = new Vector2Int(x, y);

                if (WorldUtils.IsChunkGenerated(chunkLoc)) { continue; }

                if (CreateChunk(chunkLoc, out Chunk newChunk))
                {
                    await UniTask.RunOnThreadPool(() => GenerateChunk(newChunk), cancellationToken: tokenSource.Token);
                    ChunkReady.Invoke(chunkLoc);
                }
            }
        }
    }

    public Chunk GenerateChunk(Chunk chunk)
    {
        //Create tiles in chunk
        CreateTiles(chunk);

        //Chunk is finished
        chunk.ChunkStatus = Chunk.CHUNK_STATUS.GENERATED;

        //Refreshes all tiles to enforce rules
        foreach (Tile tile in chunk.Tiles.Values)
        {
            tile.RefreshTile();

            if (tile.tileLocation.x % CHUNK_SIZE <= 1  || tile.tileLocation.z % CHUNK_SIZE <= 1)
            {
                WorldManagement.UpdateAdjacentTiles(tile.tileLocation);
            }
        }

        return chunk;
    }

    public bool CreateChunk(Vector2Int chunkLocation, out Chunk chunk)
    {
        Chunk newChunk = new(chunkLocation)
        {
            ChunkStatus = Chunk.CHUNK_STATUS.GENERATING
        };

        chunk = newChunk;

        return ChunkDict.TryAdd(chunkLocation, newChunk);
    }

    public void CheckReleaseChunks()
    {
        foreach (Vector2Int loc in ChunkDict.Keys)
        {
            if (Vector2Int.Distance(WorldUtils.GetChunkLocation(PlayerTransform.position), ChunkDict[loc].ChunkLocation) > ChunkReleaseRange)
            {
                ChunkDict.Remove(loc, out Chunk value);
                ChunkReleased.Invoke(loc);
            }
        }
    }

    public void CreateTiles(Chunk chunk)
    {
        //Create tiles in chunk
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                Vector2Int tileLocation = new Vector2Int(x + (chunk.ChunkLocation.x * CHUNK_SIZE), y + (chunk.ChunkLocation.y * CHUNK_SIZE));
                int tileId = GetTileFromNoise(tileLocation.x, tileLocation.y);

                float sample = SampleNoise(tileLocation.x, tileLocation.y);
                int terrainHeight = Mathf.RoundToInt(sample * MaxTerrainHeight);

                for (int i = 0; i < terrainHeight - 1; i++)
                {
                    CreateTile(chunk, tileId, new(tileLocation.x, i, tileLocation.y));
                }
            }
        }
    }

    public void CreateTile(Chunk chunk, int tileId, Vector3Int coordinate)
    {
        if (tileId == 0) return;
        chunk.SetTile(grass, coordinate);
    }

    private int GetTileFromNoise(int x, int y)
    {
        float sample = noiseGenerator.GetNoiseClamped(new(x, y));
        sample = Mathf.Pow(sample, noiseIsland.GetNoiseClamped(new(x, y)));

        sample *= 2;

        return Mathf.FloorToInt(sample);
    }

    private float SampleNoise(int x, int y)
    {
        float sample = noiseGenerator.GetNoiseClamped(new(x, y));
        sample = Mathf.Pow(sample, noiseIsland.GetNoiseClamped(new(x, y)));

        return sample;
    }

    private void OnApplicationQuit()
    {
        ChunkDict.Clear();

        tokenSource.Cancel();

        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }
}
