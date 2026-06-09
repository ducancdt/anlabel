# Contain Resize Example

```csharp
using ANLAbel.Core.Geometry;

var result = ContainResize.Calculate(
    containerWidth: 1280,
    containerHeight: 720,
    contentWidth: 1440,
    contentHeight: 860);

Console.WriteLine($"Scale: {result.Scale:0.###}");
Console.WriteLine($"Render size: {result.RenderWidth:0.##} x {result.RenderHeight:0.##}");
Console.WriteLine($"Offset: {result.OffsetX:0.##}, {result.OffsetY:0.##}");
```

This uses contain behavior: the whole content remains visible, aspect ratio is preserved, and extra space is centered by `OffsetX` / `OffsetY`.