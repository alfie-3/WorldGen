using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

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

        noiseGenerator.Seed = (int)DateTime.Now.Ticks;
        noiseIsland.Seed = (int)DateTime.Now.Ticks;

        await GenerateChunks();
    }

    async UniTask GenerateChunks()
    {
        Vector2Int playerChunkLoc = GetPlayerChunkLocation();

        for (int x = playerChunkLoc.x - ChunkGenerationRange; x < playerChunkLoc.x + ChunkGenerationRange; x++)
        {
            for (int y = playerChunkLoc.y - ChunkGenerationRange; y < playerChunkLoc.y + ChunkGenerationRange; y++)
            {
                Vector2Int chunkLoc = new Vector2Int(x, y);

                if (IsChunkGenerated(chunkLoc)) { continue; }

                Chunk newChunk = CreateChunk(chunkLoc);
                await UniTask.RunOnThreadPool(() => GenerateChunk(newChunk), cancellationToken: tokenSource.Token);
                ChunkDict.TryAdd(chunkLoc, newChunk);

                chunkReady.Invoke(chunkLoc);
            }
        }
    }

    public Chunk GenerateChunk(Chunk chunk)
    {
        //Create tiles in chunk
        CreateTiles(chunk);

        //Chunk is finished
        chunk.ChunkStatus = Chunk.CHUNK_STATUS.GENERATED;

        return chunk;
    }

    public Chunk CreateChunk(Vector2Int chunkLocation)
    {
        Chunk newChunk = new(chunkLocation)
        {
            ChunkStatus = Chunk.CHUNK_STATUS.GENERATING
        };

        ChunkDict.TryAdd(chunkLocation, newChunk);

        return newChunk;
    }

    public static bool GetChunk(Vector2Int chunkCoordinate, out Chunk returnChunk)
    {
        if (ChunkDict.TryGetValue(chunkCoordinate, out Chunk chunk))
        {
            returnChunk = chunk;
            return true;
        }

        returnChunk = null;
        return false;
    }

    public static bool GetTile(Vector3Int tileCoordinate, out Tile returnTile)
    {
        returnTile = null;
        Vector2Int chunkLoc = Vector2Int.zero;
        chunkLoc = new Vector2Int(RoundFloatToInt(tileCoordinate.x, CHUNK_SIZE), RoundFloatToInt(tileCoordinate.z, CHUNK_SIZE));

        if (GetChunk(chunkLoc, out Chunk returnChunk))
        { 
            if (returnChunk.GetTile(tileCoordinate, out Tile tile))
            {
                returnTile = tile;
                return true;
            }

            else return false;
        }

        return false;
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
        return Vector2Int.FloorToInt(new Vector2(PlayerTransform.position.x, PlayerTransform.position.z) / CHUNK_SIZE);
    }

    public static int RoundFloatToInt(int a, int b)
    {
        int res = a / b;
        return (a < 0 && a != b * res) ? res - 1 : res;
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
        chunk.SetTile(grass, coordinate);
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
