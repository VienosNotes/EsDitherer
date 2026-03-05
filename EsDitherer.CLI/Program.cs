using EsDitherer.Core;
using EsDitherer.Core.Ditherers;
using EsDitherer.Core.Quantizers;
using EsDitherer.Core.Resizers;

namespace EsDitherer;

class Program
{
    static void Main(string[] args)
    {
        var testfile = "lightning.png";
        var rawBuf = ImageBuffer.LoadFromFile($"img/{testfile}");
        var resizer = new FitInsideResizer();
        var resized = resizer.Resize(rawBuf);

        if (!Directory.Exists($"./output_{testfile}"))
        {
            Directory.CreateDirectory($"./output_{testfile}");
        }
        // resized.WriteImage($"output_{testfile}/{DateTime.Now:yyyyMMdd_HHmmss}_resize.png");

        var qzer = new RgbLinearQuantizer(QuantizerBase.EzSign4cPalette.ToArray());
        // var dther = new PureColorDitherer();
        // var dithered = dther.Dither(resized, qzer);
        //
        // dithered.WriteImage($"output_{testfile}/{DateTime.Now:MMdd_HHmmss}_pcd.png");

        var fsdther = new FloSteDitherer();
        var fsdthered = fsdther.Dither(resized, qzer);
        
        fsdthered.WriteImage($"output_{testfile}/{DateTime.Now:MMdd_HHmmss}_fsd.png");
        
        var mfsdther = new ModFloSteDitherer();
        var mfsdthered = mfsdther.Dither(resized, qzer);
        
        mfsdthered.WriteImage($"output_{testfile}/{DateTime.Now:MMdd_HHmmss}_mfsd.png");
        
        // var yq = new YCbCrQuantizer(QuantizerBase.EzSign4cPalette.ToArray());
        // var yfsddthered = fsdther.Dither(resized, yq);
        //
        // yfsddthered.WriteImage($"output_{testfile}/{DateTime.Now:MMdd_HHmmss}_yfsd.png");
    }
}