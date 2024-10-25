using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldRenderer : MonoBehaviour
{
    public WorldGeneration WorldGeneration;

    private void LateUpdate()
    {
        RenderParams rp = new();

        foreach (KeyValuePair<Vector2, Chunk> chunk in WorldGeneration.ChunkDict)
        {
            if (chunk.Value.ChunkStatus != Chunk.CHUNK_STATUS.GENERATED) return;

            foreach (KeyValuePair<Vector3, Tile> tile in chunk.Value.Tiles)
            {
                TileData data = tile.Value.tileData;
                if (rp.material != data.TileMaterial) rp = new(data.TileMaterial);
                Graphics.RenderMesh(rp, data.TileMesh, 0, Matrix4x4.Translate(Vector3.Scale(tile.Key, new Vector3(chunk.Value.ChunkLocation.x, 0, chunk.Value.ChunkLocation.y))));
            }
        }
    }
}
