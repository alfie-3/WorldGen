using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGeneration : MonoBehaviour
{
    const int CHUNK_SIZE = 16;

    //Tile stuff TEMP
    [SerializeField] GameObject grass;
    [SerializeField] GameObject water;

    //Noise Layers
    [SerializeField] SO_FastNoiseLiteGenerator noiseGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator noiseIsland;

    //Chunk Generation
    public int ChunkGenerationRange = 5;

    public static Dictionary<Vector2Int, Chunk> ChunkDict { get; private set; } = new();

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
                await GenerateChunk(new(x,y));
            }
        }
    }

    async UniTask GenerateChunk(Vector2Int chunkCoordinate)
    {
        //Create New Chunk
        Chunk newChunk = CreateChunk(chunkCoordinate);

        //Float for chunkgeneration speed
        float timeStart = Time.unscaledTime;

        //Create tiles in chunk
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                Vector2Int tileLocation = new Vector2Int(x + (chunkCoordinate.x * CHUNK_SIZE), y + (chunkCoordinate.y * CHUNK_SIZE));
                int tileId = GetTileFromNoise(tileLocation.x, tileLocation.y);
                await CreateTile(chunkCoordinate, tileId, new(tileLocation.x, 0, tileLocation.y));
            }
        }

        //Chunk is finished
        newChunk.SetGenerationStatus(Chunk.GenerationStatus.GENERATED);
        Debug.Log($"{chunkCoordinate} Generated in {Time.unscaledTime - timeStart} seconds");
    }

    public Chunk CreateChunk(Vector2Int chunkLocation)
    {
        Chunk newChunk = new(chunkLocation);
        newChunk.SetGenerationStatus(Chunk.GenerationStatus.GENERATING);
        ChunkDict.Add(chunkLocation, newChunk);

        return newChunk;
    }

    async UniTask CreateTile(Vector2Int chunkLocation, int tileId, Vector3Int coordinate)
    {
        await InstantiateAsync(tileId == 0 ? water : grass, 1, transform, coordinate, Quaternion.identity);
        ChunkDict[chunkLocation].AddTile(new(tileId.ToString()), coordinate);
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
        foreach(Transform child in transform)
            Destroy(child.gameObject);
    }
}
