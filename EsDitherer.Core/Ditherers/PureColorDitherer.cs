using System.ComponentModel;

namespace EsDitherer.Core.Ditherers;

public class PureColorDitherer : IDitherer
{
    public ImageBuffer Dither(ImageBuffer src, IQuantizer quantizer)
    {
        var output = new ImageBuffer(src.Width, src.Height);
        
        var serialLength = src.Width * src.Height;
        for (var sPos = 0; sPos < serialLength; sPos++)
        {
            if (src.Mask[sPos] == MaskValue.Inactive)
            {
                output.Pixels[sPos] = src.Pixels[sPos];
            }
            else
            {
                output.Pixels[sPos] = quantizer.GetColor(quantizer.Quantize(src.Pixels[sPos]));
            }

            output.Mask[sPos] = src.Mask[sPos];
        }
        
        return output;
    }
}