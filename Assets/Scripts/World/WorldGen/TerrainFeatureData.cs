using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Terrain Feature", menuName = "Terrain/TerrainFeature")]
public class TerrainFeatureData : ScriptableObject
{
    [SerializeField] GameObject featurePrefab = null;
}
