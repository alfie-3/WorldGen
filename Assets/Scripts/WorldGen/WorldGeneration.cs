using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGeneration : MonoBehaviour
{
    [SerializeField] int chunkSize;

    [SerializeField] Tilemap tilemap;
    [SerializeField] TileBase grass;
    [SerializeField] TileBase water;

    [SerializeField] SO_FastNoiseLiteGenerator noiseGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator noiseIsland;

    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
    }

    [ContextMenu("Regenerate")]
    void Regenerate()
    {
        tilemap.ClearAllTiles();

        noiseGenerator.SetSeed((int)System.DateTime.Now.Ticks);
        noiseIsland.SetSeed((int)System.DateTime.Now.Ticks);

        GenerateMap();
    }

    public void GenerateMap()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                int tileId = GetTileFromNoise(x, y);
                CreateTile(tileId, new(x, y, 0));
            }
        }
    }

    private void CreateTile(int tileId, Vector3Int coordinate)
    {
        TileBase tile_prefab = tileId == 0 ? grass : water;
        tilemap.SetTile(coordinate, tile_prefab);
    }

    private int GetTileFromNoise(int x, int y)
    {
        float sample = noiseGenerator.GetNoiseClamped(new(x, y));
        sample = Mathf.Pow(sample, noiseIsland.GetNoiseClamped(new(x,y)));

        sample *= 2;

        return Mathf.FloorToInt(sample);
    }
}
