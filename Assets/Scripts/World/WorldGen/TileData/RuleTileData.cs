using System;
using System.Collections.Generic;
using UnityEngine;
using static TilingRule;
using static UnityEngine.RuleTile.TilingRuleOutput;

[CreateAssetMenu(fileName = "New Rule Tile", menuName = "Tiles/New Rule Tile Data", order = 1)]
public class RuleTileData : TileData
{
    public List<TilingRule> Rules;

    public static readonly Vector3Int[] NeighbourPositions =
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


    public int[] GetNeighbours(Vector3Int location)
    {
        int[] neighbours = new int[NeighbourPositions.Length];
        
        for (int i = 0; i < NeighbourPositions.Length; i++)
        {
            if (WorldUtils.GetTile(location + NeighbourPositions[i], out Tile tile))
            {
                neighbours[i] = TilingRule.Neighbour.TilePresent;
            }
            else neighbours[i] = TilingRule.Neighbour.NoTile;
        }

        return neighbours;
    }

    public override TileData GetTileData(Vector3Int position)
    {
        int[] neighbours = GetNeighbours(position);

        foreach (var rule in Rules)
        {
            if (rule.CheckReturnTile(neighbours, out TileData data))
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