using System.Runtime.CompilerServices;
using Silk.NET.WebGPU;

namespace SilkWebGpuPbr;

public unsafe class DynamicGpuMesh : IDisposable
{
    private readonly WebGPU _webGpu;
    private readonly Device* _device;
    private readonly Queue* _queue;
    private readonly string _label;

    private GpuBuffer? _vertexBuffer;
    private GpuBuffer? _indexBuffer;
    private int _vertexCapacity;
    private int _indexCapacity;

    public DynamicGpuMesh(WebGPU webGpu, Device* device, Queue* queue, string label)
    {
        _webGpu = webGpu;
        _device = device;
        _queue = queue;
        _label = label;
        IndexCount = 0;
    }

    public Silk.NET.WebGPU.Buffer* VertexBuffer => _vertexBuffer != null ? _vertexBuffer.Handle : null;
    public ulong VertexBufferSize => _vertexBuffer?.Size ?? 0;
    public Silk.NET.WebGPU.Buffer* IndexBuffer => _indexBuffer != null ? _indexBuffer.Handle : null;
    public ulong IndexBufferSize => _indexBuffer?.Size ?? 0;
    public int IndexCount { get; private set; }

    public void Update(ChunkMeshData meshData)
    {
        IndexCount = meshData.IndexCount;

        if (IndexCount == 0)
        {
            return;
        }

        if (meshData.VertexCount > _vertexCapacity || _vertexBuffer == null)
        {
            _vertexBuffer?.Dispose();
            _vertexCapacity = Math.Max(meshData.VertexCount, _vertexCapacity * 2);
            _vertexCapacity = Math.Max(_vertexCapacity, 1024);
            _vertexBuffer = new GpuBuffer(
                _webGpu, _device, _queue, $"{_label} Vertices",
                (ulong)(_vertexCapacity * Unsafe.SizeOf<VoxelVertex>()),
                BufferUsage.Vertex | BufferUsage.CopyDst);
        }

        if (meshData.IndexCount > _indexCapacity || _indexBuffer == null)
        {
            _indexBuffer?.Dispose();
            _indexCapacity = Math.Max(meshData.IndexCount, _indexCapacity * 2);
            _indexCapacity = Math.Max(_indexCapacity, 1024);
            _indexBuffer = new GpuBuffer(
                _webGpu, _device, _queue, $"{_label} Indices",
                (ulong)(_indexCapacity * Unsafe.SizeOf<uint>()),
                BufferUsage.Index | BufferUsage.CopyDst);
        }

        ReadOnlySpan<VoxelVertex> vertexSpan = meshData.Vertices.AsSpan(0, meshData.VertexCount);
        _vertexBuffer.Upload(vertexSpan);

        ReadOnlySpan<uint> indexSpan = meshData.Indices.AsSpan(0, meshData.IndexCount);
        _indexBuffer.Upload(indexSpan);
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
