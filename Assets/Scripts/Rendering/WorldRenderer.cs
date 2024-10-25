using System.Collections.Generic;
using UnityEngine;

public class WorldRenderer : MonoBehaviour
{
    public WorldGeneration WorldGeneration;

    Dictionary<string, List<Tile>> batchTilesDict = new Dictionary<string, List<Tile>>();

    private void LateUpdate()
    {
        foreach (KeyValuePair<Vector2, Chunk> chunk in WorldGeneration.ChunkDict)
        {
            if (chunk.Value.ChunkStatus != Chunk.CHUNK_STATUS.GENERATED) return;

            foreach (KeyValuePair<Vector3, Tile> tile in chunk.Value.Tiles)
            {
                batchTilesDict.TryAdd(tile.Value.tileData.TileId, new List<Tile>());
                batchTilesDict[tile.Value.tileData.TileId].Add(tile.Value);

                //TileData data = tile.Value.tileData;
                //if (rp.material != data.TileMaterial) rp = new(data.TileMaterial);
                //Graphics.RenderMesh(rp, data.TileMesh, 0, Matrix4x4.Translate(tile.Key));
            }

            foreach (KeyValuePair<string, List<Tile>> batchTileList in batchTilesDict)
            {
                Matrix4x4[] matricies = new Matrix4x4[batchTileList.Value.Count];
                for (int i = 0; i < matricies.Length; i++)
                {
                    matricies[i] = Matrix4x4.Translate(batchTileList.Value[i].tileLocation);
                }

                Graphics.DrawMeshInstanced(batchTileList.Value[0].tileData.TileMesh, 0, batchTileList.Value[0].tileData.TileMaterial, matricies);
            }

            batchTilesDict.Clear();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (WorldGeneration == null) return;
        if (WorldGeneration.ChunkDict == null) return;

        foreach (KeyValuePair<Vector2, Chunk> chunk in WorldGeneration.ChunkDict)
        {
            Gizmos.matrix = Matrix4x4.Translate(new(WorldGeneration.CHUNK_SIZE / 2, 0, WorldGeneration.CHUNK_SIZE / 2));

            switch (chunk.Value.ChunkStatus)
            {
                case Chunk.CHUNK_STATUS.GENERATED:
                    Gizmos.color = Color.green;
                    break;
                case Chunk.CHUNK_STATUS.GENERATING:
                    Gizmos.color = Color.yellow;
                    break;
                case Chunk.CHUNK_STATUS.UNGENERATED:
                    Gizmos.color = Color.red;
                    break;
                case Chunk.CHUNK_STATUS.SLEEPING:
                    Gizmos.color = Color.blue;
                    break;
            } //Change chunk boundary colour

            Gizmos.DrawWireCube(new Vector3(chunk.Value.ChunkLocation.x * WorldGeneration.CHUNK_SIZE, 0, chunk.Value.ChunkLocation.y * WorldGeneration.CHUNK_SIZE), Vector3.one * WorldGeneration.CHUNK_SIZE);
        }
    }
}
