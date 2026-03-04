namespace EsDitherer.Core;

public interface IResizer
{
    /// <summary>
    /// 画像データを指定されたサイズの長方形に内接するように変換します。
    /// </summary>
    /// <param name="source"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="allowRotate"></param>
    /// <returns></returns>
    ImageBuffer Resize(ImageBuffer source, int width, int height, bool allowRotate);
}

public static class ResizerExtensions
{
    /// <summary>
    /// 画像データを既定の条件でリサイズします。
    /// </summary>
    /// <param name="resizer"></param>
    /// <param name="source"></param>
    /// <returns></returns>
    public static ImageBuffer Resize(this IResizer resizer, ImageBuffer source)
    {
        return resizer.Resize(source, 400, 300, true);
    }
}

public interface IQuantizer
{
    public IReadOnlyList<PixelF> Palette { get; }
    
    public int Quantize(PixelF p);
    
    public PixelF GetColor(int index) {
        return Palette[index];
    }
}

public interface IDitherer
{
    public ImageBuffer Dither(ImageBuffer src, IQuantizer quantizer);
}