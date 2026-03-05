namespace EsDitherer.Core.Ditherers;

public class FloSteDitherer : IDitherer
{
    public CommittableImageBuffer Dither(ImageBuffer src, IQuantizer quantizer)
    {
        var result = new CommittableImageBuffer(src.Width, src.Height)
        {
            InactiveColorIndex = src.InactiveColorIndex
        };

        Array.Copy(src.Pixels, 0, result.Pixels, 0,  src.Pixels.Length);
        Array.Copy(src.Mask, 0, result.Mask, 0, src.Mask.Length);

        for (var y = 0; y < src.Height; y++)
        {
            for (var x = 0; x < src.Width; x++)
            {
                if (src.GetMask(x, y) == MaskValue.Inactive)
                {
                    result.PaletteIndices[y*src.Width + x] = src.InactiveColorIndex;
                    continue;
                }
                
                var oldp = result[x, y];
                var qIndex = quantizer.Quantize(oldp);
                var qColor = quantizer.GetColor(qIndex);

                result[x, y] = qColor;
                result.PaletteIndices[y*src.Width + (src.Width - x - 1)] = (byte)qIndex;
                
                var diff = oldp.GetDiffFrom(qColor);

                if (src.GetMask(x + 1, y) == MaskValue.Active)
                {
                    var qError = diff.Multiply(7.0f / 16.0f);
                    result[x+1, y] = result[x+1, y].Add(qError);
                }
                
                if (src.GetMask(x - 1, y + 1) == MaskValue.Active)
                {
                    var qError = diff.Multiply(3.0f / 16.0f);
                    result[x-1, y+1] = result[x-1, y+1].Add(qError);
                }
                
                if (src.GetMask(x, y + 1) == MaskValue.Active)
                {
                    var qError = diff.Multiply(5.0f / 16.0f);
                    result[x, y+1] = result[x, y+1].Add(qError);
                }
                
                if (src.GetMask(x + 1, y + 1) == MaskValue.Active)
                {
                    var qError = diff.Multiply(1.0f / 16.0f);
                    result[x+1, y+1] = result[x+1, y+1].Add(qError);
                }
                
                
            }
            
        }

        return result;

    }
}