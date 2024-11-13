using Cysharp.Threading.Tasks;
using System;
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

    public static Dictionary<Vector2Int, Chunk> ChunkDict { get; private set; } = new();

    public Action<Vector2Int> chunkReady;
    public Action<Vector2Int> chunkRemoved;

    Transform PlayerTransform;

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

        CheckClearChunks();
    }

    [ContextMenu("Regenerate")]
    async UniTaskVoid Regenerate()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        ChunkDict.Clear();

        noiseGenerator.Seed = (int)System.DateTime.Now.Ticks;
        noiseIsland.Seed = (int)System.DateTime.Now.Ticks;

        await GenerateChunks();
    }

    async UniTask GenerateChunks()
    {
        Vector2Int playerChunkLoc = GetPlayerChunkLocation();

        for (int x = playerChunkLoc.x - ChunkGenerationRange; x < playerChunkLoc.x + ChunkGenerationRange; x++)
        {
            for (int y = playerChunkLoc.y - ChunkGenerationRange; y < playerChunkLoc.y + ChunkGenerationRange; y++)
            {
                if (IsChunkGenerated(new(x, y))) { continue; }

                Chunk newChunk = await UniTask.RunOnThreadPool(() => GenerateChunk(new(x, y)), cancellationToken: tokenSource.Token);
                ChunkDict.TryAdd(new(x, y), newChunk);

                chunkReady.Invoke(new(x, y));
            }
        }
    }

    public Chunk GenerateChunk(Vector2Int chunkCoordinate)
    {
        //Create New Chunk
        Chunk newChunk = CreateChunk(chunkCoordinate);

        //Create tiles in chunk
        CreateTiles(newChunk);

        //Chunk is finished
        newChunk.ChunkStatus = Chunk.CHUNK_STATUS.GENERATED;

        return newChunk;
    }

    public Chunk CreateChunk(Vector2Int chunkLocation)
    {
        Chunk newChunk = new(chunkLocation)
        {
            ChunkStatus = Chunk.CHUNK_STATUS.GENERATING
        };

        return newChunk;
    }

    public static Chunk GetChunk(Vector2Int chunkCoordinate)
    {
        if (ChunkDict.TryGetValue(chunkCoordinate, out Chunk chunk))
        {
            return chunk;
        }
        return null;
    }

    public static Tile GetTile(Chunk chunk, Vector3Int tileCoordinate, CoordinateMode coordMode = CoordinateMode.Global)
    {
        if (chunk.GetTile(tileCoordinate, out Tile tile))
        {
            return tile;
        }
        else
        {
            return null;
        }
    }

    public bool IsChunkGenerated(Vector2Int chunkCoordinate)
    {
        if (ChunkDict.TryGetValue(chunkCoordinate, out Chunk chunk))
        {
            return chunk.ChunkStatus == Chunk.CHUNK_STATUS.GENERATED;
        }
        else
            return false;
    }

    public void CheckClearChunks()
    {
        foreach (Vector2Int loc in ChunkDict.Keys)
        {
            if (Vector2Int.Distance(GetPlayerChunkLocation(), ChunkDict[loc].ChunkLocation) > ChunkReleaseRange)
            {
                ChunkDict.Remove(loc);
                chunkRemoved.Invoke(loc);
            }
        }
    }

    public Vector2Int GetPlayerChunkLocation()
    {
        return Vector2Int.RoundToInt(new(PlayerTransform.position.x, PlayerTransform.position.z)) / CHUNK_SIZE;
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
                CreateTile(chunk, tileId, new(tileLocation.x, 0, tileLocation.y));
            }
        }
    }

    public void CreateTile(Chunk chunk, int tileId, Vector3Int coordinate)
    {
        if (tileId == 0) return;
        chunk.SetTile(grass.GetTileData(chunk, coordinate), coordinate);
    }

    private int GetTileFromNoise(int x, int y)
    {
        float sample = noiseGenerator.GetNoiseClamped(new(x, y));
        sample = Mathf.Pow(sample, noiseIsland.GetNoiseClamped(new(x, y)));

        sample *= 2;

        return Mathf.FloorToInt(sample);
    }

    private void OnApplicationQuit()
    {
        ChunkDict.Clear();

        tokenSource.Cancel();

        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }
}
