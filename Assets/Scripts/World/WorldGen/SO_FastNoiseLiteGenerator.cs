using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FN_Generator", menuName = "New FN Generator", order = 0)]
public class SO_FastNoiseLiteGenerator: ScriptableObject
{
    [field: SerializeField] public float NoiseScale {  get; private set; }
    [SerializeField] FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2S;
    [SerializeField] FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.Ridged;
    [SerializeField] FastNoiseLite.DomainWarpType domainWarpType = FastNoiseLite.DomainWarpType.OpenSimplex2;
    public int Seed = 1337;

    FastNoiseLite noise = null;

    public Action NoiseValuesChanged;

    public float GetNoise(Vector2 coordinate)
    {
        if (noise != null)
        {
           return noise.GetNoise(coordinate.x / NoiseScale, coordinate.y / NoiseScale); ;
        }

        CreateNoise();

        return noise.GetNoise(coordinate.x / NoiseScale, coordinate.y / NoiseScale);
    }

    public float GetNoiseClamped(Vector2 coordinate, float min = 0, float max = 1)
    {
        float noise = GetNoise(coordinate);
        return Mathf.Clamp(noise, min, max);
    }

    public void CreateNoise()
    {
        noise = new FastNoiseLite(Seed);
        noise.SetNoiseType(noiseType);
        noise.SetFractalType(fractalType);
        noise.SetDomainWarpType(domainWarpType);
        Seed = (int)System.DateTime.Now.Ticks;
    }

    public void SetSeed(int seed)
    {
        noise.SetSeed(seed);
    }
}
