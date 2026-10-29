namespace ANLAbel.Core.Enums;

public enum ObjectType
{
    Text,
    TextBox,
    Rectangle,
    Ellipse,
    Line,
    BarcodeCode128,
    QRCode,
    DataMatrix,
    Image,
    /// <summary>
    /// NiceLabel 10 Inverse: a frame that inverts black and white of
    /// everything beneath it (lower Z order) in the printed label.
    /// </summary>
    Inverse,
    /// <summary>
    /// NiceLabel 10 Rich Text Box: formatted text (RTF in
    /// <see cref="Models.LabelObject.RichText"/>) owned by its frame and
    /// clipped at it, like a TextBox.
    /// </summary>
    RichTextBox
}
