using System.Collections.Generic;
using UnityEngine;

public class WorldGeneration : MonoBehaviour
{
    [SerializeField] int chunkSize;

    [SerializeField] GameObject grass;
    [SerializeField] GameObject water;

    List<List<int>> noise_grid = new List<List<int>>();
    List<List<GameObject>> tile_grid = new List<List<GameObject>>();

    [SerializeField] float magnification = 7;

    [SerializeField] SO_FastNoiseLiteGenerator noiseGenerator = default;

    int x_offset = 0;
    int y_offset = 0;

    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
    }

    [ContextMenu("Regenerate")]
    void Regenerate()
    {
        foreach(Transform child in transform)
            Destroy(child.gameObject);

        GenerateMap();
    }

    public void GenerateMap()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            noise_grid.Add(new List<int>());
            tile_grid.Add(new List<GameObject>());

            for (int y = 0; y < chunkSize; y++)
            {
                int tileId = GetTileUsingPerlin(x, y);
                noise_grid[x].Add(tileId);
                CreateTile(tileId, new(x, y));
            }
        }
    }

    private void CreateTile(int tileId, Vector2 coordinate)
    {
        GameObject tile_prefab = tileId == 0 ? grass : water;
        GameObject tile = Instantiate(tile_prefab, coordinate, Quaternion.identity);
        tile.transform.parent = transform;
    }

    private int GetTileUsingPerlin(int x, int y)
    {
        float rawSimplex = noiseGenerator.GetNoise().GetNoise(
            (x - x_offset) / magnification,
            (y - y_offset) / magnification
            );

        float rawPerlin = Mathf.PerlinNoise(
            (x - x_offset) / magnification,
            (y - y_offset) / magnification
            );

        float clamp_perlin = Mathf.Clamp(rawSimplex, 0f, 1f);
        float scale_perlin = clamp_perlin * 2;
        
        return Mathf.FloorToInt(scale_perlin);
    }
}
