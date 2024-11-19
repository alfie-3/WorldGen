using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WorldMeshBuilder : MonoBehaviour
{
    public class ChunkMeshData
    {
        public Mesh Mesh;
        public Material[] Materials;
        public MeshCollider Collider;

        public bool Dirty = true;

        public ChunkMeshData(Mesh mesh, Material[] materials)
        {
            this.Mesh = mesh;
            this.Materials = materials;
        }
    }

    static ConcurrentDictionary<Vector2Int, ChunkMeshData> ChunkMeshes = new();

    private void OnEnable()
    {
        WorldGeneration.ChunkReady += CreateChunkMesh;
    }

    private void LateUpdate()
    {
        RenderMeshes();
    }

    public void CreateChunkMesh(Vector2Int chunkLoc)
    {
        if (ChunkMeshes.TryAdd(chunkLoc, GenerateChunkMeshData(chunkLoc)))
        {
            GenerateCollider(chunkLoc);
        }
    }

    public void UpdateChunkMesh(Vector2Int chunkLoc)
    {
        if (ChunkMeshes.ContainsKey(chunkLoc))
        {
            ChunkMeshes[chunkLoc] = GenerateChunkMeshData(chunkLoc);
        }
        else
        {
            CreateChunkMesh(chunkLoc);
        }

        GenerateCollider(chunkLoc);
    }

    public void ReleaseChunk(Vector2Int chunkLoc)
    {
        if (ChunkMeshes.TryRemove(chunkLoc, out ChunkMeshData chunkMeshData))
        {
            Destroy(chunkMeshData.Collider);
        }
    }

    public ChunkMeshData GenerateChunkMeshData(Vector2Int chunkPos)
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



        ChunkMeshData chunkMeshData = new(mesh, materials.ToArray());
        chunkMeshData.Dirty = false;

        return chunkMeshData;
    }

    public void GenerateCollider(Vector2Int chunkLoc)
    {
        if (ChunkMeshes[chunkLoc].Collider != null)
        {
            ChunkMeshes[chunkLoc].Collider.sharedMesh = ChunkMeshes[chunkLoc].Mesh;
        }
        else
        {
            MeshCollider collider = gameObject.AddComponent<MeshCollider>();

            ChunkMeshes[chunkLoc].Collider = collider;
            ChunkMeshes[chunkLoc].Collider.sharedMesh = ChunkMeshes[chunkLoc].Mesh;
        }
    }

    public static void SetChunkDirty(Vector2Int chunkLoc)
    {
        if (ChunkMeshes.TryGetValue(chunkLoc, out ChunkMeshData value))
        {
            value.Dirty = true;
        }
    }

    public void RenderMeshes()
    {
        foreach (KeyValuePair<Vector2Int, ChunkMeshData> ChunkMeshPairs in ChunkMeshes)
        {
            if (ChunkMeshPairs.Value.Dirty)
            {
                UpdateChunkMesh(ChunkMeshPairs.Key);
            }

            if (Vector2Int.Distance(ChunkMeshPairs.Key, WorldUtils.GetChunkLocation(WorldGeneration.PlayerTransform.position)) > WorldGeneration.ChunkGenerationRange + 1) { continue; }

            foreach (Material material in ChunkMeshPairs.Value.Materials)
            {
                RenderParams renderParams = new(material);

                Vector3 renderPos = Vector3.zero;

                Graphics.RenderMesh(renderParams, ChunkMeshPairs.Value.Mesh, 0, Matrix4x4.Translate(renderPos));
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
