using System.Numerics;
using System.Runtime.InteropServices;

namespace SilkWebGpuPbr;

[StructLayout(LayoutKind.Sequential)]
public struct PbrMaterial
{
    public Vector4 BaseColorFactor;
    public Vector4 EmissiveFactor;
    public float MetallicFactor;
    public float RoughnessFactor;
    public float NormalScale;
    public float OcclusionStrength;

    public static PbrMaterial Default => new()
    {
        BaseColorFactor = Vector4.One,
        EmissiveFactor = Vector4.Zero,
        MetallicFactor = 0.0f,
        RoughnessFactor = 0.5f,
        NormalScale = 1.0f,
        OcclusionStrength = 1.0f
    };
}
