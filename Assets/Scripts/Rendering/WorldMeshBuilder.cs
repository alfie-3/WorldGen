using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WorldMeshBuilder : MonoBehaviour
{
    public class ChunkData
    {
        public ChunkMeshData MeshData = new();
        public MeshCollider Collider;

        public Coroutine generationRoutine;

        public class ChunkMeshData
        {
            public Mesh mesh;
            public CombineInstance[] combineInstances = new CombineInstance[0];

            public Mesh ColliderMesh = new();
        }
    }

    [SerializeField] Material terrainMaterial;

    static ConcurrentDictionary<Vector2Int, ChunkData> ChunkDataDict = new();

    public static Queue<Vector2Int> chunkGenerationBuffer = new Queue<Vector2Int>();
    public static ConcurrentDictionary<Vector2Int, byte> dirtyChunkBuffer = new ConcurrentDictionary<Vector2Int, byte>();
    System.Random randomPicker = new System.Random();

    private void OnEnable()
    {
        WorldGeneration.ChunkReady += (value) => chunkGenerationBuffer.Enqueue(value);
        WorldGeneration.ChunkReleased += ReleaseChunk;
        Chunk.RefreshChunk += (coord) => { SetChunkDirty(coord); };
    }

    private void LateUpdate()
    {
        if (chunkGenerationBuffer.Any())
            StartChunkMeshGeneration(chunkGenerationBuffer.Dequeue());

        //Buffers out chunk updates across multiple frames to prevent stuterring
        if (dirtyChunkBuffer.Any())
        {
            KeyValuePair<Vector2Int, byte> randomHashet = dirtyChunkBuffer.ElementAt(randomPicker.Next(dirtyChunkBuffer.Count));
            if (dirtyChunkBuffer.Contains(randomHashet))
            {
                if (ChunkDataDict.ContainsKey(randomHashet.Key))
                    StartChunkMeshGeneration(randomHashet.Key);

                dirtyChunkBuffer.Remove(randomHashet.Key, out _);
            }
        }

        RenderMeshes();
    }

    public void StartChunkMeshGeneration(Vector2Int coord)
    {
        ChunkDataDict.TryAdd(coord, new ChunkData());
        UpdateChunkMesh(coord);
    }

    public void UpdateChunkMesh(Vector2Int chunkLoc)
    {
        GenerateChunkMeshData(chunkLoc);
        UpdateCollider(chunkLoc);

    }

    public void ReleaseChunk(Vector2Int chunkLoc)
    {
        if (ChunkDataDict.TryRemove(chunkLoc, out ChunkData chunkMeshData))
        {
            Destroy(chunkMeshData.Collider);
        }
    }

    public void GenerateChunkMeshData(Vector2Int chunkPos)
    {
        int validTiles = WorldUtils.CountValidTiles(chunkPos);
        if (ChunkDataDict[chunkPos].MeshData.combineInstances.Length != validTiles)
            ChunkDataDict[chunkPos].MeshData.combineInstances = new CombineInstance[validTiles];

        int i = 0;

        if (WorldUtils.GetChunk(chunkPos, out Chunk chunk))
        {
            foreach (Tile tile in chunk.Tiles)
            {
                if (tile == null) continue;
                if (tile.tileData == null) continue;

                ChunkDataDict[chunkPos].MeshData.combineInstances[i].transform = tile.tileTransform;
                ChunkDataDict[chunkPos].MeshData.combineInstances[i].mesh = tile.tileData.TileMesh;

                i++;
            }
        }

        if (validTiles > i)
        {
            int remainder = validTiles - i;

            for (int j = 0; j < i + remainder; j++)
            {
                ChunkDataDict[chunkPos].MeshData.combineInstances[j].transform = Matrix4x4.zero;
                ChunkDataDict[chunkPos].MeshData.combineInstances[j].mesh = new();
            }
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(ChunkDataDict[chunkPos].MeshData.combineInstances, true, true);
        mesh.Optimize();
        ChunkDataDict[chunkPos].MeshData.mesh = mesh;
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

    public void SetChunkDirty(Vector2Int chunkLoc)
    {
        dirtyChunkBuffer.TryAdd(chunkLoc, 0);
    }

    public void RenderMeshes()
    {
        foreach (KeyValuePair<Vector2Int, ChunkData> ChunkMeshPairs in ChunkDataDict)
        {
            if (Vector2Int.Distance(ChunkMeshPairs.Key, WorldUtils.GetChunkLocation(WorldGeneration.PlayerTransform.position)) > WorldGeneration.ChunkGenerationRange + 1) { continue; }

            RenderParams renderParams = new(terrainMaterial);

            Vector3 renderPos = Vector3.zero;

            Graphics.RenderMesh(renderParams, ChunkMeshPairs.Value.MeshData.mesh, 0, Matrix4x4.Translate(renderPos));
        }
    }

#if UNITY_EDITOR
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
#endif
}