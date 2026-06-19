using System.Numerics;
using Silk.NET.WebGPU;

namespace SilkWebGpuPbr;

public unsafe sealed class CubeScene : IDisposable
{
    private readonly PbrInstancedRenderer _renderer;
    private readonly GpuMesh _cube;
    private readonly PbrMaterial[] _materials;
    private readonly MeshInstance[] _instances;

    public CubeScene(
        WebGPU webGpu,
        Device* device,
        Queue* queue,
        TextureFormat colorFormat,
        TextureFormat depthFormat,
        float cubeSize = 1.0f)
    {
        _renderer = new PbrInstancedRenderer(webGpu, device, queue, colorFormat, depthFormat);

        MeshData cubeData = PrimitiveMeshes.CreateCube(cubeSize);
        _cube = new GpuMesh(webGpu, device, queue, "Cube", cubeData.Vertices, cubeData.Indices);

        _materials =
        [
            new PbrMaterial
            {
                BaseColorFactor = new Vector4(0.95f, 0.38f, 0.18f, 1.0f),
                EmissiveFactor = Vector4.Zero,
                MetallicFactor = 0.0f,
                RoughnessFactor = 0.48f,
                NormalScale = 1.0f,
                OcclusionStrength = 1.0f
            }
        ];
        _instances = [MeshInstance.Create(Matrix4x4.Identity, 0)];

        _renderer.UpdateMaterials(_materials);
        _renderer.UpdateInstances(_instances);
    }

    public void SetCubeTransform(Matrix4x4 model)
    {
        _instances[0] = MeshInstance.Create(model, 0);
        _renderer.UpdateInstances(_instances);
    }

    public void Draw(
        RenderPassEncoder* pass,
        Matrix4x4 viewProjection,
        Vector3 cameraPosition,
        Vector3 lightDirection)
    {
        _renderer.UpdateScene(GpuSceneData.Create(
            viewProjection,
            cameraPosition,
            lightDirection,
            Vector3.One,
            4.0f,
            new Vector3(0.035f, 0.04f, 0.05f),
            1.0f));

        _renderer.Draw(pass, _cube);
    }

    public void Dispose()
    {
        _cube.Dispose();
        _renderer.Dispose();
    }
}
