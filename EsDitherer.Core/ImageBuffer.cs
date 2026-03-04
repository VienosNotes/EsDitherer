using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EsDitherer.Core;

public struct PixelF
{
    public float R;
    public float G;
    public float B;
}

public static class MaskValue
{
    public const int Inactive = 0;
    public const int Active = 1;
}

public class ImageBuffer(int width, int height)
{
    public PixelF[] Pixels { get; } = new PixelF[width * height];
    public byte[] Mask { get; } = new byte[width * height];
    public int Width { get; } = width;
    public int Height { get; } = height;

    public int IndexOf(int x, int y) => y * Width + x;
    public ref PixelF this[int x, int y] => ref Pixels[y * Width + x];
    public byte GetMask(int x, int y) => Mask[y * Width + x];

    public void WriteImage(string filename)
    {
        using var img = new Image<Rgba32>(Width, Height);

        for (int y = 0; y < Height; y++)
        {
            Span<Rgba32> row = img.DangerousGetPixelRowMemory(y).Span;
            for (int x = 0; x < Width; x++)
            {
                int i = IndexOf(x, y);

                // Mask=0 は白背景表示（好みで透明にしてもOK）
                if (Mask[i] == 0)
                {
                    row[x] = new Rgba32(255, 255, 255, 255);
                    continue;
                }

                PixelF p = Pixels[i];
                byte r = ToByte01(p.R);
                byte g = ToByte01(p.G);
                byte b = ToByte01(p.B);
                row[x] = new Rgba32(r, g, b, 255);
            }
        }

        img.Save(filename);
    }

    private static byte ToByte01(float v)
    {
        if (v < 0f) v = 0f;
        if (v > 1f) v = 1f;
        return (byte)(v * 255f + 0.5f);
    }

    /// <summary>
    /// 画像ファイルを読み込み、ImageBuffer (PixelF: 0..1 sRGB) に変換します。
    /// Mask は全ピクセル 1（有効）で初期化します。
    /// </summary>
    public static ImageBuffer LoadFromFile(string filename, bool autoOrient = true)
    {
        if (filename is null) throw new ArgumentNullException(nameof(filename));

        using Image<Rgba32> img = Image.Load<Rgba32>(filename);

        if (autoOrient)
        {
            // EXIF の向き補正（必要なら）
            img.Mutate(x => x.AutoOrient());
        }

        return FromImage(img);
    }

    /// <summary>
    /// ImageSharp の Image から ImageBuffer へ変換します（PixelF: 0..1 sRGB）。
    /// </summary>
    public static ImageBuffer FromImage(Image<Rgba32> img)
    {
        if (img is null) throw new ArgumentNullException(nameof(img));

        var buf = new ImageBuffer(img.Width, img.Height);

        // まず全部有効
        Array.Fill(buf.Mask, (byte)1);

        // ピクセルコピー（sRGB 0..1）
        for (int y = 0; y < img.Height; y++)
        {
            Span<Rgba32> row = img.DangerousGetPixelRowMemory(y).Span;
            int rowBase = y * img.Width;

            for (int x = 0; x < img.Width; x++)
            {
                Rgba32 p = row[x];

                // 透明が来た場合の扱い：
                // ここでは白背景にアルファブレンドして不透明化（電子ペーパー用途で安全）
                float a = p.A / 255f;

                float r = p.R / 255f;
                float g = p.G / 255f;
                float b = p.B / 255f;

                if (a < 1f)
                {
                    // white over
                    r = r * a + 1f * (1f - a);
                    g = g * a + 1f * (1f - a);
                    b = b * a + 1f * (1f - a);
                }

                buf.Pixels[rowBase + x] = new PixelF { R = r, G = g, B = b };
            }
        }

        return buf;
    }
}