using ImageMagick;
using ImageMagick.Drawing;

namespace HandballBackend.EndpointHelpers;

public static class ImageHelper {
    private static readonly FileInfo CircleOutline = new("./resources/images/circle_outline.png");

    public static string SaveImageWithCircle(Stream otherFile, string searchableName = "test") {
        var circleOutline = new MagickImage(CircleOutline);
        using var imageIn = new MagickImage(otherFile);
        var w = imageIn.Width;
        var h = imageIn.Height;
        var finalSize = circleOutline.Height;
        // we want to resize so that the smaller of the two sizes is correct (otherwise we will get gaps)
        if (w > h) {
            imageIn.Resize(0, finalSize);
        } else {
            imageIn.Resize(finalSize, 0);
        }

        imageIn.Crop(circleOutline.Width, finalSize, Gravity.Center);

        imageIn.ResetPage();

        //use a mask to eliminate the corner bits
        using var mask = new MagickImage(MagickColors.Transparent, finalSize, finalSize);

        //luciferian magic to make the circle look nice (I don't know how it works)
        new Drawables().FillColor(MagickColors.White)
            .Circle(finalSize / 2.0, finalSize / 2.0, (finalSize - 70) / 2.0, 10)
            .Draw(mask);
        mask.Alpha(AlphaOption.Extract);
        imageIn.Composite(mask, CompositeOperator.CopyAlpha);

        // Make the background transparent
        imageIn.Alpha(AlphaOption.Set);
        imageIn.BackgroundColor = MagickColors.Transparent;
        imageIn.Evaluate(Channels.Alpha, EvaluateOperator.Multiply, 1);

        using var images = new MagickImageCollection(); //used so we can composite the two images
        images.Add(imageIn);
        images.Add(circleOutline);

        using var result = images.Mosaic();
        result.Write($"./resources/images/big/users/{searchableName}.png");
        result.Resize(200, 200);
        result.Write($"./resources/images/users/{searchableName}.png");
        return $"/api/people/image?name={searchableName}";
    }
}