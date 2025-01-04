using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData : ScriptableObject
{
    [field: SerializeField] public string TileId { get; private set; } = string.Empty;

    public virtual TileData GetTileData(Vector3Int position, ref byte rotation) { return this; }
}

public enum TileFullness
{
    Full,
    Half,
    None
}