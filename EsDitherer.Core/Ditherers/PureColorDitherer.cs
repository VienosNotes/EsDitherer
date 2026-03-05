using System.ComponentModel;

namespace EsDitherer.Core.Ditherers;

public class PureColorDitherer : IDitherer
{
    public CommittableImageBuffer Dither(ImageBuffer src, IQuantizer quantizer)
    {
        var output = new CommittableImageBuffer(src.Width, src.Height)
        {
            InactiveColorIndex =  src.InactiveColorIndex,
        };
        
        var serialLength = src.Width * src.Height;
        for (var sPos = 0; sPos < serialLength; sPos++)
        {
            if (src.Mask[sPos] == MaskValue.Inactive)
            {
                output.Pixels[sPos] = src.Pixels[sPos];
            }
            else
            {
                var paletteIndex = quantizer.Quantize(src.Pixels[sPos]);
                output.Pixels[sPos] = quantizer.GetColor(paletteIndex);
                output.PaletteIndices[(sPos/src.Width)*src.Width + (src.Width - (sPos%src.Width) -1)] = (byte)paletteIndex;
            }

            output.Mask[sPos] = src.Mask[sPos];
        }
        
        return output;
    }
}