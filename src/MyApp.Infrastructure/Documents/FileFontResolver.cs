using PdfSharp.Fonts;

namespace MyApp.Infrastructure.Documents;

internal sealed class FileFontResolver(string fontPath) : IFontResolver
{
    private const string FaceName = "MyAppDocumentFont";
    private readonly Lazy<byte[]> _fontData = new(
        () => File.ReadAllBytes(fontPath),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public byte[] GetFont(string faceName) =>
        faceName.Equals(FaceName, StringComparison.Ordinal)
            ? _fontData.Value
            : throw new InvalidOperationException(
                $"Неизвестный шрифт PDF: {faceName}.");

    public FontResolverInfo ResolveTypeface(
        string familyName,
        bool isBold,
        bool isItalic) =>
        new(FaceName);
}
