using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Rule Tile", menuName = "Tiles/New Rule Tile Data", order = 1)]
public class RuleTileData : TileData
{
    [SerializeField] List<TileRule> TopLeftRules;
    [SerializeField] List<TileRule> TopMiddleRules;
    [SerializeField] List<TileRule> TopRightRules;
    [SerializeField] List<TileRule> MiddleLeftRules;
    [SerializeField] List<TileRule> CenterRules;
    [SerializeField] List<TileRule> MiddleRightRules;
    [SerializeField] List<TileRule> BottomLeftRules;
    [SerializeField] List<TileRule> BottomMiddleRules;
    [SerializeField] List<TileRule> BottomRightRules;

    public override TileData GetTileData()
    {
        return base.GetTileData();
    }
}
