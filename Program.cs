using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;
using Silk.NET.Windowing;

namespace SilkWebGpuPbr;

public static unsafe class Program
{
    private const TextureFormat DepthFormat = TextureFormat.Depth24Plus;

    private static IWindow _window = null!;
    private static WebGPU _wgpu = null!;
    private static Wgpu _wgpuExt = null!;
    private static Instance* _instance;
    private static Surface* _surface;
    private static Adapter* _adapter;
    private static Device* _device;
    private static Queue* _queue;
    private static SurfaceConfiguration _surfaceConfiguration;
    private static TextureFormat _surfaceFormat;
    private static Texture* _depthTexture;
    private static TextureView* _depthView;
    private static CubeScene? _cubeScene;
    private static double _time;

    public static void Main()
    {
        WindowOptions options = WindowOptions.Default with
        {
            API = GraphicsAPI.None,
            Size = new Vector2D<int>(1280, 720),
            Title = "Silk.NET WebGPU PBR Cube",
            VSync = true
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFramebufferResize;
        _window.Closing += OnClosing;
        _window.Run();
        _window.Dispose();
    }

    private static void OnLoad()
    {
        _wgpu = WebGPU.GetApi();
        _wgpuExt = new Wgpu(_wgpu.Context);

        InstanceDescriptor instanceDescriptor = new();
        _instance = _wgpu.CreateInstance(&instanceDescriptor);
        _surface = WebGPUSurface.CreateWebGPUSurface(_window, _wgpu, _instance);

        _adapter = RequestAdapter();
        _device = RequestDevice(_adapter);
        _queue = _wgpu.DeviceGetQueue(_device);

        ConfigureSurface();
        CreateDepthResources();
        _cubeScene = new CubeScene(_wgpu, _device, _queue, _surfaceFormat, DepthFormat, 1.5f);
    }

    private static Adapter* RequestAdapter()
    {
        InstanceEnumerateAdapterOptions options = new();
        nuint count = _wgpuExt.InstanceEnumerateAdapters(_instance, &options, null);
        if (count == 0)
        {
            throw new InvalidOperationException("No WebGPU adapters were found.");
        }

        Adapter** adapters = stackalloc Adapter*[(int)count];
        _wgpuExt.InstanceEnumerateAdapters(_instance, &options, adapters);
        return adapters[0];
    }

    private static Device* RequestDevice(Adapter* adapter)
    {
        Device* device = null;
        DeviceDescriptor descriptor = new();

        _wgpu.AdapterRequestDevice(adapter, &descriptor, new PfnRequestDeviceCallback(OnDeviceRequested), &device);

        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (device is null && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(1);
        }

        return device is null
            ? throw new InvalidOperationException("Timed out while requesting a WebGPU device.")
            : device;
    }

    private static void OnDeviceRequested(RequestDeviceStatus status, Device* device, byte* message, void* userdata)
    {
        if (status != RequestDeviceStatus.Success)
        {
            string error = message is null ? "unknown error" : Marshal.PtrToStringUTF8((nint)message) ?? "unknown error";
            throw new InvalidOperationException($"WebGPU device request failed: {error}");
        }

        *(Device**)userdata = device;
    }

    private static void ConfigureSurface()
    {
        SurfaceCapabilities capabilities = new();
        _wgpu.SurfaceGetCapabilities(_surface, _adapter, &capabilities);
        _surfaceFormat = capabilities.FormatCount > 0 ? capabilities.Formats[0] : _wgpu.SurfaceGetPreferredFormat(_surface, _adapter);

        Vector2D<int> size = _window.FramebufferSize;
        _surfaceConfiguration = new SurfaceConfiguration
        {
            Usage = TextureUsage.RenderAttachment,
            Device = _device,
            Format = _surfaceFormat,
            PresentMode = PresentMode.Fifo,
            AlphaMode = capabilities.AlphaModeCount > 0 ? capabilities.AlphaModes[0] : CompositeAlphaMode.Auto,
            Width = (uint)Math.Max(1, size.X),
            Height = (uint)Math.Max(1, size.Y)
        };

        _wgpu.SurfaceConfigure(_surface, in _surfaceConfiguration);
    }

    private static void CreateDepthResources()
    {
        _depthView = null;
        _depthTexture = null;

        TextureDescriptor descriptor = new()
        {
            Dimension = TextureDimension.Dimension2D,
            Size = new Extent3D(_surfaceConfiguration.Width, _surfaceConfiguration.Height, 1),
            Format = DepthFormat,
            MipLevelCount = 1,
            SampleCount = 1,
            Usage = TextureUsage.RenderAttachment
        };

        _depthTexture = _wgpu.DeviceCreateTexture(_device, &descriptor);
        TextureViewDescriptor viewDescriptor = new()
        {
            Format = DepthFormat,
            Dimension = TextureViewDimension.Dimension2D,
            BaseMipLevel = 0,
            MipLevelCount = 1,
            BaseArrayLayer = 0,
            ArrayLayerCount = 1,
            Aspect = TextureAspect.DepthOnly
        };
        _depthView = _wgpu.TextureCreateView(_depthTexture, &viewDescriptor);
    }

    private static void OnFramebufferResize(Vector2D<int> size)
    {
        if (_surface is null || _device is null || size.X <= 0 || size.Y <= 0)
        {
            return;
        }

        ConfigureSurface();
        CreateDepthResources();
    }

    private static void OnRender(double deltaSeconds)
    {
        if (_cubeScene is null)
        {
            return;
        }

        _time += deltaSeconds;

        SurfaceTexture surfaceTexture = new();
        _wgpu.SurfaceGetCurrentTexture(_surface, &surfaceTexture);
        if (surfaceTexture.Status != SurfaceGetCurrentTextureStatus.Success)
        {
            ConfigureSurface();
            return;
        }

        TextureView* colorView = _wgpu.TextureCreateView(surfaceTexture.Texture, null);
        CommandEncoderDescriptor encoderDescriptor = new();
        CommandEncoder* encoder = _wgpu.DeviceCreateCommandEncoder(_device, &encoderDescriptor);

        RenderPassColorAttachment colorAttachment = new()
        {
            View = colorView,
            LoadOp = LoadOp.Clear,
            StoreOp = StoreOp.Store,
            ClearValue = new Color(0.025, 0.03, 0.04, 1.0)
        };

        RenderPassDepthStencilAttachment depthAttachment = new()
        {
            View = _depthView,
            DepthLoadOp = LoadOp.Clear,
            DepthStoreOp = StoreOp.Store,
            DepthClearValue = 1.0f,
            StencilLoadOp = LoadOp.Undefined,
            StencilStoreOp = StoreOp.Undefined
        };

        RenderPassDescriptor passDescriptor = new()
        {
            ColorAttachmentCount = 1,
            ColorAttachments = &colorAttachment,
            DepthStencilAttachment = &depthAttachment
        };

        RenderPassEncoder* pass = _wgpu.CommandEncoderBeginRenderPass(encoder, &passDescriptor);

        Matrix4x4 model = Matrix4x4.CreateRotationY((float)_time) * Matrix4x4.CreateRotationX((float)_time * 0.45f);
        _cubeScene.SetCubeTransform(model);

        Vector3 cameraPosition = new(0, 1.2f, 4.0f);
        Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPosition, Vector3.Zero, Vector3.UnitY);
        float aspect = (float)_surfaceConfiguration.Width / _surfaceConfiguration.Height;
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4.0f, aspect, 0.1f, 100.0f);
        _cubeScene.Draw(pass, view * projection, cameraPosition, Vector3.Normalize(new Vector3(-0.4f, -1.0f, -0.2f)));

        _wgpu.RenderPassEncoderEnd(pass);
        CommandBufferDescriptor commandBufferDescriptor = new();
        CommandBuffer* commandBuffer = _wgpu.CommandEncoderFinish(encoder, &commandBufferDescriptor);
        _wgpu.QueueSubmit(_queue, 1, &commandBuffer);
        _wgpu.SurfacePresent(_surface);
    }

    private static void OnClosing()
    {
        _cubeScene?.Dispose();
    }
}
