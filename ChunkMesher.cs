using System.Buffers;
using System.Numerics;

namespace SilkWebGpuPbr;

public readonly struct ChunkMeshData : IDisposable
{
    public readonly VoxelVertex[] Vertices;
    public readonly uint[] Indices;
    public readonly int VertexCount;
    public readonly int IndexCount;

    public ChunkMeshData(VoxelVertex[] vertices, uint[] indices, int vertexCount, int indexCount)
    {
        Vertices = vertices;
        Indices = indices;
        VertexCount = vertexCount;
        IndexCount = indexCount;
    }

    public void Dispose()
    {
        if (Vertices != null)
        {
            ArrayPool<VoxelVertex>.Shared.Return(Vertices);
        }

        if (Indices != null)
        {
            ArrayPool<uint>.Shared.Return(Indices);
        }
    }
}

public static class ChunkMesher
{
    private static readonly Vector3[] VoxelVertices =
    [
        new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0), // Front
        new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1)  // Back
    ];

    private static readonly int[][] FaceIndices =
    [
        [2, 1, 0, 0, 3, 2], // Front (-Z)
        [6, 7, 4, 4, 5, 6], // Back (+Z)
        [3, 0, 4, 4, 7, 3], // Left (-X)
        [1, 2, 6, 6, 5, 1], // Right (+X)
        [2, 3, 7, 7, 6, 2], // Top (+Y)
        [0, 1, 5, 5, 4, 0]  // Bottom (-Y)
    ];

    private static readonly Vector3[] FaceNormals =
    [
        new Vector3(0, 0, -1),
        new Vector3(0, 0, 1),
        new Vector3(-1, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(0, -1, 0)
    ];

    private static readonly int[][] FaceVertices =
    [
        [0, 1, 2, 3], // Front (-Z)
        [5, 4, 7, 6], // Back (+Z)
        [4, 0, 3, 7], // Left (-X)
        [1, 5, 6, 2], // Right (+X)
        [3, 2, 6, 7], // Top (+Y)
        [4, 5, 1, 0]  // Bottom (-Y)
    ];

    private static readonly (int x, int y, int z)[] FaceOffsets =
    [
        (0, 0, -1), // Front
        (0, 0, 1),  // Back
        (-1, 0, 0), // Left
        (1, 0, 0),  // Right
        (0, 1, 0),  // Top
        (0, -1, 0)  // Bottom
    ];

    public static ChunkMeshData CreateMesh(Chunk chunk)
    {
        int capacityFaces = 4096;
        VoxelVertex[] vertices = ArrayPool<VoxelVertex>.Shared.Rent(capacityFaces * 4);
        uint[] indices = ArrayPool<uint>.Shared.Rent(capacityFaces * 6);

        int vertexCount = 0;
        int indexCount = 0;

        for (int x = 0; x < Chunk.Width; x++)
        {
            for (int y = 0; y < Chunk.Height; y++)
            {
                for (int z = 0; z < Chunk.Depth; z++)
                {
                    BlockType block = chunk.GetBlock(x, y, z);
                    if (block == BlockType.Air)
                    {
                        continue;
                    }

                    Vector4 color = GetColorForBlock(block);
                    Vector3 positionOffset = new Vector3(x, y, z);

                    for (int f = 0; f < 6; f++)
                    {
                        var (ox, oy, oz) = FaceOffsets[f];
                        BlockType neighbor = chunk.GetBlock(x + ox, y + oy, z + oz);

                        if (neighbor == BlockType.Air)
                        {
                            if (vertexCount + 4 > vertices.Length)
                            {
                                var newVertices = ArrayPool<VoxelVertex>.Shared.Rent(vertices.Length * 2);
                                Array.Copy(vertices, newVertices, vertexCount);
                                ArrayPool<VoxelVertex>.Shared.Return(vertices);
                                vertices = newVertices;
                            }
                            if (indexCount + 6 > indices.Length)
                            {
                                var newIndices = ArrayPool<uint>.Shared.Rent(indices.Length * 2);
                                Array.Copy(indices, newIndices, indexCount);
                                ArrayPool<uint>.Shared.Return(indices);
                                indices = newIndices;
                            }

                            Vector3 normal = FaceNormals[f];

                            for (int v = 0; v < 4; v++)
                            {
                                int cornerIndex = FaceVertices[f][v];
                                Vector3 localPos = VoxelVertices[cornerIndex];
                                vertices[vertexCount + v] = new VoxelVertex(positionOffset + localPos, normal, color);
                            }

                            indices[indexCount + 0] = (uint)(vertexCount + 0);
                            indices[indexCount + 1] = (uint)(vertexCount + 1);
                            indices[indexCount + 2] = (uint)(vertexCount + 2);
                            indices[indexCount + 3] = (uint)(vertexCount + 2);
                            indices[indexCount + 4] = (uint)(vertexCount + 3);
                            indices[indexCount + 5] = (uint)(vertexCount + 0);

                            vertexCount += 4;
                            indexCount += 6;
                        }
                    }
                }
            }
        }

        return new ChunkMeshData(vertices, indices, vertexCount, indexCount);
    }

    private static Vector4 GetColorForBlock(BlockType block)
    {
        return block switch
        {
            BlockType.Dirt => new Vector4(0.5f, 0.3f, 0.1f, 1.0f),
            BlockType.Grass => new Vector4(0.2f, 0.8f, 0.2f, 1.0f),
            BlockType.Stone => new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
            _ => new Vector4(1, 0, 1, 1)
        };
    }
}
