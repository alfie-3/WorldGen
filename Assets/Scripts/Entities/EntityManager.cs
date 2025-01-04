using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public HashSet<EntityContainer> entities = new HashSet<EntityContainer>();

    public ConcurrentQueue<TileEntityCreationInfo> EntityCreationBuffer = new();

    private void OnEnable()
    {
        Chunk.OnTileUpdate += BufferTileEntityCreation;
    }

    private void FixedUpdate()
    {
        while (EntityCreationBuffer.Count > 0)
        {
            if (EntityCreationBuffer.TryDequeue(out TileEntityCreationInfo result))
                CreateTileEntity(result);
        }
    }

    public void BufferTileEntityCreation(Vector2Int chunkCoord, TileInfo prev, TileInfo current)
    {
        EntityData entityData = current.TileData is not IEntityData iEntityData ? null : iEntityData.GetEntityData();

        if (entityData == null) return;

        StaticEntity staticEntityData = entityData is not StaticEntity ? null : entityData as StaticEntity;

        if (staticEntityData == null) return;

        EntityCreationBuffer.Enqueue(new TileEntityCreationInfo(chunkCoord, staticEntityData, current));
    }

    public void CreateTileEntity(TileEntityCreationInfo creationData)
    {
        Vector3Int chunkOffset = new(creationData.chunkCoord.x * WorldGeneration.CHUNK_SIZE, 0, creationData.chunkCoord.y * WorldGeneration.CHUNK_SIZE);

        InstantiateAsync(creationData.staticEntityData.EntityPrefab.gameObject,
            creationData.tileInfo.TileLocation + creationData.staticEntityData.EntityPrefab.transform.position + chunkOffset,
            Tile.GetRotation(creationData.tileInfo.rotation));

    }
}

public struct TileEntityCreationInfo
{
    public Vector2Int chunkCoord;
    public StaticEntity staticEntityData;
    public TileInfo tileInfo;

    public TileEntityCreationInfo(Vector2Int chunkCoord, StaticEntity staticEntity, TileInfo tileInfo)
    {
        this.chunkCoord = chunkCoord;
        this.staticEntityData = staticEntity;
        this.tileInfo = tileInfo;
    }
}