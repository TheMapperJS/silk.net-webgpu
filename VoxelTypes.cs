using System.Numerics;

namespace SilkWebGpuPbr;

public enum BlockType : ushort
{
    Air = 0,
    Dirt = 1,
    Grass = 2,
    Stone = 3
}

public struct VoxelVertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector4 Color;

    public VoxelVertex(Vector3 position, Vector3 normal, Vector4 color)
    {
        Position = position;
        Normal = normal;
        Color = color;
    }
}
