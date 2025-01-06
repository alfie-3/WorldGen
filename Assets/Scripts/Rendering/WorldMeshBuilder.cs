using System.Collections;
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
        WorldGeneration.OnChunkReady += (value) => chunkGenerationBuffer.Enqueue(value);
        WorldGeneration.OnChunkReleased += ReleaseChunk;

        Chunk.OnTileUpdate += UpdateTile;
    }

    private void Update() {
        //Buffers out chunk updates across multiple frames to prevent stuterring
        if (dirtyChunkBuffer.Any())
        {
            KeyValuePair<Vector2Int, byte> randomHashet = dirtyChunkBuffer.ElementAt(randomPicker.Next(dirtyChunkBuffer.Count));
            if (dirtyChunkBuffer.Contains(randomHashet))
            {
                if (ChunkDataDict.ContainsKey(randomHashet.Key))
                    StartUpdateChunk(randomHashet.Key);

                dirtyChunkBuffer.Remove(randomHashet.Key, out _);
            }
        }

        if (chunkGenerationBuffer.Any())
            StartAddChunk(chunkGenerationBuffer.Dequeue());
    }

    private void LateUpdate() {
        RenderMeshes();
    }

    public void StartAddChunk(Vector2Int chunkLoc)
    {
        ChunkDataDict.TryAdd(chunkLoc, new ChunkData());
        StartCoroutine(AddChunk(chunkLoc));
    }

    public void StartUpdateChunk(Vector2Int chunkLoc)
    {
        if (ChunkDataDict.TryGetValue(chunkLoc, out ChunkData chunkData))
        {
            if (chunkData.generationRoutine != null)
                StopCoroutine(chunkData.generationRoutine);

            chunkData.generationRoutine = StartCoroutine(UpdateChunkMesh(chunkLoc));
        }
    }

    //Adds a new chunk to the tile dictionary and builds the tilset from the current tiles
    public IEnumerator AddChunk(Vector2Int coord) {
        if (WorldUtils.TryGetChunk(coord, out Chunk chunk)) {
            foreach (Tile tile in chunk.Tiles) {
                if (tile == null) continue;
                if (tile.DontDraw) continue;

                AddTileData(coord, new(tile));
            }
        }

        ChunkDataDict[coord].generationRoutine = StartCoroutine(UpdateChunkMesh(coord));

        yield return null;
    }

    //Updates chunk mesh at given coordinate
    public IEnumerator UpdateChunkMesh(Vector2Int chunkLoc) {
        yield return GenerateChunkMeshData(chunkLoc);
        //UpdateCollider(chunkLoc);

        yield return null;
    }

    //Releases chunk from rendering memory
    public void ReleaseChunk(Vector2Int chunkLoc) {
        if (ChunkDataDict.TryRemove(chunkLoc, out ChunkData chunkMeshData)) {
            Destroy(chunkMeshData.Collider);
        }
    }

    //Generates a mesh per tile material by combining all the tiles using that material
    public IEnumerator GenerateChunkMeshData(Vector2Int chunkPos) {
        foreach (KeyValuePair<Material, ChunkData.MaterialMeshData> materialMeshData in ChunkDataDict[chunkPos].MaterialMeshes) {
            if (!materialMeshData.Value.dirty) continue;
            materialMeshData.Value.dirty = false;

            yield return CacheCombinerInstances(materialMeshData.Value);

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(materialMeshData.Value.cachedCombineInstances, true, true);
            mesh.Optimize();
            mesh.RecalculateNormals();
            materialMeshData.Value.mesh = mesh;
        }

        yield return null;
    }


    //Caches combine instances used by chunk mesh generator
    public IEnumerator CacheCombinerInstances(ChunkData.MaterialMeshData materialMeshData) {
        List<CombineInstance> combineInstances = new List<CombineInstance>();

        foreach (TileMeshInfo meshInfo in materialMeshData.TileMeshInfos) {
            if (meshInfo.Transform == Matrix4x4.zero) continue;

            combineInstances.Add(new CombineInstance()
            {
                mesh = meshInfo.Mesh,
                transform = meshInfo.Transform
            });
        }

        materialMeshData.cachedCombineInstances = combineInstances.ToArray();
        yield return null;
    }

    //Updates the colliders to the new collision mesh
    //Creates a MeshCollider component if non is present for the chunk
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

    //Updates the previous tile to the current tile, and removes the old one from its previous position
    public void UpdateTile(Vector2Int chunkCoord, TileInfo previousData, TileInfo currentData) {
        if (!ChunkDataDict.ContainsKey(chunkCoord)) return;

        RemoveTileData(chunkCoord, previousData);
        AddTileData(chunkCoord, currentData);

        dirtyChunkBuffer.TryAdd(chunkCoord, 0);
    }

    //Removes tile from address
    public void RemoveTileData(Vector2Int chunkCoord, TileInfo tile) {
        BlockData blockData = tile.TileData is not IBlockData iblockData ? null : iblockData.GetBlockData();
        if (blockData == null) return;

        if (ChunkDataDict[chunkCoord].MaterialMeshes.TryGetValue(blockData.TileMaterials[0], out ChunkData.MaterialMeshData chunkMatData)) {
            chunkMatData.TileMeshInfos[tile.TileLocation.x, tile.TileLocation.y, tile.TileLocation.z] = TileMeshInfo.Empty;
            chunkMatData.dirty = true;
        }
    }

    //Adds new tile to new address & creates the material in the dicionary if it isnt available
    public void AddTileData(Vector2Int chunkCoord, TileInfo tile) {
        BlockData blockData = tile.TileData is not IBlockData iblockData ? null : iblockData.GetBlockData();
        if (blockData == null) return;
      
        if (!ChunkDataDict[chunkCoord].MaterialMeshes.TryGetValue(blockData.TileMaterials[0], out ChunkData.MaterialMeshData chunkMaterialData)) {
            ChunkDataDict[chunkCoord].MaterialMeshes.TryAdd(blockData.TileMaterials[0], new());
            chunkMaterialData = ChunkDataDict[chunkCoord].MaterialMeshes[blockData.TileMaterials[0]];
        }

        chunkMaterialData.allocatedTileMeshInfo.Mesh = blockData.TileMesh;
        chunkMaterialData.allocatedTileMeshInfo.Transform = Matrix4x4.TRS(tile.TileLocation, Tile.GetRotation(tile.rotation), Vector3.one);

        chunkMaterialData.TileMeshInfos[tile.TileLocation.x, tile.TileLocation.y, tile.TileLocation.z] = chunkMaterialData.allocatedTileMeshInfo;

        chunkMaterialData.dirty = true;
    }

    //Sets chunk to dirty if it should be updated
    public void SetChunkDirty(Vector2Int chunkLoc) {
        dirtyChunkBuffer.TryAdd(chunkLoc, 0);
    }

    //Render each mesh for each chunk and each material
    public void RenderMeshes() {
        Vector2Int playerChunkLocation = WorldUtils.GetChunkLocation(Vector3Int.RoundToInt(WorldGeneration.PlayerTransform.position));

        foreach (KeyValuePair<Vector2Int, ChunkData> ChunkMeshPair in ChunkDataDict) {
            if (Vector2.Distance(playerChunkLocation, ChunkMeshPair.Key) > WorldGeneration.ChunkGenerationRange + 1) { continue; }

            foreach (KeyValuePair<Material, ChunkData.MaterialMeshData> materialMeshData in ChunkMeshPair.Value.MaterialMeshes) {
                if (materialMeshData.Value.mesh == null) continue;

                RenderParams renderParams = new(materialMeshData.Key)
                {
                    receiveShadows = true,
                    shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                };

                Matrix4x4 renderPos = Matrix4x4.Translate(new(ChunkMeshPair.Key.x * WorldGeneration.CHUNK_SIZE, 0, ChunkMeshPair.Key.y * WorldGeneration.CHUNK_SIZE));
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