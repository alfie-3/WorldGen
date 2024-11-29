using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tile Data", menuName = "Tiles/New Tile Data", order = 0)]
public class TileData : ScriptableObject
{
    [field: SerializeField] public string TileId {  get; private set; } = string.Empty;
    [field: SerializeField] public Mesh TileMesh {  get; private set; }
    [field: SerializeField] public Material[] TileMaterials { get; private set; }

    public virtual TileData GetTileData(Vector3Int position, ref Matrix4x4 tileTransform) { return this; }
}
