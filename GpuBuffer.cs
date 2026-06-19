using System.Runtime.CompilerServices;
using System.Text;
using Silk.NET.WebGPU;

namespace SilkWebGpuPbr;

public unsafe sealed class GpuBuffer : IDisposable
{
    private readonly WebGPU _webGpu;
    private readonly Queue* _queue;

    public GpuBuffer(WebGPU webGpu, Device* device, Queue* queue, string label, ulong size, BufferUsage usage)
    {
        _webGpu = webGpu;
        _queue = queue;
        Size = size;

        byte[] labelBytes = Encoding.UTF8.GetBytes(label + "\0");
        fixed (byte* labelPtr = labelBytes)
        {
            BufferDescriptor descriptor = new()
            {
                Label = labelPtr,
                Size = size,
                Usage = usage
            };

            Handle = _webGpu.DeviceCreateBuffer(device, &descriptor);
        }
    }

    public Silk.NET.WebGPU.Buffer* Handle { get; private set; }

    public ulong Size { get; }

    public void Upload<T>(ReadOnlySpan<T> data, ulong destinationOffset = 0)
        where T : unmanaged
    {
        ulong byteLength = (ulong)(data.Length * Unsafe.SizeOf<T>());
        if (destinationOffset + byteLength > Size)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Upload would exceed the GPU buffer size.");
        }

        fixed (T* source = data)
        {
            _webGpu.QueueWriteBuffer(_queue, Handle, destinationOffset, source, (nuint)byteLength);
        }
    }

    public void Upload<T>(in T data, ulong destinationOffset = 0)
        where T : unmanaged
    {
        fixed (T* source = &data)
        {
            _webGpu.QueueWriteBuffer(_queue, Handle, destinationOffset, source, (nuint)Unsafe.SizeOf<T>());
        }
    }

    public void Dispose()
    {
        if (Handle is null)
        {
            return;
        }

        _webGpu.BufferDestroy(Handle);
        Handle = null;
    }
}
