using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Rule Tile", menuName = "Tiles/New Rule Tile Data", order = 1)]
public class RuleTileData : TileData
{
    public List<TilingRule> Rules;

    public static readonly Vector3Int[] NeighbourPositions =
    {
        //Surrounding Tiles
                new Vector3Int(-1, 0, 1),
                new Vector3Int(0, 0, 1),
                new Vector3Int(1, 0, 1),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, -1),
                new Vector3Int(0, 0, -1),
                new Vector3Int(1, 0, -1),
        //Above and Below Tiles
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, -1, 0)

    };


    public int[] GetNeighbours(Vector3Int location)
    {
        int[] neighbours = new int[NeighbourPositions.Length];

        for (int i = 0; i < NeighbourPositions.Length; i++)
        {
            if (WorldUtils.TryGetTile(location + NeighbourPositions[i], out Tile tile))
            {
                neighbours[i] = TilingRule.Neighbour.TilePresent;
            }
            else neighbours[i] = TilingRule.Neighbour.NoTile;
        }

        return neighbours;
    }

    public override TileData GetTileData(Vector3Int position, ref Matrix4x4 tileTransform)
    {
        int[] neighbours = GetNeighbours(position);

        foreach (var rule in Rules)
        {
            if (rule.CheckReturnTile(neighbours, out TileData data, ref tileTransform))
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

    //Tile data to return
    public TileData tile;

    //List of rules the neighbours will check against
    public List<int> NeighbourRules = new List<int>();

    //IDK WHAT THIS IS YET
    public Dictionary<Vector3Int, int> CachedNeighourRulesDict;
    public Dictionary<Vector3Int, int> NeighbourRulesDict
    {
        get
        {

            if (CachedNeighourRulesDict != null) return CachedNeighourRulesDict;

            CachedNeighourRulesDict = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < RuleTileData.NeighbourPositions.Length; i++)
            {
                if (i > NeighbourRules.Count-1)
                    CachedNeighourRulesDict.Add(RuleTileData.NeighbourPositions[i], Neighbour.Ignore);
                else
                    CachedNeighourRulesDict.Add(RuleTileData.NeighbourPositions[i], NeighbourRules[i]);
            }

            return CachedNeighourRulesDict;
        }
    }

    //Specifies the kinds of checks that a tile will undergo
    //This is to reduce remaking the same kind of tile for different symmetries/rotations
    public enum TILE_TRANSFORM
    {
        Static,
        MirrorX,
        MirrorY,
        MirrorXY,
        Rotated
    }

    //For Rotation Rule Tiles
    public const int RotationAngle = 90;
    public const int RotationCount = 360 / RotationAngle;

    public TILE_TRANSFORM TileTransformation;

    public class Neighbour
    {
        public const int NoTile = 0;
        public const int TilePresent = 1;
        public const int Ignore = 2;
    }

    public bool CheckReturnTile(int[] neighbours, out TileData newData, ref Matrix4x4 tileTransform)
    {
        newData = tile;


        switch (TileTransformation)
        {
            case (TILE_TRANSFORM.Static):
                if (CheckTileMatches(neighbours)) return true;
                break;

            case (TILE_TRANSFORM.MirrorX):
                if (CheckTileMatches(neighbours, true, false)) return true;
                break;

            case (TILE_TRANSFORM.MirrorY):
                if (CheckTileMatches(neighbours, false, true)) return true;
                break;

            case (TILE_TRANSFORM.MirrorXY):
                if (CheckTileMatches(neighbours, false, false)) return true;
                if (CheckTileMatches(neighbours, true, true)) return true;
                if (CheckTileMatches(neighbours, true, false)) return true;
                if (CheckTileMatches(neighbours, false, true)) return true;
                break;

            case (TILE_TRANSFORM.Rotated):
                for (int angle = 0; angle < 360; angle += RotationAngle)
                {
                    if (CheckRotationalTileMatch(neighbours, angle))
                    {
                        tileTransform = Matrix4x4.TRS(tileTransform.GetPosition(), Quaternion.Euler(0f, -angle, 0f), Vector3.one);
                        return true;
                    }
                }
                break;
        }

        return false;
    }

    public bool CheckTileMatches(int[] neighbours, bool mirrorX = false, bool mirrorY = false)
    {
        int minCount = Math.Min(neighbours.Length, NeighbourRules.Count);

        for (int i = 0; i < minCount; i++)
        {
            int mirroredPos = NeighbourRulesDict[GetMirroredPosition(RuleTileData.NeighbourPositions[i], mirrorX, mirrorY)];
            if (mirroredPos == Neighbour.Ignore) continue;

            if (neighbours[i] != mirroredPos) { return false; }
        }

        return true;
    }

    public bool CheckRotationalTileMatch(int[] neighbours, int angle, bool mirrorX = false)
    {
        int minCount = Math.Min(neighbours.Length, NeighbourRules.Count);

        for (int i = 0; i < minCount; i++)
        {
            Vector3Int neighbourPos = mirrorX ? GetMirroredPosition(RuleTileData.NeighbourPositions[i], true, false) : RuleTileData.NeighbourPositions[i];
            int rotatedNeighbourValue = GetRotatedNeighbourRule(neighbourPos, angle);

            if (rotatedNeighbourValue == Neighbour.Ignore) continue;

            if (neighbours[i] != rotatedNeighbourValue) { return false; }
        }

        return true;
    }

    public int GetRotatedNeighbourRule(Vector3Int position, int rotation)
    {
        switch (rotation)
        {
            case 0:
                return NeighbourRulesDict[position];
            case 90:
                return NeighbourRulesDict[new(position.z, position.y, -position.x)];
            case 180:
                return NeighbourRulesDict[new(-position.x, position.y, -position.z)];
            case 270:
                return NeighbourRulesDict[new(-position.z, position.y, position.x)];
        }

        return NeighbourRulesDict[position];
    }

    public Vector3Int GetMirroredPosition(Vector3Int position, bool mirrorX, bool mirrorY)
    {
        if (mirrorX)
            position.x *= -1;
        if (mirrorY)
            position.z *= -1;
        return position;
    }

    public void OnValidate()
    {
        if (tile == null)
        {
            ElementName = "None";
            return;
        }

        ElementName = tile.TileId;
    }
}