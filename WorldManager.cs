using System.Collections.Concurrent;
using System.Numerics;
using Silk.NET.WebGPU;

namespace SilkWebGpuPbr;

public unsafe class WorldManager : IDisposable
{
    private readonly WebGPU _webGpu;
    private readonly Device* _device;
    private readonly Queue* _queue;

    private readonly Dictionary<Vector3, Chunk> _chunks = new();
    private readonly Dictionary<Vector3, DynamicGpuMesh> _chunkMeshes = new();

    private readonly ConcurrentQueue<(Vector3 Position, ChunkMeshData MeshData)> _meshingQueue = new();

    public WorldManager(WebGPU webGpu, Device* device, Queue* queue)
    {
        _webGpu = webGpu;
        _device = device;
        _queue = queue;
    }

    public void GenerateWorld(int sizeX, int sizeY, int sizeZ)
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    Chunk chunk = new Chunk(pos);
                    GenerateChunkData(chunk);
                    _chunks[pos] = chunk;

                    Task.Run(() => MeshChunkAsync(pos, chunk));
                }
            }
        }
    }

    private void GenerateChunkData(Chunk chunk)
    {
        for (int x = 0; x < Chunk.Width; x++)
        {
            for (int z = 0; z < Chunk.Depth; z++)
            {
                float worldX = chunk.Position.X * Chunk.Width + x;
                float worldZ = chunk.Position.Z * Chunk.Depth + z;

                int height = (int)(MathF.Sin(worldX * 0.1f) * 4 + MathF.Cos(worldZ * 0.1f) * 4 + 10);

                for (int y = 0; y < Chunk.Height; y++)
                {
                    float worldY = chunk.Position.Y * Chunk.Height + y;
                    if (worldY < height)
                    {
                        if (worldY < height - 3)
                            chunk.SetBlock(x, y, z, BlockType.Stone);
                        else if (worldY < height - 1)
                            chunk.SetBlock(x, y, z, BlockType.Dirt);
                        else
                            chunk.SetBlock(x, y, z, BlockType.Grass);
                    }
                }
            }
        }
    }

    private void MeshChunkAsync(Vector3 position, Chunk chunk)
    {
        ChunkMeshData meshData = ChunkMesher.CreateMesh(chunk);
        _meshingQueue.Enqueue((position, meshData));
    }

    public void Update()
    {
        while (_meshingQueue.TryDequeue(out var result))
        {
            if (!_chunkMeshes.TryGetValue(result.Position, out DynamicGpuMesh? mesh))
            {
                mesh = new DynamicGpuMesh(_webGpu, _device, _queue, $"Chunk {result.Position}");
                _chunkMeshes[result.Position] = mesh;
            }

            mesh.Update(result.MeshData);
            result.MeshData.Dispose();
        }
    }

    public void Draw(RenderPassEncoder* pass, ChunkRenderer renderer)
    {
        foreach (var mesh in _chunkMeshes.Values)
        {
            renderer.Draw(pass, mesh);
        }
    }

    public void Dispose()
    {
        foreach (var mesh in _chunkMeshes.Values)
        {
            mesh.Dispose();
        }
        _chunkMeshes.Clear();
    }
}
