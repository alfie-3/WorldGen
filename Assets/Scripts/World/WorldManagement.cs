using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldManagement : MonoBehaviour
{
    public static void SetTile(Vector3Int tileLocation, TileData tileData)
    {

        if (WorldUtils.GetTile(tileLocation, out Tile tile))
        {
            tile.SetTile(tileData);
        }
    }

    public static void UpdateAdjacentTiles(Vector3Int coordinate)
    {
        for (int i = 0; i < RuleTileData.NeighbourPositions.Length; i++)
        {
            if (WorldUtils.GetTile(coordinate + RuleTileData.NeighbourPositions[i], out Tile tile))
            {
                tile.RefreshTile();
            }
        }
    }
}
