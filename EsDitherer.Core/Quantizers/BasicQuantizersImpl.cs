namespace EsDitherer.Core.Quantizers;

public class RgbLinearQuantizer(PixelF[] palette) : QuantizerBase(palette)
{
    public override int Quantize(PixelF p)
    {
        var distances = Palette.Select(palletColor => 
            Math.Sqrt(
                Math.Pow(palletColor.R - p.R, 2) +
                Math.Pow(palletColor.G - p.G, 2) +
                Math.Pow(palletColor.B - p.B, 2))).ToList();

        var nearest = distances.Min();
        return distances.IndexOf(nearest);
    }
}