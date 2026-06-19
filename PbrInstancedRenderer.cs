using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.WebGPU;

namespace SilkWebGpuPbr;

public unsafe sealed class PbrInstancedRenderer : IDisposable
{
    private readonly WebGPU _webGpu;
    private readonly Device* _device;
    private readonly GpuBuffer _sceneBuffer;
    private readonly GpuBuffer _materialBuffer;
    private readonly GpuBuffer _instanceBuffer;
    private readonly BindGroupLayout* _sceneBindGroupLayout;
    private readonly BindGroup* _sceneBindGroup;
    private readonly PipelineLayout* _pipelineLayout;
    private readonly ShaderModule* _shaderModule;
    private readonly RenderPipeline* _pipeline;
    private uint _instanceCount;

    public PbrInstancedRenderer(
        WebGPU webGpu,
        Device* device,
        Queue* queue,
        TextureFormat colorFormat,
        TextureFormat depthFormat,
        uint maxMaterials = 1024,
        uint maxInstances = 100_000)
    {
        _webGpu = webGpu;
        _device = device;

        _sceneBuffer = new GpuBuffer(
            webGpu,
            device,
            queue,
            "PBR scene uniforms",
            AlignTo((ulong)Unsafe.SizeOf<GpuSceneData>(), 256),
            BufferUsage.Uniform | BufferUsage.CopyDst);

        _materialBuffer = new GpuBuffer(
            webGpu,
            device,
            queue,
            "PBR material storage",
            (ulong)(maxMaterials * Unsafe.SizeOf<PbrMaterial>()),
            BufferUsage.Storage | BufferUsage.CopyDst);

        _instanceBuffer = new GpuBuffer(
            webGpu,
            device,
            queue,
            "PBR instance buffer",
            (ulong)(maxInstances * Unsafe.SizeOf<MeshInstance>()),
            BufferUsage.Vertex | BufferUsage.CopyDst);

        _sceneBindGroupLayout = CreateSceneBindGroupLayout();
        _sceneBindGroup = CreateSceneBindGroup();
        _pipelineLayout = CreatePipelineLayout();
        _shaderModule = CreateShaderModule();
        _pipeline = CreateRenderPipeline(colorFormat, depthFormat);
    }

    public void UpdateScene(in GpuSceneData scene)
    {
        _sceneBuffer.Upload(scene);
    }

    public void UpdateMaterials(ReadOnlySpan<PbrMaterial> materials)
    {
        _materialBuffer.Upload(materials);
    }

    public void UpdateInstances(ReadOnlySpan<MeshInstance> instances)
    {
        _instanceBuffer.Upload(instances);
        _instanceCount = (uint)instances.Length;
    }

    public void Draw(RenderPassEncoder* pass, GpuMesh mesh, uint firstInstance = 0, uint? instanceCount = null)
    {
        uint count = instanceCount ?? _instanceCount;
        if (count == 0)
        {
            return;
        }

        _webGpu.RenderPassEncoderSetPipeline(pass, _pipeline);
        _webGpu.RenderPassEncoderSetBindGroup(pass, 0, _sceneBindGroup, 0, null);
        _webGpu.RenderPassEncoderSetVertexBuffer(pass, 0, mesh.VertexBuffer.Handle, 0, mesh.VertexBuffer.Size);
        _webGpu.RenderPassEncoderSetVertexBuffer(pass, 1, _instanceBuffer.Handle, 0, _instanceBuffer.Size);
        _webGpu.RenderPassEncoderSetIndexBuffer(pass, mesh.IndexBuffer.Handle, IndexFormat.Uint32, 0, mesh.IndexBuffer.Size);
        _webGpu.RenderPassEncoderDrawIndexed(pass, mesh.IndexCount, count, 0, 0, firstInstance);
    }

    private BindGroupLayout* CreateSceneBindGroupLayout()
    {
        BindGroupLayoutEntry* entries = stackalloc BindGroupLayoutEntry[2];
        entries[0] = new BindGroupLayoutEntry
        {
            Binding = 0,
            Visibility = ShaderStage.Vertex | ShaderStage.Fragment,
            Buffer = new BufferBindingLayout
            {
                Type = BufferBindingType.Uniform,
                MinBindingSize = (ulong)Unsafe.SizeOf<GpuSceneData>()
            }
        };
        entries[1] = new BindGroupLayoutEntry
        {
            Binding = 1,
            Visibility = ShaderStage.Vertex | ShaderStage.Fragment,
            Buffer = new BufferBindingLayout
            {
                Type = BufferBindingType.ReadOnlyStorage,
                MinBindingSize = (ulong)Unsafe.SizeOf<PbrMaterial>()
            }
        };

        BindGroupLayoutDescriptor descriptor = new()
        {
            EntryCount = 2,
            Entries = entries
        };

        return _webGpu.DeviceCreateBindGroupLayout(_device, &descriptor);
    }

    private BindGroup* CreateSceneBindGroup()
    {
        BindGroupEntry* entries = stackalloc BindGroupEntry[2];
        entries[0] = new BindGroupEntry
        {
            Binding = 0,
            Buffer = _sceneBuffer.Handle,
            Offset = 0,
            Size = _sceneBuffer.Size
        };
        entries[1] = new BindGroupEntry
        {
            Binding = 1,
            Buffer = _materialBuffer.Handle,
            Offset = 0,
            Size = _materialBuffer.Size
        };

        BindGroupDescriptor descriptor = new()
        {
            Layout = _sceneBindGroupLayout,
            EntryCount = 2,
            Entries = entries
        };

        return _webGpu.DeviceCreateBindGroup(_device, &descriptor);
    }

    private PipelineLayout* CreatePipelineLayout()
    {
        BindGroupLayout** layouts = stackalloc BindGroupLayout*[1];
        layouts[0] = _sceneBindGroupLayout;

        PipelineLayoutDescriptor descriptor = new()
        {
            BindGroupLayoutCount = 1,
            BindGroupLayouts = layouts
        };

        return _webGpu.DeviceCreatePipelineLayout(_device, &descriptor);
    }

    private ShaderModule* CreateShaderModule()
    {
        byte[] sourceBytes = System.Text.Encoding.UTF8.GetBytes(PbrShader.Source + "\0");
        fixed (byte* source = sourceBytes)
        {
            ShaderModuleWGSLDescriptor wgsl = new()
            {
                Chain = new ChainedStruct
                {
                    SType = SType.ShaderModuleWgslDescriptor
                },
                Code = source
            };

            ShaderModuleDescriptor descriptor = new()
            {
                NextInChain = (ChainedStruct*)&wgsl
            };

            return _webGpu.DeviceCreateShaderModule(_device, &descriptor);
        }
    }

    private RenderPipeline* CreateRenderPipeline(TextureFormat colorFormat, TextureFormat depthFormat)
    {
        VertexAttribute* vertexAttributes = stackalloc VertexAttribute[4];
        vertexAttributes[0] = new VertexAttribute { Format = VertexFormat.Float32x3, Offset = 0, ShaderLocation = 0 };
        vertexAttributes[1] = new VertexAttribute { Format = VertexFormat.Float32x3, Offset = 12, ShaderLocation = 1 };
        vertexAttributes[2] = new VertexAttribute { Format = VertexFormat.Float32x4, Offset = 24, ShaderLocation = 2 };
        vertexAttributes[3] = new VertexAttribute { Format = VertexFormat.Float32x2, Offset = 40, ShaderLocation = 3 };

        VertexAttribute* instanceAttributes = stackalloc VertexAttribute[9];
        for (uint i = 0; i < 4; i++)
        {
            instanceAttributes[i] = new VertexAttribute
            {
                Format = VertexFormat.Float32x4,
                Offset = i * 16,
                ShaderLocation = 4 + i
            };
            instanceAttributes[4 + i] = new VertexAttribute
            {
                Format = VertexFormat.Float32x4,
                Offset = 64 + i * 16,
                ShaderLocation = 8 + i
            };
        }
        instanceAttributes[8] = new VertexAttribute
        {
            Format = VertexFormat.Uint32,
            Offset = 128,
            ShaderLocation = 12
        };

        VertexBufferLayout* buffers = stackalloc VertexBufferLayout[2];
        buffers[0] = new VertexBufferLayout
        {
            ArrayStride = (ulong)Unsafe.SizeOf<PbrVertex>(),
            StepMode = VertexStepMode.Vertex,
            AttributeCount = 4,
            Attributes = vertexAttributes
        };
        buffers[1] = new VertexBufferLayout
        {
            ArrayStride = (ulong)Unsafe.SizeOf<MeshInstance>(),
            StepMode = VertexStepMode.Instance,
            AttributeCount = 9,
            Attributes = instanceAttributes
        };

        ColorTargetState* targets = stackalloc ColorTargetState[1];
        targets[0] = new ColorTargetState
        {
            Format = colorFormat,
            WriteMask = ColorWriteMask.All
        };

        FragmentState fragment = new()
        {
            Module = _shaderModule,
            EntryPoint = (byte*)SilkMarshal.StringToPtr("fs_main"),
            TargetCount = 1,
            Targets = targets
        };

        DepthStencilState depthStencil = new()
        {
            Format = depthFormat,
            DepthWriteEnabled = true,
            DepthCompare = CompareFunction.Less,
            StencilFront = new StencilFaceState { Compare = CompareFunction.Always },
            StencilBack = new StencilFaceState { Compare = CompareFunction.Always }
        };

        RenderPipelineDescriptor descriptor = new()
        {
            Layout = _pipelineLayout,
            Vertex = new VertexState
            {
                Module = _shaderModule,
                EntryPoint = (byte*)SilkMarshal.StringToPtr("vs_main"),
                BufferCount = 2,
                Buffers = buffers
            },
            Primitive = new PrimitiveState
            {
                Topology = PrimitiveTopology.TriangleList,
                FrontFace = FrontFace.Ccw,
                CullMode = CullMode.Back
            },
            Multisample = new MultisampleState
            {
                Count = 1,
                Mask = uint.MaxValue
            },
            Fragment = &fragment,
            DepthStencil = &depthStencil
        };

        RenderPipeline* pipeline = _webGpu.DeviceCreateRenderPipeline(_device, &descriptor);
        SilkMarshal.Free((nint)descriptor.Vertex.EntryPoint);
        SilkMarshal.Free((nint)fragment.EntryPoint);
        return pipeline;
    }

    private static ulong AlignTo(ulong value, ulong alignment)
    {
        return (value + alignment - 1) / alignment * alignment;
    }

    public void Dispose()
    {
        _instanceBuffer.Dispose();
        _materialBuffer.Dispose();
        _sceneBuffer.Dispose();
    }
}
