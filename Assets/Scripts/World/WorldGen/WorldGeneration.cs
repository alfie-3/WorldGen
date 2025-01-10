using Cysharp.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

public class WorldGeneration : MonoBehaviour
{
    [Header("Terrain Components")]
    //Tile stuff TEMP
    [SerializeField] TileData terrainRuleTile;
    [SerializeField] List<TileData> terrainFeatures;

    [Header("Noise Generators")]
    //Noise Layers
    [SerializeField] SO_FastNoiseLiteGenerator primaryGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator noiseIsland;

    [Space]
    [SerializeField] FeatureGeneration featureGeneration;

    //References
    public static Transform PlayerTransform;

    //Chunk Generation
    public const int CHUNK_SIZE = 16;
    public static int ChunkGenerationRange = 8;
    public static int ChunkSleepRange = ChunkGenerationRange + 1;
    public static int ChunkReleaseRange = 20;
    public static ConcurrentDictionary<Vector2Int, Chunk> ChunkDict { get; private set; } = new();

    //Terrain Params
    public const int MaxTerrainGeneration = 5;
    public const int MaxTerrainHeight = 8;

    //Chunk generation events
    public static Action<Vector2Int> OnChunkReady = delegate { };
    public static Action<Vector2Int> OnChunkReleased = delegate { };

    //Thread cancellation token for stopping threads when exited
    readonly CancellationTokenSource tokenSource = new();

    public Queue<Chunk> chunkQueue = new Queue<Chunk>();
    [HideInInspector] public List<Chunk> chunkList;

    public enum CoordinateMode
    {
        Local,
        Global
    }

    public void Awake()
    {
        PlayerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Start is called before the first frame update
    public void Start()
    {
        QueueChunks();
    }

    public void FixedUpdate()
    {
        QueueChunks();
        UpdateChunkStatus();

        GenerateChunks();
    }

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        ChunkDict.Clear();

        primaryGenerator.Seed = (int)DateTime.Now.Ticks;
        noiseIsland.Seed = (int)DateTime.Now.Ticks;

        QueueChunks();
    }

    //Generated chunks around the player location
    public void QueueChunks()
    {
        Vector2Int playerChunkLoc = WorldUtils.GetChunkLocation(Vector3Int.RoundToInt(PlayerTransform.position));

        for (int x = playerChunkLoc.x - ChunkGenerationRange; x < playerChunkLoc.x + ChunkGenerationRange; x++)
        {
            for (int y = playerChunkLoc.y - ChunkGenerationRange; y < playerChunkLoc.y + ChunkGenerationRange; y++)
            {
                Vector2Int chunkLoc = new Vector2Int(x, y);

                if (WorldUtils.IsChunkGenerated(chunkLoc)) { continue; }

                if (CreateChunk(chunkLoc, out Chunk newChunk))
                {
                    chunkList.Add(newChunk);
                }
            }
        }

        chunkList = chunkList.OrderBy((d) => (d.ChunkLocation - playerChunkLoc).sqrMagnitude).ToList();

        foreach (var chunk in chunkList)
        {
            chunkQueue.Enqueue(chunk);
        }

        chunkList.Clear();
    }

    //Take chunks to generate from the queue and add them to the thread pool to be generated
    public async void GenerateChunks()
    {
        while (chunkQueue.Count > 0)
        {
            Chunk chunk = chunkQueue.Dequeue();
            await UniTask.RunOnThreadPool(() => GenerateChunk(chunk, (chunk) => { }), true, cancellationToken: tokenSource.Token);
            ChunkReady(chunk);
        }
    }

    public void ChunkReady(Chunk chunk)
    {
        OnChunkReady.Invoke(chunk.ChunkLocation);
        chunk.SetStatus(Chunk.CHUNK_STATUS.GENERATED);
    }

    public Chunk GenerateChunk(Chunk chunk, Action<Chunk> chunkReady)
    {
        //Create tiles in chunk
        GenerateTiles(chunk);

        //Refreshes all tiles to enforce rules
        foreach (Tile tile in chunk.Tiles)
        {
            if (tile == null) continue;
            Vector3Int globalTileLoc = WorldUtils.TileCoordinateLocalToGlobal(tile.TileLocationVect3, chunk.ChunkLocation);

            tile.RefreshTile(globalTileLoc);

            if (globalTileLoc.x % CHUNK_SIZE <= 1 || globalTileLoc.z % CHUNK_SIZE <= 1)
            {
                WorldManagement.UpdateAdjacentTiles(globalTileLoc);
            }
        }

        featureGeneration.GenerateFeatures(chunk);

        chunkReady(chunk);
        return chunk;
    }

    public bool CreateChunk(Vector2Int chunkLocation, out Chunk chunk)
    {
        Chunk newChunk = new(chunkLocation);
        newChunk.SetStatus(Chunk.CHUNK_STATUS.GENERATING);

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

    private float SampleTerrainNoiseHeight(Vector2 location)
    {
        float sample = primaryGenerator.GetNoiseClamped(location);
        sample = Mathf.Pow(sample, noiseIsland.GetNoiseClamped(location));

        return sample;
    }

    public void UpdateChunkStatus()
    {
        foreach (Vector2Int loc in ChunkDict.Keys)
        {
            float distance = Vector2Int.Distance(WorldUtils.GetChunkLocation(Vector3Int.RoundToInt(PlayerTransform.position)), ChunkDict[loc].ChunkLocation);

            if (distance > ChunkSleepRange)
            {
                if (ChunkDict[loc].ChunkStatus is not Chunk.CHUNK_STATUS.SLEEPING or Chunk.CHUNK_STATUS.GENERATING or Chunk.CHUNK_STATUS.UNGENERATED)
                    ChunkDict[loc].SetStatus(Chunk.CHUNK_STATUS.SLEEPING);
            }
            else if (distance < ChunkSleepRange)
            {
                if (ChunkDict[loc].ChunkStatus is Chunk.CHUNK_STATUS.SLEEPING)
                    ChunkDict[loc].SetStatus(Chunk.CHUNK_STATUS.GENERATED);
            }

            if (distance > ChunkReleaseRange)
            {
                ChunkDict[loc].OnChunkRemoved.Invoke();
                ChunkDict.Remove(loc, out Chunk value);
                OnChunkReleased.Invoke(loc);
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

[Serializable]
public class FeatureGeneration
{
    [Header("Grass")]
    [SerializeField] List<EntityTileData> grass;
    [SerializeField] SO_FastNoiseLiteGenerator grassPatchGenerator;

    [Header("Trees")]
    [SerializeField] List<EntityTileData> trees;
    [SerializeField] SO_FastNoiseLiteGenerator treeRegionGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator treeSparsenessGenerator;
    [SerializeField] float treeRegionThreshold;
    [Space]
    [SerializeField] float treeFrequency;
    [SerializeField] float treeRandomFrequency;

    [Header("Rocks")]
    [SerializeField] List<EntityTileData> rocks;
    [SerializeField] SO_FastNoiseLiteGenerator rockScatterer;
    [SerializeField] float rockFrequency;

    public void GenerateFeatures(Chunk chunk)
    {
        System.Random random = new System.Random(Thread.CurrentThread.ManagedThreadId);

        GenerateRocks(chunk, random);
        GenerateTrees(chunk, random);
    }

    public void GenerateGrass(Chunk chunk)
    {

    }

    public void GenerateTrees(Chunk chunk, System.Random random)
    {
        for (int x = 0; x < chunk.Tiles.GetLength(0); x++)
        {
            for (int z = 0; z < chunk.Tiles.GetLength(2); z++)
            {
                if (chunk.Tiles[x, 0, z] == null) continue;

                Vector2Int sampleLoc = new Vector2Int(x + (chunk.ChunkLocation.x * WorldGeneration.CHUNK_SIZE), z + (chunk.ChunkLocation.y * WorldGeneration.CHUNK_SIZE));

                float sample = treeRegionGenerator.GetNoiseClamped(sampleLoc);

                if (sample > treeRegionThreshold)
                {
                    sample *= Mathf.Clamp01(treeSparsenessGenerator.GetNoiseClamped(sampleLoc));
                    if (sample < 1 - treeFrequency) continue;
                }
                else
                {
                    if (treeSparsenessGenerator.GetNoiseClamped(sampleLoc) < 1 - treeRandomFrequency) continue;
                }

                Vector3Int topTileLoc = WorldUtils.GetTopTileLocation(new(sampleLoc.x, 0, sampleLoc.y));

                if (!WorldUtils.TryGetTile(topTileLoc, out Tile tile)) continue;

                BlockData blockData = GetBlockData(tile);
                if (blockData == null) continue;

                topTileLoc.y++;

                chunk.SetTile(trees[random.Next(0, trees.Count)], topTileLoc);
            }
        }
    }

    public void GenerateRocks(Chunk chunk, System.Random random)
    {
        for (int x = 0; x < chunk.Tiles.GetLength(0); x++)
        {
            for (int z = 0; z < chunk.Tiles.GetLength(2); z++)
            {
                if (chunk.Tiles[x, 0, z] == null) continue;

                Vector2Int sampleLoc = new Vector2Int(x + (chunk.ChunkLocation.x * WorldGeneration.CHUNK_SIZE), z + (chunk.ChunkLocation.y * WorldGeneration.CHUNK_SIZE));
                if (rockScatterer.GetNoiseClamped(sampleLoc) < 1 - rockFrequency) continue;

                Vector3Int topTileLoc = WorldUtils.GetTopTileLocation(new(sampleLoc.x, 0, sampleLoc.y));

                if (!WorldUtils.TryGetTile(topTileLoc, out Tile tile)) continue;

                BlockData blockData = GetBlockData(tile);
                if (blockData == null) continue;

                topTileLoc.y++;

                chunk.SetTile(rocks[random.Next(0, rocks.Count)], topTileLoc);
            }
        }
    }

    public BlockData GetBlockData(Tile tile)
    {
        BlockData blockData = tile.tileData is not IBlockData iblockData ? null : iblockData.GetBlockData();

        if (blockData == null) return null;
        if (blockData.Fullness != TileFullness.Full) return null;

        return blockData;
    }
}