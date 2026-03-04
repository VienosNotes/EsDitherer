using SixLabors.ImageSharp.Processing;

namespace EsDitherer.Core.Quantizers;

public abstract class QuantizerBase(PixelF[] palette) : IQuantizer
{
    public static readonly IReadOnlyList<PixelF> EzSign4cPalette = [
        new PixelF() { R = 0, G = 0, B = 0 },
        new PixelF() { R = 1, G = 1, B = 1 },
        new PixelF() { R = 1, G = 1, B = 0 },
        new PixelF() { R = 1, G = 0, B = 0 }
    ]; 
    
    public IReadOnlyList<PixelF> Palette { get; } = palette;
    public abstract int Quantize(PixelF p);

}