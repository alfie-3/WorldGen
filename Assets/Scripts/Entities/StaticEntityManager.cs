using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StaticEntityManager : MonoBehaviour
{
    public HashSet<EntityContainer> entities = new HashSet<EntityContainer>();

    public ConcurrentQueue<StaticEntityTileData> EntityCreationBuffer = new();
    public ConcurrentQueue<Vector3Int> EntityRemovalBuffer = new();

    public static Dictionary<Vector3Int, TrackedStaticEntity> TrackedStaticEntityLocations = new();

    private void OnEnable()
    {
        Chunk.OnTileUpdate += OnTileUpdated;
    }

    private void FixedUpdate()
    {
        if (EntityRemovalBuffer.Count > 0)
        {
            if (EntityRemovalBuffer.TryDequeue(out var removed))
            {
                TrackedStaticEntityLocations[removed].OnRemove();
                TrackedStaticEntityLocations.Remove(removed);
            }
        }

        while (EntityCreationBuffer.Count > 0)
        {
            if (EntityCreationBuffer.TryDequeue(out StaticEntityTileData result))
                TrackTileEntity(result);
        }
    }

    public void OnTileUpdated(Vector2Int chunkCoord, TileInfo prev, TileInfo current)
    {
        Vector3Int globalTileLoc = WorldUtils.TileCoordinateLocalToGlobal(prev.TileLocation, chunkCoord);

        if (TrackedStaticEntityLocations.ContainsKey(globalTileLoc))
        {
            if (current.TileData == null)
            {
                EntityRemovalBuffer.Enqueue(globalTileLoc);
            }

            return;
        }

        if (current.TileData == null) return;

        BufferEntity(chunkCoord, current);
    }

    public void BufferEntity(Vector2Int chunkCoord, TileInfo tileInfo)
    {
        EntityData entityData = tileInfo.TileData is not IEntityData iEntityData ? null : iEntityData.GetEntityData();

        if (entityData == null) return;

        StaticEntity staticEntityData = entityData is not StaticEntity ? null : entityData as StaticEntity;

        if (staticEntityData == null) return;

        StaticEntityTileData info = new StaticEntityTileData(chunkCoord, staticEntityData, tileInfo);
        EntityCreationBuffer.Enqueue(info);
    }

    public void TrackTileEntity(StaticEntityTileData creationData)
    {
        if (WorldUtils.TryGetChunk(creationData.chunkCoord, out Chunk chunk))
        {
            Vector3Int globalTileLocation = WorldUtils.TileCoordinateLocalToGlobal(creationData.tileInfo.TileLocation, chunk.ChunkLocation);

            TrackedStaticEntityLocations.TryAdd(globalTileLocation, new(chunk, this, creationData));
        }
    }
}

public class TrackedStaticEntity
{
    EntityContainer container;
    readonly StaticEntityManager entityManager;
    readonly StaticEntity entityData;

    readonly Vector3Int globalLocation;
    readonly byte rotation;

    public TrackedStaticEntity(Chunk chunk, StaticEntityManager manager, StaticEntityTileData entityInfo)
    {
        chunk.OnUpdatedChunkStatus += OnUpdatedChunkStatus;
        this.entityData = entityInfo.staticEntityData;
        entityManager = manager;

        rotation = entityInfo.tileInfo.rotation;
        globalLocation = WorldUtils.TileCoordinateLocalToGlobal(entityInfo.tileInfo.TileLocation, chunk.ChunkLocation);

        if (chunk.ChunkStatus is Chunk.CHUNK_STATUS.GENERATED)
            PopulateStaticEntityTile();
    }

    public void PopulateStaticEntityTile()
    {
        if (container) return;

        container = entityData.EntityPool.Pool.Get();
        container.transform.SetPositionAndRotation(globalLocation, Tile.GetRotation(rotation));
        container.transform.parent = entityManager.transform;
    }

    public void OnUpdatedChunkStatus(Chunk.CHUNK_STATUS chunkStatus)
    {
        if (chunkStatus is Chunk.CHUNK_STATUS.SLEEPING)
        {
            if (container != null)
            {
                entityData.EntityPool.Pool.Release(container);
                container = null;
            }
        }

        if (chunkStatus is Chunk.CHUNK_STATUS.GENERATED)
        {
            PopulateStaticEntityTile();
        }
    }

    public void OnRemove()
    {
        if (container != null)
            entityData.EntityPool.Pool.Release(container);
    }
}

public struct StaticEntityTileData
{
    public Vector2Int chunkCoord;
    public TileInfo tileInfo;

    public StaticEntity staticEntityData;

    public StaticEntityTileData(Vector2Int chunkCoord, StaticEntity staticEntity, TileInfo tileInfo)
    {
        this.chunkCoord = chunkCoord;
        this.staticEntityData = staticEntity;
        this.tileInfo = tileInfo;
    }
}