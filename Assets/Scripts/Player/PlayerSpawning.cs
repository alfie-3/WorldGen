using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawning : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab;
    tp_PlayerController currentPlayer = null;
    bool trySpawnPlayer = true;

    private void Awake()
    {
        WorldGenerationEvents.Regenerate += () => { trySpawnPlayer = true;};
    }

    public void FixedUpdate()
    {
        if (!trySpawnPlayer) return;

        foreach (KeyValuePair<Vector2Int, Chunk> chunk in WorldGeneration.ChunkDict)
        {
            if (WorldUtils.ChunkHasCollider(chunk.Key))
            {
                for (int x = 0; x < chunk.Value.Tiles.GetLength(0); x++)
                {
                    for (int z = 0; z < chunk.Value.Tiles.GetLength(2); z++)
                    {
                        if (chunk.Value.Tiles[x, 0, z] == null) continue;

                        Vector2Int sampleLoc = new Vector2Int(x + (chunk.Value.ChunkLocation.x * WorldGeneration.CHUNK_SIZE), z + (chunk.Value.ChunkLocation.y * WorldGeneration.CHUNK_SIZE));

                        Vector3Int topTileLoc = WorldUtils.GetTopTileLocation(new(sampleLoc.x, 0, sampleLoc.y));

                        if (!WorldUtils.TryGetTile(topTileLoc, out Tile tile)) continue;

                        BlockData blockData = FeatureGeneration.GetBlockData(tile);
                        if (blockData == null) continue;

                        topTileLoc.y += 2;

                        if (currentPlayer == null)
                        {
                            currentPlayer = Instantiate(playerPrefab, topTileLoc, Quaternion.identity).GetComponent<tp_PlayerController>();
                        }
                        else
                        {
                            currentPlayer.Teleport(topTileLoc);
                        }
                        trySpawnPlayer = false;

                        return;
                    }
                }
            }
        }
    }
}
