using System.Numerics;
using System.Runtime.InteropServices;

namespace SilkWebGpuPbr;

[StructLayout(LayoutKind.Sequential)]
public struct GpuSceneData
{
    public Matrix4x4 ViewProjection;
    public Vector4 CameraPosition;
    public Vector4 LightDirection;
    public Vector4 LightColorAndIntensity;
    public Vector4 AmbientColorAndIntensity;

    public static GpuSceneData Create(
        Matrix4x4 viewProjection,
        Vector3 cameraPosition,
        Vector3 lightDirection,
        Vector3 lightColor,
        float lightIntensity,
        Vector3 ambientColor,
        float ambientIntensity)
    {
        return new GpuSceneData
        {
            ViewProjection = viewProjection,
            CameraPosition = new Vector4(cameraPosition, 1.0f),
            LightDirection = new Vector4(Vector3.Normalize(lightDirection), 0.0f),
            LightColorAndIntensity = new Vector4(lightColor, lightIntensity),
            AmbientColorAndIntensity = new Vector4(ambientColor, ambientIntensity)
        };
    }
}
