using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[Serializable]
public class FeatureGeneration
{
    [Header("Grass")]
    [SerializeField] List<EntityTileData> grass;
    [SerializeField] SO_FastNoiseLiteGenerator grassPatchGenerator;
    [SerializeField] float grassPatchThreshold;

    [Header("Trees")]
    [SerializeField] List<EntityTileData> trees;
    [SerializeField] SO_FastNoiseLiteGenerator treeRegionGenerator;
    [SerializeField] SO_FastNoiseLiteGenerator treeSparsenessGenerator;
    [SerializeField] float treeRegionThreshold;
    [Space]
    [SerializeField] float treeFrequency;
    [SerializeField] float treeRandomFrequency;

    [Header("Rocks & Bushes")]
    [SerializeField] List<EntityTileData> rocksAndBushes;
    [SerializeField] SO_FastNoiseLiteGenerator rockScatterer;
    [SerializeField] float rockAndBushFrequency;

    public void GenerateFeatures(Chunk chunk)
    {
        System.Random random = new System.Random(Thread.CurrentThread.ManagedThreadId);

        GenerateRocks(chunk, random);
        GenerateTrees(chunk, random);
        GenerateGrass(chunk, random);
    }

    public void GenerateGrass(Chunk chunk, System.Random random)
    {
        for (int x = 0; x < chunk.Tiles.GetLength(0); x++)
        {
            for (int z = 0; z < chunk.Tiles.GetLength(2); z++)
            {
                if (chunk.Tiles[x, 0, z] == null) continue;

                Vector2Int sampleLoc = new Vector2Int(x + (chunk.ChunkLocation.x * WorldGeneration.CHUNK_SIZE), z + (chunk.ChunkLocation.y * WorldGeneration.CHUNK_SIZE));

                float sample = grassPatchGenerator.GetNoiseClamped(sampleLoc);

                if (sample < grassPatchThreshold) continue;

                Vector3Int topTileLoc = WorldUtils.GetTopTileLocation(new(sampleLoc.x, 0, sampleLoc.y));

                if (!WorldUtils.TryGetTile(topTileLoc, out Tile tile)) continue;

                BlockData blockData = GetBlockData(tile);
                if (blockData == null) continue;

                topTileLoc.y++;

                chunk.SetTile(grass[random.Next(0, grass.Count)], topTileLoc);
            }
        }
    }

    public void GenerateTrees(Chunk chunk, System.Random random)
    {
        for (int x = 0; x < chunk.Tiles.GetLength(0); x++)
        {
            for (int z = 0; z < chunk.Tiles.GetLength(2); z++)
            {
                if (chunk.Tiles[x, 0, z] == null) continue;

                Vector2Int sampleLoc = new Vector2Int(x + (chunk.ChunkLocation.x * WorldGeneration.CHUNK_SIZE), z + (chunk.ChunkLocation.y * WorldGeneration.CHUNK_SIZE));

                float sample = treeRegionGenerator.GetNoiseClamped(sampleLoc);

                if (sample > treeRegionThreshold)
                {
                    sample *= Mathf.Clamp01(treeSparsenessGenerator.GetNoiseClamped(sampleLoc));
                    if (sample < 1 - treeFrequency) continue;
                }
                else
                {
                    if (treeSparsenessGenerator.GetNoiseClamped(sampleLoc) < 1 - treeRandomFrequency) continue;
                }

                Vector3Int topTileLoc = WorldUtils.GetTopTileLocation(new(sampleLoc.x, 0, sampleLoc.y));

                if (!WorldUtils.TryGetTile(topTileLoc, out Tile tile)) continue;

                BlockData blockData = GetBlockData(tile);
                if (blockData == null) continue;

                topTileLoc.y++;

                chunk.SetTile(trees[random.Next(0, trees.Count)], topTileLoc);
            }
        }
    }

    public void GenerateRocks(Chunk chunk, System.Random random)
    {
        for (int x = 0; x < chunk.Tiles.GetLength(0); x++)
        {
            for (int z = 0; z < chunk.Tiles.GetLength(2); z++)
            {
                if (chunk.Tiles[x, 0, z] == null) continue;

                Vector2Int sampleLoc = new Vector2Int(x + (chunk.ChunkLocation.x * WorldGeneration.CHUNK_SIZE), z + (chunk.ChunkLocation.y * WorldGeneration.CHUNK_SIZE));
                if (rockScatterer.GetNoiseClamped(sampleLoc) < 1 - rockAndBushFrequency) continue;

                Vector3Int topTileLoc = WorldUtils.GetTopTileLocation(new(sampleLoc.x, 0, sampleLoc.y));

                if (!WorldUtils.TryGetTile(topTileLoc, out Tile tile)) continue;

                BlockData blockData = GetBlockData(tile);
                if (blockData == null) continue;

                topTileLoc.y++;

                chunk.SetTile(rocksAndBushes[random.Next(0, rocksAndBushes.Count)], topTileLoc);
            }
        }
    }

    public BlockData GetBlockData(Tile tile)
    {
        BlockData blockData = tile.tileData is not IBlockData iblockData ? null : iblockData.GetBlockData();

        if (blockData == null) return null;
        if (blockData.Fullness != TileFullness.Full) return null;

        return blockData;
    }
}
