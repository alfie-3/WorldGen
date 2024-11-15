using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[CreateAssetMenu(fileName = "New Rule Tile", menuName = "Tiles/New Rule Tile Data", order = 1)]
public class RuleTileData : TileData, IRuleTile
{
    public List<TilingRule> Rules;

    int[] Neighbours = new int[8];
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


    public void GetNeighbours(Chunk chunk, Vector3Int location)
    {
        for (int i = 0; i < NeighbourPositions.Count; i++)
        {
            Tile tile = WorldGeneration.GetTile(chunk, location + NeighbourPositions[i]);
            Neighbours[i] = (tile == null ? TilingRule.Neighbour.NoTile : TilingRule.Neighbour.TilePresent);
        }

        return;
    }

    public TileData GetTileData(Chunk chunk, Vector3Int location)
    {
        GetNeighbours(chunk, location);

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

    public class NeighbourRule
    {
        [HideInInspector] public string Name;
        public int Rule;
    }

    public void OnValidate()
    {
        ElementName = tile.TileId;
    }
}