using System;
using System.Drawing;
using System.Globalization;
using System.IO;

class HeightMapToSTL
{
    static void Main(string[] args)

    {
        Console.WriteLine("alright nerds, this piece of junk is ONLY made to crunch VOID-FILLED SRTM tiles that have been converted to BMP format. (do this in paint you weirdo) as of writing you can get these by drunk punching your way through https://earthexplorer.usgs.gov/ just make sure you search the datasets for SRTM... prepare your computer for a single threaded process beat down, its gonna look like this shid has crashed but its just plunking away, hope you dont run out of ram lol");
        Console.Write("Path to grayscale bitmap image: ");
        string imagePath = Console.ReadLine();

        Console.Write("Desired mesh width in mm: ");
        float widthMM = float.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);

        Console.Write("Desired mesh height in mm: ");
        float heightMM = float.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);

        Console.Write("Z height scale (e.g., 10 = exaggerate heights 10x): ");
        float zScale = float.Parse(Console.ReadLine(), CultureInfo.InvariantCulture);

        Console.Write("Grid resolution (e.g., 2 means use every 2nd pixel): ");
        int step = int.Parse(Console.ReadLine());

        Console.Write("Output STL file path: ");
        string stlPath = Console.ReadLine();

        Bitmap bmp = new Bitmap(imagePath);
        int xSteps = bmp.Width / step;
        int ySteps = bmp.Height / step;

        float xScale = widthMM / (xSteps - 1);
        float yScale = heightMM / (ySteps - 1);

        Vector3[,] vertices = new Vector3[xSteps, ySteps];

        for (int y = 0; y < ySteps; y++)
        {
            for (int x = 0; x < xSteps; x++)
            {
                vertices[x, y] = GetVertex(bmp, x * step, y * step, xScale, yScale, zScale);
            }
        }

        using StreamWriter writer = new StreamWriter(stlPath);
        writer.WriteLine("solid heightmap");

        // Top surface
        for (int y = 0; y < ySteps - 1; y++)
        {
            for (int x = 0; x < xSteps - 1; x++)
            {
                Vector3 p00 = vertices[x, y];
                Vector3 p10 = vertices[x + 1, y];
                Vector3 p01 = vertices[x, y + 1];
                Vector3 p11 = vertices[x + 1, y + 1];

                WriteFacet(writer, p00, p10, p11);
                WriteFacet(writer, p00, p11, p01);
            }
        }

        // Edge walls
        AddWall(writer, vertices, xSteps, ySteps, edge: "left");
        AddWall(writer, vertices, xSteps, ySteps, edge: "right");
        AddWall(writer, vertices, xSteps, ySteps, edge: "top");
        AddWall(writer, vertices, xSteps, ySteps, edge: "bottom");

        // Base
        Vector3[,] baseVerts = new Vector3[xSteps, ySteps];
        for (int y = 0; y < ySteps; y++)
        {
            for (int x = 0; x < xSteps; x++)
            {
                baseVerts[x, y] = new Vector3(vertices[x, y].X, vertices[x, y].Y, 0);
            }
        }

        for (int y = 0; y < ySteps - 1; y++)
        {
            for (int x = 0; x < xSteps - 1; x++)
            {
                Vector3 p00 = baseVerts[x, y];
                Vector3 p10 = baseVerts[x + 1, y];
                Vector3 p01 = baseVerts[x, y + 1];
                Vector3 p11 = baseVerts[x + 1, y + 1];

                WriteFacet(writer, p10, p00, p01); // Note reversed winding to flip normal
                WriteFacet(writer, p10, p01, p11);
            }
        }

        writer.WriteLine("endsolid heightmap");
        Console.WriteLine($"STL file with edge walls saved to {stlPath}");
    }

    static void AddWall(StreamWriter writer, Vector3[,] vertices, int xSteps, int ySteps, string edge)
    {
        int maxX = xSteps - 1;
        int maxY = ySteps - 1;

        if (edge == "left")
        {
            for (int y = 0; y < maxY; y++)
            {
                WriteWallQuad(writer, vertices[0, y], vertices[0, y + 1]);
            }
        }
        else if (edge == "right")
        {
            for (int y = 0; y < maxY; y++)
            {
                WriteWallQuad(writer, vertices[maxX, y + 1], vertices[maxX, y]);
            }
        }
        else if (edge == "top")
        {
            for (int x = 0; x < maxX; x++)
            {
                WriteWallQuad(writer, vertices[x + 1, 0], vertices[x, 0]);
            }
        }
        else if (edge == "bottom")
        {
            for (int x = 0; x < maxX; x++)
            {
                WriteWallQuad(writer, vertices[x, maxY], vertices[x + 1, maxY]);
            }
        }
    }

    static void WriteWallQuad(StreamWriter writer, Vector3 top1, Vector3 top2)
    {
        Vector3 bottom1 = new Vector3(top1.X, top1.Y, 0);
        Vector3 bottom2 = new Vector3(top2.X, top2.Y, 0);

        WriteFacet(writer, top1, top2, bottom2);
        WriteFacet(writer, top1, bottom2, bottom1);
    }

    static Vector3 GetVertex(Bitmap bmp, int x, int y, float xScale, float yScale, float zScale)
    {
        Color color = bmp.GetPixel(x, y);
        float gray = (color.R + color.G + color.B) / 3.0f;
        float z = (gray / 255.0f) * zScale;
        return new Vector3(
            x / (float)(bmp.Width - 1) * xScale * (bmp.Width - 1),
            y / (float)(bmp.Height - 1) * yScale * (bmp.Height - 1),
            z
        );
    }

    static void WriteFacet(StreamWriter writer, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        Vector3 normal = Vector3.Cross(v2 - v1, v3 - v1).Normalize();
        writer.WriteLine($"  facet normal {normal.X} {normal.Y} {normal.Z}");
        writer.WriteLine("    outer loop");
        writer.WriteLine($"      vertex {v1.X} {v1.Y} {v1.Z}");
        writer.WriteLine($"      vertex {v2.X} {v2.Y} {v2.Z}");
        writer.WriteLine($"      vertex {v3.X} {v3.Y} {v3.Z}");
        writer.WriteLine("    endloop");
        writer.WriteLine("  endfacet");
    }
}

struct Vector3
{
    public float X, Y, Z;

    public Vector3(float x, float y, float z)
    {
        X = x; Y = y; Z = z;
    }

    public static Vector3 operator -(Vector3 a, Vector3 b) =>
        new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3 Cross(Vector3 a, Vector3 b) =>
        new Vector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );

    public Vector3 Normalize()
    {
        float len = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        if (len == 0) return new Vector3(0, 0, 0);
        return new Vector3(X / len, Y / len, Z / len);
    }
}
