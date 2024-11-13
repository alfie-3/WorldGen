using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

[CreateAssetMenu(fileName = "New Rule Tile", menuName = "Tiles/New Rule Tile Data", order = 1)]
public class RuleTileData : TileData
{
    public List<TilingRule> Rules;

    public List<int> Neighbours;
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


    public List<int> GetNeighbours(Chunk chunk, Vector3Int location)
    {
        Tile tile = null;

        List<int> neighbours = new List<int>();

        for (int i = 0; i < NeighbourPositions.Count; i++)
        {
            tile = WorldGeneration.GetTile(chunk, location + NeighbourPositions[i]);
            neighbours.Add(tile == null ? 0 : 1);
        }

        return neighbours;
    }

    public override TileData GetTileData(Chunk chunk, Vector3Int location)
    {
        Neighbours = GetNeighbours(chunk, location);

        foreach (var rule in Rules)
        {
            if (rule.CheckReturnTile(Neighbours, out TileData data))
            {
                return data;
            }
        }

        return this;
    }
}

[System.Serializable]
public class TilingRule
{
    public TileData tile;

    public List<int> NeighbourRules = new List<int>();

    public bool CheckReturnTile(List<int> neighbours, out TileData newData)
    {
        int minCount = Math.Min(neighbours.Count, NeighbourRules.Count);

        for (int i = 0; i < minCount; i++)
        {
            newData = null;
            if (neighbours[i] != NeighbourRules[i]) { return false; }
        }

        newData = tile;
        return true;
    }
}