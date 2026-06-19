using System.Numerics;
using System.Runtime.InteropServices;

namespace SilkWebGpuPbr;

[StructLayout(LayoutKind.Sequential)]
public struct PbrVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector4 Tangent;
    public Vector2 TexCoord0;
}
