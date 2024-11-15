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
}
