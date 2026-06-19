using System.Numerics;

namespace SilkWebGpuPbr;

public readonly record struct MeshData(PbrVertex[] Vertices, uint[] Indices);

public static class PrimitiveMeshes
{
    public static MeshData CreateCube(float size = 1.0f)
    {
        float h = size * 0.5f;

        PbrVertex[] vertices =
        [
            // Front
            Vertex(new(-h, -h,  h), Vector3.UnitZ, new(1, 0, 0, 1), new(0, 1)),
            Vertex(new( h, -h,  h), Vector3.UnitZ, new(1, 0, 0, 1), new(1, 1)),
            Vertex(new( h,  h,  h), Vector3.UnitZ, new(1, 0, 0, 1), new(1, 0)),
            Vertex(new(-h,  h,  h), Vector3.UnitZ, new(1, 0, 0, 1), new(0, 0)),

            // Back
            Vertex(new( h, -h, -h), -Vector3.UnitZ, new(-1, 0, 0, 1), new(0, 1)),
            Vertex(new(-h, -h, -h), -Vector3.UnitZ, new(-1, 0, 0, 1), new(1, 1)),
            Vertex(new(-h,  h, -h), -Vector3.UnitZ, new(-1, 0, 0, 1), new(1, 0)),
            Vertex(new( h,  h, -h), -Vector3.UnitZ, new(-1, 0, 0, 1), new(0, 0)),

            // Left
            Vertex(new(-h, -h, -h), -Vector3.UnitX, new(0, 0, 1, 1), new(0, 1)),
            Vertex(new(-h, -h,  h), -Vector3.UnitX, new(0, 0, 1, 1), new(1, 1)),
            Vertex(new(-h,  h,  h), -Vector3.UnitX, new(0, 0, 1, 1), new(1, 0)),
            Vertex(new(-h,  h, -h), -Vector3.UnitX, new(0, 0, 1, 1), new(0, 0)),

            // Right
            Vertex(new( h, -h,  h), Vector3.UnitX, new(0, 0, -1, 1), new(0, 1)),
            Vertex(new( h, -h, -h), Vector3.UnitX, new(0, 0, -1, 1), new(1, 1)),
            Vertex(new( h,  h, -h), Vector3.UnitX, new(0, 0, -1, 1), new(1, 0)),
            Vertex(new( h,  h,  h), Vector3.UnitX, new(0, 0, -1, 1), new(0, 0)),

            // Top
            Vertex(new(-h,  h,  h), Vector3.UnitY, new(1, 0, 0, 1), new(0, 1)),
            Vertex(new( h,  h,  h), Vector3.UnitY, new(1, 0, 0, 1), new(1, 1)),
            Vertex(new( h,  h, -h), Vector3.UnitY, new(1, 0, 0, 1), new(1, 0)),
            Vertex(new(-h,  h, -h), Vector3.UnitY, new(1, 0, 0, 1), new(0, 0)),

            // Bottom
            Vertex(new(-h, -h, -h), -Vector3.UnitY, new(1, 0, 0, 1), new(0, 1)),
            Vertex(new( h, -h, -h), -Vector3.UnitY, new(1, 0, 0, 1), new(1, 1)),
            Vertex(new( h, -h,  h), -Vector3.UnitY, new(1, 0, 0, 1), new(1, 0)),
            Vertex(new(-h, -h,  h), -Vector3.UnitY, new(1, 0, 0, 1), new(0, 0)),
        ];

        uint[] indices =
        [
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        ];

        return new MeshData(vertices, indices);
    }

    private static PbrVertex Vertex(Vector3 position, Vector3 normal, Vector4 tangent, Vector2 uv)
    {
        return new PbrVertex
        {
            Position = position,
            Normal = normal,
            Tangent = tangent,
            TexCoord0 = uv
        };
    }
}
