using ImageMagick;
using ImageMagick.Drawing;

namespace HandballBackend.EndpointHelpers;

public static class ImageHelper {
    private static readonly FileInfo CircleOutline = new("./resources/images/circle_outline.png");

    public static string SaveImageWithCircle(Stream otherFile, string searchableName = "test") {
        using var images = new MagickImageCollection();
        var circleOutline = new MagickImage(CircleOutline);
        using var imageIn = new MagickImage(otherFile);
        images.Add(imageIn);
        var w = imageIn.Width;
        var h = imageIn.Height;
        var finalSize = circleOutline.Height;
        if (w > h) {
            imageIn.Resize(0, finalSize);
        } else {
            imageIn.Resize(finalSize, 0);
        }

        imageIn.Crop(circleOutline.Width, finalSize, Gravity.Center);
        imageIn.ResetPage();
        using var mask = new MagickImage(MagickColors.Transparent, finalSize, finalSize);
        new Drawables().FillColor(MagickColors.White)
            .Circle(finalSize / 2.0, finalSize / 2.0, (finalSize - 70) / 2.0, 10)
            .Draw(mask);
        mask.Alpha(AlphaOption.Extract);
        imageIn.Composite(mask, CompositeOperator.CopyAlpha);

        // Make the background transparent
        imageIn.Alpha(AlphaOption.Set);
        imageIn.BackgroundColor = MagickColors.Transparent;
        imageIn.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, 1);
        images.Add(circleOutline);

        using var result = images.Mosaic();
        result.Write($"./resources/images/big/users/{searchableName}.png");
        result.Resize(200, 200);
        result.Write($"./resources/images/users/{searchableName}.png");
        return $"/api/people/image?name={searchableName}";
    }
}