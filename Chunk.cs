using System.Numerics;

namespace SilkWebGpuPbr;

public class Chunk
{
    public const int Width = 32;
    public const int Height = 32;
    public const int Depth = 32;

    private readonly BlockType[] _blocks;

    public Chunk(Vector3 position)
    {
        Position = position;
        _blocks = new BlockType[Width * Height * Depth];
    }

    public Vector3 Position { get; }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
        {
            return BlockType.Air;
        }

        return _blocks[GetIndex(x, y, z)];
    }

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
        {
            return;
        }

        _blocks[GetIndex(x, y, z)] = type;
    }

    private static int GetIndex(int x, int y, int z)
    {
        return x + y * Width + z * Width * Height;
    }
}
