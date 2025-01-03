using Cysharp.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class WorldGeneration : MonoBehaviour
{
    [Header("Terrain Components")]
    //Tile stuff TEMP
    [SerializeField] TileData terrainRuleTile;
    [SerializeField] List<TileData> terrainFeatures;

    [Header("Noise Generators")]
    //Noise Layers
    [SerializeField] SO_FastNoiseLiteGenerator primaryGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator featuresGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator noiseIsland;

    [Header("Controls")]
    [SerializeField] float featureFrequency;

    //References
    public static Transform PlayerTransform;

    //Chunk Generation
    public const int CHUNK_SIZE = 16;
    public static int ChunkGenerationRange = 8;
    public static int ChunkReleaseRange = 20;
    public static ConcurrentDictionary<Vector2Int, Chunk> ChunkDict { get; private set; } = new();

    //Terrain Params
    public const int MaxTerrainGeneration = 4;
    public const int MaxTerrainHeight = 6;

    //Chunk generation events
    public static Action<Vector2Int> ChunkReady = delegate { };
    public static Action<Vector2Int> ChunkReleased = delegate { };

    //Thread cancellation token for stopping threads when exited
    readonly CancellationTokenSource tokenSource = new();

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

    public async UniTaskVoid FixedUpdate()
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

        primaryGenerator.Seed = (int)DateTime.Now.Ticks;
        noiseIsland.Seed = (int)DateTime.Now.Ticks;

        await GenerateChunks();
    }

    //Generated chunks around the player location
    //When a chunk is being generated it is put on the thread pool to devide the load between multiple CPU cores
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
        GenerateTiles(chunk);

        //Chunk is finished
        chunk.ChunkStatus = Chunk.CHUNK_STATUS.GENERATED;

        //Refreshes all tiles to enforce rules
        foreach (Tile tile in chunk.Tiles)
        {
            if (tile == null) continue;

            tile.RefreshTile();

            if (tile.globalTileLocation.x % CHUNK_SIZE <= 1 || tile.globalTileLocation.z % CHUNK_SIZE <= 1)
            {
                WorldManagement.UpdateAdjacentTiles(tile.globalTileLocation);
            }
        }

        GenerateTerrainFeatures(chunk);

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

    public void GenerateTiles(Chunk chunk)
    {
        //Create tiles in chunk
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                Vector2Int tileLocation = new Vector2Int(x + (chunk.ChunkLocation.x * CHUNK_SIZE), y + (chunk.ChunkLocation.y * CHUNK_SIZE));

                float sample = SampleTerrainNoiseHeight(tileLocation);
                int terrainHeight = Mathf.RoundToInt(sample * MaxTerrainGeneration);

                for (int i = 0; i < terrainHeight - 1; i++)
                {
                    if (terrainHeight > 0)
                        chunk.SetTile(terrainRuleTile, new(tileLocation.x, i, tileLocation.y));
                }
            }
        }
    }

    public void GenerateTerrainFeatures(Chunk chunk)
    {
        for (int x = 0; x < chunk.Tiles.GetLength(0); x++)
        {
            for (int z = 0; z < chunk.Tiles.GetLength(2); z++)
            {
                if (chunk.Tiles[x, 0, z] == null) continue;

                Vector2Int sampleLoc = new Vector2Int(x + (chunk.ChunkLocation.x * CHUNK_SIZE), z + (chunk.ChunkLocation.y * CHUNK_SIZE));
                if (featuresGenerator.GetNoiseClamped(sampleLoc) < featureFrequency) continue;

                Vector3Int topTileLoc = WorldUtils.GetTopTileLocation(new(sampleLoc.x, 0, sampleLoc.y));

                if (!WorldUtils.TryGetTile(topTileLoc, out _)) continue;

                topTileLoc.y++;

                System.Random random = new System.Random(Thread.CurrentThread.ManagedThreadId);
                chunk.SetTile(terrainFeatures[random.Next(0, terrainFeatures.Count - 1)], topTileLoc);
            }
        }
    }

    private float SampleTerrainNoiseHeight(Vector2 location)
    {
        float sample = primaryGenerator.GetNoiseClamped(location);
        sample = Mathf.Pow(sample, noiseIsland.GetNoiseClamped(location));

        return sample;
    }

    private float SampleTerrainFeatureNoise(Vector2 location)
    {
        float sample = primaryGenerator.GetNoise(location);
        return sample;
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

    private void OnApplicationQuit()
    {
        ChunkDict.Clear();

        tokenSource.Cancel();

        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }
}
