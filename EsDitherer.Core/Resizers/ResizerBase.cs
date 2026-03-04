namespace EsDitherer.Core.Resizers;

using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public sealed class FitInsideResizer : IResizer
{
    private readonly PixelF _background;

    public FitInsideResizer(PixelF? background = null)
    {
        _background = background ?? new PixelF { R = 1f, G = 1f, B = 1f }; // white
    }

    public ImageBuffer Resize(ImageBuffer source, int width, int height, bool allowRotate)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (source.Width <= 0 || source.Height <= 0) throw new ArgumentException("Invalid source size.");

        // Decide whether to rotate 90 degrees (swap W/H) to maximize scale
        bool rotate90 = false;
        if (allowRotate)
        {
            float sNoRot = FitScale(source.Width, source.Height, width, height);
            float sRot   = FitScale(source.Height, source.Width, width, height); // swapped

            // If rotating makes it bigger, rotate
            if (sRot > sNoRot) rotate90 = true;
        }

        int srcW = rotate90 ? source.Height : source.Width;
        int srcH = rotate90 ? source.Width  : source.Height;

        float scale = FitScale(srcW, srcH, width, height);

        int scaledW = Math.Max(1, (int)MathF.Round(srcW * scale));
        int scaledH = Math.Max(1, (int)MathF.Round(srcH * scale));

        int offsetX = (width - scaledW) / 2;
        int offsetY = (height - scaledH) / 2;

        var dst = new ImageBuffer(width, height);

        // Fill background + mask = 0
        for (int i = 0; i < dst.Pixels.Length; i++)
        {
            dst.Pixels[i] = _background;
            dst.Mask[i] = 0;
        }

        // Map each destination pixel in content rect back to source space
        // Use pixel-center mapping: (x+0.5) in dst corresponds to (u+0.5) in src
        // Inverse mapping:
        //   u = ((x - offsetX) + 0.5) / scale - 0.5
        //   v = ((y - offsetY) + 0.5) / scale - 0.5
        // where u,v are in "virtual source" coordinates (after optional rotation).

        for (int y = 0; y < scaledH; y++)
        {
            int dy = offsetY + y;
            if ((uint)dy >= (uint)height) continue;

            float v = ((y + 0.5f) / scale) - 0.5f;

            for (int x = 0; x < scaledW; x++)
            {
                int dx = offsetX + x;
                if ((uint)dx >= (uint)width) continue;

                float u = ((x + 0.5f) / scale) - 0.5f;

                PixelF sample = SampleBilinearVirtual(source, u, v, rotate90);

                int di = dst.IndexOf(dx, dy);
                dst.Pixels[di] = sample;
                dst.Mask[di] = 1; // content
            }
        }

        return dst;
    }

    private static float FitScale(int srcW, int srcH, int dstW, int dstH)
    {
        float sx = (float)dstW / srcW;
        float sy = (float)dstH / srcH;
        return MathF.Min(sx, sy);
    }

    // Bilinear sampling in "virtual source coordinates":
    // - If rotate90 == false: virtual (u,v) maps to original (u,v)
    // - If rotate90 == true : virtual (u,v) maps to original coordinates with a 90° rotation.
    //
    // Here we define rotate90 as "rotate source 90° CW to fit" (virtual image is rotated).
    // Mapping virtual -> original:
    //   virtual width  = source.Height
    //   virtual height = source.Width
    //   virtual (u,v) corresponds to original:
    //     ox = u' -> original x = v
    //     oy = v' -> original y = (source.Height - 1) - u
    //
    // This is CW rotation: (x,y) in original -> (y, W-1-x) in rotated.
    // We need inverse: (x',y') in rotated -> (x,y) in original:
    //   x = (W-1) - y'
    //   y = x'
    // but careful with W/H. We'll compute directly with floats.

    private static PixelF SampleBilinearVirtual(ImageBuffer src, float u, float v, bool rotate90)
    {
        // Convert virtual coords (u,v) -> original coords (ox,oy)
        float ox, oy;
        if (!rotate90)
        {
            ox = u;
            oy = v;
        }
        else
        {
            // virtual size: (src.Height, src.Width)
            // CW rotation: rotatedX = originalY, rotatedY = (src.Width - 1) - originalX
            // inverse:
            // originalX = (src.Width - 1) - rotatedY
            // originalY = rotatedX
            // Here rotatedX=u, rotatedY=v (virtual coords)
            ox = (src.Width - 1) - v;
            oy = u;
        }

        return SampleBilinearOriginal(src, ox, oy);
    }

    private static PixelF SampleBilinearOriginal(ImageBuffer src, float x, float y)
    {
        // Clamp to edge (simplest, good enough for resize)
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        if (x > src.Width - 1) x = src.Width - 1;
        if (y > src.Height - 1) y = src.Height - 1;

        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        int x1 = Math.Min(x0 + 1, src.Width - 1);
        int y1 = Math.Min(y0 + 1, src.Height - 1);

        float tx = x - x0;
        float ty = y - y0;

        PixelF p00 = src.Pixels[src.IndexOf(x0, y0)];
        PixelF p10 = src.Pixels[src.IndexOf(x1, y0)];
        PixelF p01 = src.Pixels[src.IndexOf(x0, y1)];
        PixelF p11 = src.Pixels[src.IndexOf(x1, y1)];

        // bilinear: lerp(lerp(p00,p10,tx), lerp(p01,p11,tx), ty)
        PixelF a = Lerp(p00, p10, tx);
        PixelF b = Lerp(p01, p11, tx);
        return Lerp(a, b, ty);
    }

    private static PixelF Lerp(PixelF a, PixelF b, float t)
    {
        return new PixelF
        {
            R = a.R + (b.R - a.R) * t,
            G = a.G + (b.G - a.G) * t,
            B = a.B + (b.B - a.B) * t
        };
    }
}