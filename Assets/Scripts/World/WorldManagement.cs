using System;
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
        else
        {
            CreateTile(tileLocation, tileData);
        }
    }

    public static void CreateTile(Vector3Int coordinate, TileData tile)
    {
        if (WorldUtils.GetChunk(WorldUtils.GetChunkLocation(coordinate), out Chunk chunk))
        {
            chunk.SetTile(tile, coordinate);
            UpdateAdjacentTiles(coordinate);
            WorldMeshBuilder.SetChunkDirty(WorldUtils.GetChunkLocation(coordinate));
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
