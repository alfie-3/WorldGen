using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Cysharp.Threading.Tasks;

public class WorldGeneration : MonoBehaviour
{
    [SerializeField] int chunkSize;

    [SerializeField] Tilemap tilemap;
    [SerializeField] GameObject grass;
    [SerializeField] GameObject water;

    [SerializeField] SO_FastNoiseLiteGenerator noiseGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator noiseIsland;

    // Start is called before the first frame update
    async UniTaskVoid Start()
    {
        await GenerateChunk();
    }

    [ContextMenu("Regenerate")]
     async UniTaskVoid Regenerate()
    {
        tilemap.ClearAllTiles();

        noiseGenerator.SetSeed((int)System.DateTime.Now.Ticks);
        noiseIsland.SetSeed((int)System.DateTime.Now.Ticks);

        await GenerateChunk();
    }

    async UniTask GenerateChunk()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                int tileId = GetTileFromNoise(x, y);
                await CreateTile(tileId, new(x, 0, y));
            }
        }
    }

    async UniTask CreateTile(int tileId, Vector3Int coordinate)
    {
        if (tileId != 0) return;

        await InstantiateAsync(grass, 1, transform, coordinate, Quaternion.identity);
    }

    private int GetTileFromNoise(int x, int y)
    {
        float sample = noiseGenerator.GetNoiseClamped(new(x, y));
        sample = Mathf.Pow(sample, noiseIsland.GetNoiseClamped(new(x,y)));

        sample *= 2;

        return Mathf.FloorToInt(sample);
    }
}
