using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class WorldRenderer : MonoBehaviour
{
    private void LateUpdate()
    {
        foreach (Chunk chunk in WorldGeneration.ChunkDict.Values)
        {
            foreach (Tile tile in chunk.Tiles)
            {
                if (tile == null) continue;
                if (tile.DontDraw) continue;

                for (int i = 0; i < tile.tileData.TileMaterials.Length; i++)
                {
                    RenderParams renderParams = new RenderParams(tile.tileData.TileMaterials[i]);
                    Graphics.RenderMesh(renderParams, tile.tileData.TileMesh, i, tile.tileTransform);
                }
            }
        }
    }
}
