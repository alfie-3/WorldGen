using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WorldMeshBuilder : MonoBehaviour {
    public class ChunkData {
        public Coroutine generationRoutine;

        public ConcurrentDictionary<Material, MaterialMeshData> MaterialMeshes = new();

        public MeshCollider Collider;
        public Mesh ColliderMesh = new();

        public class MaterialMeshData {
            public Mesh mesh;
            public bool dirty;

            public TileMeshInfo allocatedTileMeshInfo = new();
            public TileMeshInfo[,,] TileMeshInfos = new TileMeshInfo[WorldGeneration.CHUNK_SIZE, WorldGeneration.MaxTerrainHeight, WorldGeneration.CHUNK_SIZE];
            public CombineInstance[] cachedCombineInstances = new CombineInstance[0];
        }
    }

    [SerializeField] Material terrainMaterial;

    static ConcurrentDictionary<Vector2Int, ChunkData> ChunkDataDict = new();

    public static Queue<Vector2Int> chunkGenerationBuffer = new Queue<Vector2Int>();
    public static ConcurrentDictionary<Vector2Int, byte> dirtyChunkBuffer = new ConcurrentDictionary<Vector2Int, byte>();

    System.Random randomPicker = new System.Random();

    private void OnEnable() {
        WorldGeneration.ChunkReady += (value) => chunkGenerationBuffer.Enqueue(value);
        WorldGeneration.ChunkReleased += ReleaseChunk;

        Chunk.OnTileUpdate += UpdateTile;
    }

    private void FixedUpdate() {
        if (chunkGenerationBuffer.Any())
            AddChunk(chunkGenerationBuffer.Dequeue());

        //Buffers out chunk updates across multiple frames to prevent stuterring
        if (dirtyChunkBuffer.Any()) {
            KeyValuePair<Vector2Int, byte> randomHashet = dirtyChunkBuffer.ElementAt(randomPicker.Next(dirtyChunkBuffer.Count));
            if (dirtyChunkBuffer.Contains(randomHashet)) {
                if (ChunkDataDict.ContainsKey(randomHashet.Key))
                    StartChunkMeshGeneration(randomHashet.Key);

                dirtyChunkBuffer.Remove(randomHashet.Key, out _);
            }
        }
    }

    private void LateUpdate() {
        RenderMeshes();
    }

    public void AddChunk(Vector2Int coord) {
        ChunkDataDict.TryAdd(coord, new ChunkData());

        if (WorldUtils.TryGetChunk(coord, out Chunk chunk)) {
            foreach (Tile tile in chunk.Tiles) {
                if (tile == null) continue;
                if (tile.DontDraw) continue;

                AddTileData(coord, new(tile));
            }
        }

        StartChunkMeshGeneration(coord);
    }

    public void StartChunkMeshGeneration(Vector2Int coord) {
        UpdateChunkMesh(coord);
    }

    public void UpdateChunkMesh(Vector2Int chunkLoc) {
        GenerateChunkMeshData(chunkLoc);
        UpdateCollider(chunkLoc);

    }

    public void ReleaseChunk(Vector2Int chunkLoc) {
        if (ChunkDataDict.TryRemove(chunkLoc, out ChunkData chunkMeshData)) {
            Destroy(chunkMeshData.Collider);
        }
    }

    public void GenerateChunkMeshData(Vector2Int chunkPos) {
        foreach (KeyValuePair<Material, ChunkData.MaterialMeshData> materialMeshData in ChunkDataDict[chunkPos].MaterialMeshes) {
            if (!materialMeshData.Value.dirty) continue;
            materialMeshData.Value.dirty = false;

            CacheCombinerInstances(materialMeshData.Value);

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(materialMeshData.Value.cachedCombineInstances, true, true);
            mesh.Optimize();
            materialMeshData.Value.mesh = mesh;
        }
    }

    public void CacheCombinerInstances(ChunkData.MaterialMeshData materialMeshData) {
        int arrayLength = 0;
        foreach (TileMeshInfo meshInfo in materialMeshData.TileMeshInfos) {
            if (meshInfo.Transform != Matrix4x4.zero) {
                arrayLength++;
            }
        }

        if (materialMeshData.cachedCombineInstances.Length != arrayLength)
            materialMeshData.cachedCombineInstances = new CombineInstance[arrayLength];

        int i = 0;
        foreach (TileMeshInfo meshInfo in materialMeshData.TileMeshInfos) {
            if (meshInfo.Transform == Matrix4x4.zero) continue;
            if (i > materialMeshData.cachedCombineInstances.Length - 1) continue;

            materialMeshData.cachedCombineInstances[i].mesh = meshInfo.Mesh;
            materialMeshData.cachedCombineInstances[i].transform = meshInfo.Transform;
            i++;
        }


        if (arrayLength > i) {
            int remainder = arrayLength - i;

            for (int j = i; j < i + remainder; j++) {
                materialMeshData.cachedCombineInstances[j].transform = Matrix4x4.zero;
                materialMeshData.cachedCombineInstances[j].mesh = new();
            }
        }
    }

    public void UpdateCollider(Vector2Int chunkLoc) {
        if (ChunkDataDict[chunkLoc].Collider != null) {
            ChunkDataDict[chunkLoc].Collider.sharedMesh = ChunkDataDict[chunkLoc].ColliderMesh;
        }
        else {
            MeshCollider collider = gameObject.AddComponent<MeshCollider>();

            ChunkDataDict[chunkLoc].Collider = collider;
            ChunkDataDict[chunkLoc].Collider.sharedMesh = ChunkDataDict[chunkLoc].ColliderMesh;
        }
    }

    public void UpdateTile(Vector2Int chunkCoord, TileInfo previousData, TileInfo currentData) {
        if (!ChunkDataDict.ContainsKey(chunkCoord)) return;

        RemoveTileData(chunkCoord, previousData);
        AddTileData(chunkCoord, currentData);

        dirtyChunkBuffer.TryAdd(chunkCoord, 0);
    }

    public void RemoveTileData(Vector2Int chunkCoord, TileInfo tile) {
        if (tile.tiledata == null) return;

        if (ChunkDataDict[chunkCoord].MaterialMeshes.TryGetValue(tile.tiledata.TileMaterials[0], out ChunkData.MaterialMeshData chunkMatData)) {
            chunkMatData.TileMeshInfos[tile.tileLocation.x, tile.tileLocation.y, tile.tileLocation.z] = TileMeshInfo.Empty;
            chunkMatData.dirty = true;
        }
    }

    public void AddTileData(Vector2Int chunkCoord, TileInfo tile) {
        if (tile.tiledata == null) return;


        if (!ChunkDataDict[chunkCoord].MaterialMeshes.TryGetValue(tile.tiledata.TileMaterials[0], out ChunkData.MaterialMeshData chunkMaterialData)) {
            ChunkDataDict[chunkCoord].MaterialMeshes.TryAdd(tile.tiledata.TileMaterials[0], new());
            chunkMaterialData = ChunkDataDict[chunkCoord].MaterialMeshes[tile.tiledata.TileMaterials[0]];
        }

        chunkMaterialData.allocatedTileMeshInfo.Mesh = tile.tiledata.TileMesh;
        chunkMaterialData.allocatedTileMeshInfo.Transform = tile.tileTransform;

        chunkMaterialData.TileMeshInfos[tile.tileLocation.x, tile.tileLocation.y, tile.tileLocation.z] = chunkMaterialData.allocatedTileMeshInfo;

        chunkMaterialData.dirty = true;
    }



    public void SetChunkDirty(Vector2Int chunkLoc) {
        dirtyChunkBuffer.TryAdd(chunkLoc, 0);
    }

    public void RenderMeshes() {
        foreach (KeyValuePair<Vector2Int, ChunkData> ChunkMeshPair in ChunkDataDict) {
            if (Vector2Int.Distance(ChunkMeshPair.Key, WorldUtils.GetChunkLocation(WorldGeneration.PlayerTransform.position)) > WorldGeneration.ChunkGenerationRange + 1) { continue; }

            foreach (KeyValuePair<Material, ChunkData.MaterialMeshData> materialMeshData in ChunkMeshPair.Value.MaterialMeshes) {
                if (materialMeshData.Value.mesh == null) continue;

                RenderParams renderParams = new(materialMeshData.Key);
                Matrix4x4 renderPos = Matrix4x4.Translate(Vector3.zero);
                Graphics.RenderMesh(renderParams, materialMeshData.Value.mesh, 0, renderPos);
            }
        }
    }

    public struct TileMeshInfo {
        public Matrix4x4 Transform;
        public Mesh Mesh;

        public static readonly TileMeshInfo Empty = new(true);
        public bool IsEmpty;

        public TileMeshInfo(Matrix4x4 transform, Mesh mesh) {
            this.Transform = transform;
            this.Mesh = mesh;
            IsEmpty = false;
        }

        public TileMeshInfo(bool IsEmpty) {
            this.IsEmpty = true;
            Transform = Matrix4x4.zero;
            Mesh = null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        if (WorldGeneration.ChunkDict == null) return;

        foreach (KeyValuePair<Vector2Int, Chunk> chunk in WorldGeneration.ChunkDict) {
            Gizmos.matrix = Matrix4x4.Translate(new(WorldGeneration.CHUNK_SIZE / 2, 0, WorldGeneration.CHUNK_SIZE / 2));

            switch (chunk.Value.ChunkStatus) {
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