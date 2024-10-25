using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGeneration : MonoBehaviour
{
    public const int CHUNK_SIZE = 16;

    //Tile stuff TEMP
    [SerializeField] TileData grass;

    //Noise Layers
    [SerializeField] SO_FastNoiseLiteGenerator noiseGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator noiseIsland;

    CancellationTokenSource tokenSource = new CancellationTokenSource();

    //Chunk Generation
    public int ChunkGenerationRange = 5;

    public static Dictionary<Vector2, Chunk> ChunkDict { get; private set; } = new();

    // Start is called before the first frame update
    async UniTaskVoid Start()
    {
        await GenerateChunks();
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
        for (int x = 0; x < ChunkGenerationRange; x++)
        {
            for (int y = 0; y < ChunkGenerationRange; y++)
            {
                await UniTask.RunOnThreadPool(() => GenerateChunk(new(x, y)), cancellationToken: tokenSource.Token);
            }
        }
    }

    public void GenerateChunk(Vector2Int chunkCoordinate)
    {
        //Create New Chunk
        Chunk newChunk = CreateChunk(chunkCoordinate);

        //Create tiles in chunk
        CreateTiles(chunkCoordinate);

        //Chunk is finished
        newChunk.ChunkStatus = Chunk.CHUNK_STATUS.GENERATED;
    }

    public Chunk CreateChunk(Vector2Int chunkLocation)
    {
        Chunk newChunk = new(chunkLocation)
        {
            ChunkStatus = Chunk.CHUNK_STATUS.GENERATING
        };

        ChunkDict.Add(chunkLocation, newChunk);

        return newChunk;
    }

    public void CreateTiles(Vector2Int chunkCoordinate)
    {
        //Create tiles in chunk
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                Vector2Int tileLocation = new Vector2Int(x + (chunkCoordinate.x * CHUNK_SIZE), y + (chunkCoordinate.y * CHUNK_SIZE));
                int tileId = GetTileFromNoise(tileLocation.x, tileLocation.y);
                CreateTile(chunkCoordinate, tileId, new(tileLocation.x, 0, tileLocation.y));
            }
        }
    }

    public void CreateTile(Vector2Int chunkLocation, int tileId, Vector3Int coordinate)
    {
        if (tileId == 0) return;
        ChunkDict[chunkLocation].SetTile(grass, coordinate);
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

        foreach(Transform child in transform)
            Destroy(child.gameObject);
    }
}
