using System.Drawing;
using UnityEngine;

public class WorldManagement : MonoBehaviour
{
    public static void SetTile(Vector3Int tileLocation, TileData tileData, bool placeOnNextVerticalTile = false)
    {
        if (!WorldUtils.IsTileCoordinateValid(tileLocation)) return;

        if (WorldUtils.TryGetTile(tileLocation, out Tile tile))
        {
            if (!placeOnNextVerticalTile)
                tile.SetTile(tileData, tileLocation);
            else
            {
                tileLocation.y++;
                SetTile(tileLocation, tileData, false);
            }
        }
        else
        {
            SetTile(tileLocation, tileData);
        }
    }

    public static void RemoveTile(Vector3Int tileLocation)
    {
        if (WorldUtils.TryGetChunk(WorldUtils.GetChunkLocation(tileLocation), out Chunk chunk))
        {
            chunk.ClearTile(tileLocation);
            UpdateAdjacentTiles(tileLocation);
            Chunk.RefreshChunk.Invoke(WorldUtils.GetChunkLocation(tileLocation));
        }
    }

    public static void SetTile(Vector3Int coordinate, TileData tile)
    {
        if (WorldUtils.TryGetChunk(WorldUtils.GetChunkLocation(coordinate), out Chunk chunk))
        {
            chunk.SetTile(tile, coordinate);
            UpdateAdjacentTiles(coordinate);
            Chunk.RefreshChunk.Invoke(WorldUtils.GetChunkLocation(coordinate));
        }
    }

    public static void UpdateAdjacentTiles(Vector3Int coordinate)
    {
        for (int i = 0; i < RuleTileData.NeighbourPositions.Length; i++)
        {
            Vector3Int refTileCoordinate = coordinate + RuleTileData.NeighbourPositions[i];

            if (WorldUtils.TryGetTile(refTileCoordinate, out Tile tile))
            {
                TileInfo prevTileInfo = new(tile);

                tile.RefreshTile(refTileCoordinate);

                Chunk.OnTileUpdate.Invoke(WorldUtils.GetChunkLocation(refTileCoordinate), prevTileInfo, new TileInfo(tile));
                Chunk.RefreshChunk.Invoke(WorldUtils.GetChunkLocation(refTileCoordinate));
            }
        }
    }
}
