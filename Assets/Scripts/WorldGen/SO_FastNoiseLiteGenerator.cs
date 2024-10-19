using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FN_Generator", menuName = "New FN Generator", order = 0)]
public class SO_FastNoiseLiteGenerator: ScriptableObject
{
    [SerializeField] FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2S;
    [SerializeField] FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.Ridged;
    [SerializeField] FastNoiseLite.DomainWarpType domainWarpType = FastNoiseLite.DomainWarpType.OpenSimplex2;

    FastNoiseLite noise = null;

    bool dirty;
    public Action NoiseValuesChanged;

    public FastNoiseLite GetNoise()
    {
        if (noise != null && !dirty) return noise;

        CreateNoise();

        return noise;
    }

    public void CreateNoise()
    {
        noise = new FastNoiseLite();
        noise.SetNoiseType(noiseType);
        noise.SetFractalType(fractalType);
        noise.SetDomainWarpType(domainWarpType);
    }

    private void OnValidate()
    {
        dirty = true;
    }
}
