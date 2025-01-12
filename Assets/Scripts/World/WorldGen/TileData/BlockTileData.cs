using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Block Tile Data",  menuName = "Tiles/New Block Tile")]
public class BlockTileData : TileData, IBlockData
{
    [field: SerializeField] public BlockData BlockData { get; private set; }

    public BlockData GetBlockData() { return BlockData; }

    public override TileData GetTileData(Vector3Int position, ref byte rotation) { return this; }
}

[System.Serializable]
public class BlockData
{
    [field: SerializeField] public Mesh TileMesh { get; private set; }
    [field: SerializeField] public Mesh ColliderMesh { get; private set; }
    [field: SerializeField] public Material[] TileMaterials { get; private set; }

    [field: SerializeField] public TileFullness Fullness { get; private set; } = TileFullness.Full;
}

public interface IBlockData
{
    public BlockData GetBlockData();
}