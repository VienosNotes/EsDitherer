using EsDitherer.Core;
using EsDitherer.Core.Ditherers;
using EsDitherer.Core.Quantizers;
using EsDitherer.Core.Resizers;

namespace EsDitherer;

class Program
{
    static void Main(string[] args)
    {
        var rawBuf = ImageBuffer.LoadFromFile("img/test1.jpg");
        var resizer = new FitInsideResizer();
        var resized = resizer.Resize(rawBuf);

        if (!Directory.Exists("./output"))
        {
            Directory.CreateDirectory("./output");
        }
        resized.WriteImage($"output/{DateTime.Now:yyyyMMdd_HHmmss}_resize.png");

        var qzer = new RgbLinearQuantizer(QuantizerBase.EzSign4cPalette.ToArray());
        var dther = new PureColorDitherer();
        var dithered = dther.Dither(resized, qzer);
        
        dithered.WriteImage($"output/{DateTime.Now:yyyyMMdd_HHmmss}_dithered.png");

    }
}