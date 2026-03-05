using System.Reflection.Metadata;

namespace EsDitherer.Core.Quantizers;

public class YCbCrQuantizer(PixelF[] palette) :  QuantizerBase(palette)
{
    public float WeightY { get; set; } = 1.0f;
    public float WeightCr { get; set; } = 0.7f;
    public float WeightCb { get; set; } = 0.3f;
    
    public override int Quantize(PixelF p)
    {
        var src = PixelY.FromRGB(p);

        var distances = Palette.Select(PixelY.FromRGB).Select(
            py => WeightY * Math.Pow((py.Y - src.Y), 2) + WeightCb * Math.Pow((py.Cb - src.Cb), 2) + WeightCr * Math.Pow((py.Cr - src.Cr), 2)
            ).ToList();

        var nearest = distances.Min();
        return distances.IndexOf(nearest);
    }
}