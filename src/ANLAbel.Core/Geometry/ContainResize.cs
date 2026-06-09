namespace ANLAbel.Core.Geometry;

public readonly record struct ContainResizeResult(
    double Scale,
    double RenderWidth,
    double RenderHeight,
    double OffsetX,
    double OffsetY);

public static class ContainResize
{
    public static ContainResizeResult Calculate(
        double containerWidth,
        double containerHeight,
        double contentWidth,
        double contentHeight)
    {
        ValidatePositive(containerWidth, nameof(containerWidth));
        ValidatePositive(containerHeight, nameof(containerHeight));
        ValidatePositive(contentWidth, nameof(contentWidth));
        ValidatePositive(contentHeight, nameof(contentHeight));

        var scale = Math.Min(containerWidth / contentWidth, containerHeight / contentHeight);
        var renderWidth = contentWidth * scale;
        var renderHeight = contentHeight * scale;
        var offsetX = (containerWidth - renderWidth) / 2;
        var offsetY = (containerHeight - renderHeight) / 2;

        return new ContainResizeResult(scale, renderWidth, renderHeight, offsetX, offsetY);
    }

    private static void ValidatePositive(double value, string parameterName)
    {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Value must be a positive finite number.");
        }
    }
}