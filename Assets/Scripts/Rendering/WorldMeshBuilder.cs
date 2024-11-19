using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WorldMeshBuilder : MonoBehaviour
{
    public class ChunkData
    {
        public ChunkMeshData MeshData;
        public MeshCollider Collider;

        public bool Dirty = true;

        public class ChunkMeshData
        {
            public Mesh Mesh;
            public Material[] Materials;

            public ChunkMeshData(Mesh mesh, Material[] materials)
            {
                Mesh = mesh;
                Materials = materials;
            }
        }
    }

    static ConcurrentDictionary<Vector2Int, ChunkData> ChunkMeshes = new();

    private void OnEnable()
    {
        WorldGeneration.ChunkReady += UpdateChunkMesh;
        WorldGeneration.ChunkReleased += ReleaseChunk;
    }

    private void LateUpdate()
    {
        RenderMeshes();
    }

    public void UpdateChunkMesh(Vector2Int chunkLoc)
    {
        if (ChunkMeshes.ContainsKey(chunkLoc))
        {
            ChunkMeshes[chunkLoc].MeshData = GenerateChunkMeshData(chunkLoc);
        }
        else
        {
            if (ChunkMeshes.TryAdd(chunkLoc, new()))
            {
                ChunkMeshes[chunkLoc].MeshData = GenerateChunkMeshData(chunkLoc);
            }
        }

        UpdateCollider(chunkLoc);
        ChunkMeshes[chunkLoc].Dirty = false;
    }

    public void ReleaseChunk(Vector2Int chunkLoc)
    {
        if (ChunkMeshes.TryRemove(chunkLoc, out ChunkData chunkMeshData))
        {
            Destroy(chunkMeshData.Collider);
        }
    }

    public ChunkData.ChunkMeshData GenerateChunkMeshData(Vector2Int chunkPos)
    {
        List<CombineInstance> instances = new List<CombineInstance>();
        CombineInstance instance = new();

        HashSet<Material> materials = new HashSet<Material>();

        foreach (KeyValuePair<Vector3Int, Tile> tile in WorldGeneration.ChunkDict[chunkPos].Tiles)
        {
            TileData data = tile.Value.tileData;

            instance.transform = Matrix4x4.Translate(tile.Value.tileLocation);
            instance.mesh = data.TileMesh;

            instances.Add(instance);
            materials.Add(tile.Value.tileData.TileMaterial);
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(instances.ToArray(), true, true);

        ChunkData.ChunkMeshData chunkMeshData = new(mesh, materials.ToArray());

        return chunkMeshData;
    }

    public void UpdateCollider(Vector2Int chunkLoc)
    {
        if (ChunkMeshes[chunkLoc].Collider != null)
        {
            ChunkMeshes[chunkLoc].Collider.sharedMesh = ChunkMeshes[chunkLoc].MeshData.Mesh;
        }
        else
        {
            MeshCollider collider = gameObject.AddComponent<MeshCollider>();

            ChunkMeshes[chunkLoc].Collider = collider;
            ChunkMeshes[chunkLoc].Collider.sharedMesh = ChunkMeshes[chunkLoc].MeshData.Mesh;
        }
    }

    public static void SetChunkDirty(Vector2Int chunkLoc)
    {
        if (ChunkMeshes.TryGetValue(chunkLoc, out ChunkData value))
        {
            value.Dirty = true;
        }
    }

    public void RenderMeshes()
    {
        foreach (KeyValuePair<Vector2Int, ChunkData> ChunkMeshPairs in ChunkMeshes)
        {
            if (ChunkMeshPairs.Value.Dirty)
            {
                UpdateChunkMesh(ChunkMeshPairs.Key);
            }

            if (Vector2Int.Distance(ChunkMeshPairs.Key, WorldUtils.GetChunkLocation(WorldGeneration.PlayerTransform.position)) > WorldGeneration.ChunkGenerationRange + 1) { continue; }

            foreach (Material material in ChunkMeshPairs.Value.MeshData.Materials)
            {
                RenderParams renderParams = new(material);

                Vector3 renderPos = Vector3.zero;

                Graphics.RenderMesh(renderParams, ChunkMeshPairs.Value.MeshData.Mesh, 0, Matrix4x4.Translate(renderPos));
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (WorldGeneration.ChunkDict == null) return;

        foreach (KeyValuePair<Vector2Int, Chunk> chunk in WorldGeneration.ChunkDict)
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

            Gizmos.DrawWireCube(new Vector3(chunk.Value.ChunkLocation.x * WorldGeneration.CHUNK_SIZE - 0.5f, 0, chunk.Value.ChunkLocation.y * WorldGeneration.CHUNK_SIZE - 0.5f), Vector3.one * WorldGeneration.CHUNK_SIZE);
            Handles.Label(new Vector3(chunk.Value.ChunkLocation.x * WorldGeneration.CHUNK_SIZE, 0, chunk.Value.ChunkLocation.y * WorldGeneration.CHUNK_SIZE), chunk.Value.ChunkLocation.ToString());
        }
    }
}
