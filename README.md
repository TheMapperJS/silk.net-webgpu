# SilkWebGpuPbr

## Run the sample

```powershell
dotnet run --project (insert path here)\SilkWebGpuPbr\SilkWebGpuPbr.csproj
```

This opens a Silk.NET window and renders a rotating PBR-lit cube with WebGPU.
A small Silk.NET WebGPU renderer core for physically based shading and hardware instancing.

## What is included

- `PbrInstancedRenderer`: creates the WebGPU bind group layout, pipeline layout, WGSL shader module, render pipeline, scene/material buffers, and instance buffer.
- `GpuMesh`: uploads indexed mesh vertex/index buffers.
- `PrimitiveMeshes.CreateCube`: returns a 24-vertex, 36-index cube with normals, tangents, and UVs.
- `CubeScene`: ready-to-draw one-cube scene wrapper for an existing WebGPU render pass.
- `PbrVertex`: position, normal, tangent, and UV vertex layout.
- `MeshInstance`: per-instance model matrix, normal matrix, and material index.
- `PbrMaterial`: base color, emissive, metallic, roughness, normal scale, and occlusion fields.
- `GpuSceneData`: view-projection, camera position, directional light, and ambient light uniforms.

## Draw a cube

Create the cube scene after you have a Silk.NET WebGPU `Device*`, `Queue*`, swapchain/surface color format, and depth format:

```csharp
var cubeScene = new CubeScene(webGpu, device, queue, colorFormat, depthFormat, cubeSize: 1.0f);
```

Inside an active `RenderPassEncoder*`:

```csharp
cubeScene.SetCubeTransform(Matrix4x4.CreateRotationY((float)time));
cubeScene.Draw(
    renderPass,
    viewProjection,
    cameraPosition,
    lightDirection: Vector3.Normalize(new Vector3(-0.4f, -1.0f, -0.2f)));
```

Dispose it with the rest of your GPU resources:

```csharp
cubeScene.Dispose();
```

## Lower-level use

If you want to manage the mesh yourself:

```csharp
MeshData cube = PrimitiveMeshes.CreateCube();
using var gpuCube = new GpuMesh(webGpu, device, queue, "Cube", cube.Vertices, cube.Indices);

renderer.UpdateMaterials(materials);
renderer.UpdateInstances(instances);
renderer.Draw(renderPass, gpuCube);
```

The pipeline expects `TextureFormat` values from your configured WebGPU surface and depth texture. It does not own the device, queue, surface, command encoder, render pass, or frame lifecycle.

