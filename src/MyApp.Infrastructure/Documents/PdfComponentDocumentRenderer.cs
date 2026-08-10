using System.Globalization;
using MyApp.Application.Abstractions;
using MyApp.Application.DTO;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.AcroForms;
using PdfSharp.Pdf.IO;

namespace MyApp.Infrastructure.Documents;

public sealed class PdfComponentDocumentRenderer : IComponentDocumentRenderer
{
    private const int ItemsPerDocument = 5;
    private readonly string _templatePath;
    private readonly string _fontPath;

    public PdfComponentDocumentRenderer(
        string templatePath,
        string fontPath)
    {
        _templatePath = templatePath;
        _fontPath = fontPath;
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new FileFontResolver(fontPath);
        }
    }

    public IReadOnlyList<GeneratedPdfDocument> Render(
        ComponentDocumentRequest request)
    {
        if (!File.Exists(_templatePath))
        {
            throw new FileNotFoundException(
                $"PDF-шаблон не найден: {_templatePath}",
                _templatePath);
        }

        if (!File.Exists(_fontPath))
        {
            throw new FileNotFoundException(
                $"Шрифт для заполнения PDF не найден: {_fontPath}",
                _fontPath);
        }

        var documents = new List<GeneratedPdfDocument>();
        var chunks = request.Items.Chunk(ItemsPerDocument).ToArray();
        for (var documentIndex = 0; documentIndex < chunks.Length; documentIndex++)
        {
            using var template = PdfReader.Open(
                _templatePath,
                PdfDocumentOpenMode.Modify);
            FillDocument(template, request, chunks[documentIndex]);

            using var output = new MemoryStream();
            template.Save(output, false);
            documents.Add(new GeneratedPdfDocument(
                $"components-{request.Date:yyyy-MM-dd}-{documentIndex + 1}.pdf",
                output.ToArray()));
        }

        return documents;
    }

    private static void FillDocument(
        PdfDocument document,
        ComponentDocumentRequest request,
        IReadOnlyList<ComponentDocumentItem> items)
    {
        if (document.AcroForm is null)
        {
            throw new InvalidDataException(
                "PDF-шаблон не содержит интерактивную форму.");
        }

        SetText(document, "date_n", request.Date.ToString("dd.MM.yyyy"));
        SetText(document, "car_n_1", request.VehicleNumber);

        for (var index = 0; index < ItemsPerDocument; index++)
        {
            var item = index < items.Count ? items[index] : null;
            SetText(document, $"text{index + 1}", item?.Name ?? string.Empty);
            SetText(document, $"u_{index + 1}", item?.Unit ?? string.Empty);
            SetText(
                document,
                $"a_{index + 1}",
                item?.Quantity.ToString(
                    "0.############################",
                    CultureInfo.InvariantCulture) ?? string.Empty);
        }

        document.AcroForm.Elements.SetBoolean("/NeedAppearances", true);
    }

    private static void SetText(
        PdfDocument document,
        string fieldName,
        string value)
    {
        var field = document.AcroForm?.Fields[fieldName];
        if (field is not PdfTextField textField)
        {
            throw new InvalidDataException(
                $"В PDF-шаблоне отсутствует текстовое поле '{fieldName}'.");
        }

        textField.Value = new PdfString(value, PdfStringEncoding.Unicode);
        textField.ReadOnly = true;
        RemoveAppearance(textField);
    }

    private static void RemoveAppearance(PdfAcroField field)
    {
        field.Elements.Remove("/AP");
        if (!field.HasKids)
        {
            return;
        }

        for (var index = 0; index < field.Fields.Count; index++)
        {
            RemoveAppearance(field.Fields[index]);
        }
    }
}
