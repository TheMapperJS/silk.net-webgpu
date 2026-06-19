using System.Runtime.CompilerServices;
using Silk.NET.WebGPU;
using Silk.NET.Core.Native;

namespace SilkWebGpuPbr;

public unsafe class ChunkRenderer : IDisposable
{
    private readonly WebGPU _webGpu;
    private readonly Device* _device;

    private readonly GpuBuffer _sceneBuffer;
    private readonly BindGroupLayout* _sceneBindGroupLayout;
    private readonly BindGroup* _sceneBindGroup;
    private readonly PipelineLayout* _pipelineLayout;
    private readonly ShaderModule* _shaderModule;
    private readonly RenderPipeline* _pipeline;

    public ChunkRenderer(
        WebGPU webGpu,
        Device* device,
        Queue* queue,
        TextureFormat colorFormat,
        TextureFormat depthFormat)
    {
        _webGpu = webGpu;
        _device = device;

        _sceneBuffer = new GpuBuffer(
            webGpu,
            device,
            queue,
            "Chunk Renderer Scene Buffer",
            AlignTo((ulong)Unsafe.SizeOf<GpuSceneData>(), 256),
            BufferUsage.Uniform | BufferUsage.CopyDst);

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

    public void Draw(RenderPassEncoder* pass, DynamicGpuMesh mesh)
    {
        if (mesh.IndexCount == 0 || mesh.VertexBuffer == null || mesh.IndexBuffer == null)
        {
            return;
        }

        _webGpu.RenderPassEncoderSetPipeline(pass, _pipeline);
        _webGpu.RenderPassEncoderSetBindGroup(pass, 0, _sceneBindGroup, 0, null);

        _webGpu.RenderPassEncoderSetVertexBuffer(pass, 0, mesh.VertexBuffer, 0, mesh.VertexBufferSize);
        _webGpu.RenderPassEncoderSetIndexBuffer(pass, mesh.IndexBuffer, IndexFormat.Uint32, 0, mesh.IndexBufferSize);

        _webGpu.RenderPassEncoderDrawIndexed(pass, (uint)mesh.IndexCount, 1, 0, 0, 0);
    }

    private BindGroupLayout* CreateSceneBindGroupLayout()
    {
        BindGroupLayoutEntry* entries = stackalloc BindGroupLayoutEntry[1];
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

        BindGroupLayoutDescriptor descriptor = new()
        {
            EntryCount = 1,
            Entries = entries
        };

        return _webGpu.DeviceCreateBindGroupLayout(_device, &descriptor);
    }

    private BindGroup* CreateSceneBindGroup()
    {
        BindGroupEntry* entries = stackalloc BindGroupEntry[1];
        entries[0] = new BindGroupEntry
        {
            Binding = 0,
            Buffer = _sceneBuffer.Handle,
            Offset = 0,
            Size = _sceneBuffer.Size
        };

        BindGroupDescriptor descriptor = new()
        {
            Layout = _sceneBindGroupLayout,
            EntryCount = 1,
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
        byte[] sourceBytes = System.Text.Encoding.UTF8.GetBytes(VoxelShader.Source + "\0");
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
        VertexAttribute* vertexAttributes = stackalloc VertexAttribute[3];
        vertexAttributes[0] = new VertexAttribute { Format = VertexFormat.Float32x3, Offset = 0, ShaderLocation = 0 }; // Position
        vertexAttributes[1] = new VertexAttribute { Format = VertexFormat.Float32x3, Offset = 12, ShaderLocation = 1 }; // Normal
        vertexAttributes[2] = new VertexAttribute { Format = VertexFormat.Float32x4, Offset = 24, ShaderLocation = 2 }; // Color

        VertexBufferLayout* buffers = stackalloc VertexBufferLayout[1];
        buffers[0] = new VertexBufferLayout
        {
            ArrayStride = (ulong)Unsafe.SizeOf<VoxelVertex>(),
            StepMode = VertexStepMode.Vertex,
            AttributeCount = 3,
            Attributes = vertexAttributes
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
                BufferCount = 1,
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
        _sceneBuffer.Dispose();
        _webGpu.RenderPipelineRelease(_pipeline);
        _webGpu.ShaderModuleRelease(_shaderModule);
        _webGpu.PipelineLayoutRelease(_pipelineLayout);
        _webGpu.BindGroupRelease(_sceneBindGroup);
        _webGpu.BindGroupLayoutRelease(_sceneBindGroupLayout);
    }
}
