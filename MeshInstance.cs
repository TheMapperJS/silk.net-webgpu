using System.Numerics;
using System.Runtime.InteropServices;

namespace SilkWebGpuPbr;

[StructLayout(LayoutKind.Sequential)]
public struct MeshInstance
{
    public Matrix4x4 Model;
    public Matrix4x4 NormalModel;
    public uint MaterialIndex;
    private readonly uint _padding0;
    private readonly uint _padding1;
    private readonly uint _padding2;

    public static MeshInstance Create(Matrix4x4 model, uint materialIndex)
    {
        Matrix4x4.Invert(model, out Matrix4x4 inverseModel);

        return new MeshInstance
        {
            Model = model,
            NormalModel = Matrix4x4.Transpose(inverseModel),
            MaterialIndex = materialIndex
        };
    }
}
