using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[CreateAssetMenu(fileName = "New Rule Tile", menuName = "Tiles/New Rule Tile Data", order = 1)]
public class RuleTileData : TileData
{
    public List<TilingRule> Rules;

    public int[] Neighbours = new int[8];
    public static List<Vector3Int> NeighbourPositions = new List<Vector3Int>()
    {
                new Vector3Int(-1, 0, 1),
                new Vector3Int(0, 0, 1),
                new Vector3Int(1, 0, 1),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, -1),
                new Vector3Int(0, 0, -1),
                new Vector3Int(1, 0, -1),
    };


    public void GetNeighbours(Vector3Int location)
    {
        for (int i = 0; i < NeighbourPositions.Count; i++)
        {
            if (WorldGeneration.GetTile(location + NeighbourPositions[i], out Tile tile))
            {
                Neighbours[i] = TilingRule.Neighbour.TilePresent;
            }
            else Neighbours[i] = TilingRule.Neighbour.NoTile;
        }
    }

    public override TileData GetTileData(Vector3Int position)
    {
        GetNeighbours(position);

        foreach (var rule in Rules)
        {
            if (rule.CheckReturnTile(Neighbours, out TileData data))
            {
                return data;
            }
        }

        return this;
    }

    private void OnValidate()
    {
        foreach (var rule in Rules)
        {
            rule.OnValidate();
        }
    }
}

[System.Serializable]
public class TilingRule
{
    [HideInInspector] public string ElementName; 

    public TileData tile;

    public List<int> NeighbourRules = new List<int>();

    public class Neighbour
    {
        public const int NoTile = 0;
        public const int TilePresent = 1;
        public const int Ignore = 2;
    }

    public bool CheckReturnTile(int[] neighbours, out TileData newData)
    {
        int minCount = Math.Min(neighbours.Length, NeighbourRules.Count);

        for (int i = 0; i < minCount; i++)
        {
            if (NeighbourRules[i] == Neighbour.Ignore) continue;

            newData = null;
            if (neighbours[i] != NeighbourRules[i]) { return false; }
        }

        newData = tile;
        return true;
    }

    public void OnValidate()
    {
        ElementName = tile.TileId;
    }
}