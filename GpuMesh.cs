using System.Runtime.CompilerServices;
using Silk.NET.WebGPU;

namespace SilkWebGpuPbr;

public unsafe sealed class GpuMesh : IDisposable
{
    public GpuMesh(
        WebGPU webGpu,
        Device* device,
        Queue* queue,
        string name,
        ReadOnlySpan<PbrVertex> vertices,
        ReadOnlySpan<uint> indices)
    {
        if (vertices.IsEmpty)
        {
            throw new ArgumentException("A mesh needs at least one vertex.", nameof(vertices));
        }

        if (indices.IsEmpty)
        {
            throw new ArgumentException("A mesh needs at least one index.", nameof(indices));
        }

        VertexBuffer = new GpuBuffer(
            webGpu,
            device,
            queue,
            $"{name} vertices",
            (ulong)(vertices.Length * Unsafe.SizeOf<PbrVertex>()),
            BufferUsage.Vertex | BufferUsage.CopyDst);
        VertexBuffer.Upload(vertices);

        IndexBuffer = new GpuBuffer(
            webGpu,
            device,
            queue,
            $"{name} indices",
            (ulong)(indices.Length * sizeof(uint)),
            BufferUsage.Index | BufferUsage.CopyDst);
        IndexBuffer.Upload(indices);

        IndexCount = (uint)indices.Length;
    }

    public GpuBuffer VertexBuffer { get; }

    public GpuBuffer IndexBuffer { get; }

    public uint IndexCount { get; }

    public void Dispose()
    {
        IndexBuffer.Dispose();
        VertexBuffer.Dispose();
    }
}
