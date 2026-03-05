using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace EsDitherer.Core.Ditherers;

public class ModFloSteDitherer : IDitherer
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

                var py = PixelY.FromRGB(src[x,y]);
                
                // 白黒が選ばれたときのチェック
                if (qIndex < 2)
                {
                    /*
                    if (IsExtremeChroma(src[x, y]))
                    {
                        // 彩度極大の場合は白黒を許さない
                        qIndex = CoerceColor(oldp);
                    }
                    else 
                    */
                    if (IsHighChroma(src[x, y]))
                    {
                        // 彩度大の場合は白黒のうち明度に反するほうを許さない

                        if (py.Y > 0.7 && qIndex == 0 || py.Y < 0.7 && qIndex == 1)
                        {
                            qIndex = CoerceColor(oldp);
                        }
                    }
                }

                // そもそも明度が極端な領域に白黒の逆側は出ない
                if (py.Y > 0.75 && qIndex == 0)
                {
                    qIndex = CoerceColorLight(oldp);
                }
                else if (py.Y < 0.25 && qIndex == 1)
                {
                    qIndex = CoerceColorDark(oldp);
                }
                
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

    private static float GetPseudoChroma(PixelF p)
    {
        return ((float[])[p.R, p.G, p.B]).Max() - ((float[])[p.R, p.G, p.B]).Min();
    }

    private static readonly float HighChromaThreshold = 0.4f;
    
    private static bool IsHighChroma(PixelF p)
    {
        return GetPseudoChroma(p) > HighChromaThreshold;
    }
    
    private static readonly float ExtremeChromaThreshold = 0.9f;

    private static bool IsExtremeChroma(PixelF p)
    {
        return GetPseudoChroma(p) > ExtremeChromaThreshold;
    }

    private static readonly PixelF Black = new PixelF()
    {
        R = 0f, G = 0f, B = 0f
    };
    
    private static readonly PixelF White = new PixelF()
    {
        R = 1f, G = 1f, B = 1f
    };
    
    private static readonly PixelF Yellow = new PixelF()
    {
        R = 1f, G = 1f, B = 0f
    };
    
    private static readonly PixelF Red = new PixelF()
    {
        R = 1f, G = 0f, B = 0f
    };

    private static byte CoerceColor(PixelF p)
    {
        if (p.GetDiffFrom(Yellow).GetNorm() < p.GetDiffFrom(Red).GetNorm())
        {
            return 2;
        }
        else
        {
            return 3;
        }
    }
    
    private static byte CoerceColorDark(PixelF p)
    {
        return new List<(float, byte)> {
            (p.GetDiffFrom(Yellow).GetNorm(), 2),
            (p.GetDiffFrom(Red).GetNorm(), 3),
            (p.GetDiffFrom(Black).GetNorm()/2f, 0),
        }.OrderBy(t => t.Item1).First().Item2;
    }
    
    private static byte CoerceColorLight(PixelF p)
    {
         return new List<(float, byte)> {
            (p.GetDiffFrom(Yellow).GetNorm(), 2),
            (p.GetDiffFrom(Red).GetNorm(), 3),
            (p.GetDiffFrom(White).GetNorm()/2f, 1),
        }.OrderBy(t => t.Item1).First().Item2;
    }
}