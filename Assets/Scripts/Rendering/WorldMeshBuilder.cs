using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            public Dictionary<Material, Mesh> meshDataDict = new Dictionary<Material, Mesh>();
            public Mesh ColliderMesh = new();
        }
    }

    static ConcurrentDictionary<Vector2Int, ChunkData> ChunkDataDict = new();

    private void OnEnable()
    {
        WorldGeneration.ChunkReady += (value) => StartCoroutine(UpdateChunkMesh(value));
        WorldGeneration.ChunkReleased += ReleaseChunk;
    }

    private void LateUpdate()
    {
        RenderMeshes();
    }

    public IEnumerator UpdateChunkMesh(Vector2Int chunkLoc)
    {
        if (ChunkDataDict.ContainsKey(chunkLoc))
        {
            ChunkDataDict[chunkLoc].Dirty = false;
        }

        yield return GenerateChunkMeshData(chunkLoc, value =>
        {
            ChunkDataDict.TryAdd(chunkLoc, new());
            ChunkDataDict[chunkLoc].Dirty = false;
            ChunkDataDict[chunkLoc].MeshData = value;
        }
        );

        UpdateCollider(chunkLoc);

        yield return null;
    }

    public void ReleaseChunk(Vector2Int chunkLoc)
    {
        if (ChunkDataDict.TryRemove(chunkLoc, out ChunkData chunkMeshData))
        {
            Destroy(chunkMeshData.Collider);
        }
    }

    public IEnumerator GenerateChunkMeshData(Vector2Int chunkPos, Action<ChunkData.ChunkMeshData> meshData)
    {
        Debug.Log($"Generating chunk at {chunkPos}");

        Dictionary<Material, List<CombineInstance>> instances = new();
        CombineInstance instance = new();

        foreach (Tile tile in WorldGeneration.ChunkDict[chunkPos].Tiles)
        {
            //Ignore tiles marked as "Dont Draw"


            instance.transform = Matrix4x4.TRS(tile.tileLocation, tile.tileTransform.rotation, tile.tileTransform.lossyScale);
            instance.mesh = tile.tileData.TileMesh;

            for (int i = 0; i < tile.tileData.TileMaterials.Length; i++)
            {
                instance.subMeshIndex = i;

                instances.TryAdd(tile.tileData.TileMaterials[i], new());

                instances[tile.tileData.TileMaterials[i]].Add(instance);
            }
        }

        ChunkData.ChunkMeshData chunkMeshData = new();
        foreach (KeyValuePair<Material, List<CombineInstance>> combineInstance in instances)
        {
            Mesh mesh = new Mesh();

            mesh.CombineMeshes(combineInstance.Value.ToArray(), true, true);
            chunkMeshData.meshDataDict.Add(combineInstance.Key, mesh);
        }

        yield return null;
        meshData(chunkMeshData);
    }

    public void UpdateCollider(Vector2Int chunkLoc)
    {
        if (ChunkDataDict[chunkLoc].Collider != null)
        {
            ChunkDataDict[chunkLoc].Collider.sharedMesh = ChunkDataDict[chunkLoc].MeshData.ColliderMesh;
        }
        else
        {
            MeshCollider collider = gameObject.AddComponent<MeshCollider>();

            ChunkDataDict[chunkLoc].Collider = collider;
            ChunkDataDict[chunkLoc].Collider.sharedMesh = ChunkDataDict[chunkLoc].MeshData.ColliderMesh;
        }
    }

    public static void SetChunkDirty(Vector2Int chunkLoc)
    {
        if (ChunkDataDict.TryGetValue(chunkLoc, out ChunkData value))
        {
            value.Dirty = true;
        }
    }

    public void RenderMeshes()
    {
        foreach (KeyValuePair<Vector2Int, ChunkData> ChunkMeshPairs in ChunkDataDict)
        {
            if (ChunkMeshPairs.Value.Dirty)
            {
                StartCoroutine(UpdateChunkMesh(ChunkMeshPairs.Key));
            }

            if (Vector2Int.Distance(ChunkMeshPairs.Key, WorldUtils.GetChunkLocation(WorldGeneration.PlayerTransform.position)) > WorldGeneration.ChunkGenerationRange + 1) { continue; }

            foreach (KeyValuePair<Material, Mesh> meshData in ChunkMeshPairs.Value.MeshData.meshDataDict)
            {
                RenderParams renderParams = new(meshData.Key);

                Vector3 renderPos = Vector3.zero;

                Graphics.RenderMesh(renderParams, meshData.Value, 0, Matrix4x4.Translate(renderPos));
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
