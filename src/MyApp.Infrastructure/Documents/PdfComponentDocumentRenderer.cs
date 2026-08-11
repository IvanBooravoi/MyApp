using System.Globalization;
using MyApp.Application.Abstractions;
using MyApp.Application.DTO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace MyApp.Infrastructure.Documents;

public sealed class PdfComponentDocumentRenderer : IComponentDocumentRenderer
{
    private const int ItemsPerDocument = 5;
    private readonly string _templatePath;

    public PdfComponentDocumentRenderer(string templatePath)
    {
        _templatePath = templatePath;
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
        var fields = document.AcroForm?.Elements.GetArray("/Fields");
        var field = fields is null
            ? null
            : FindField(fields, fieldName);
        if (field is null)
        {
            throw new InvalidDataException(
                $"В PDF-шаблоне отсутствует текстовое поле '{fieldName}'.");
        }

        field.Elements["/V"] = new PdfString(value, PdfStringEncoding.Unicode);
        field.Elements.SetInteger(
            "/Ff",
            field.Elements.GetInteger("/Ff") | 1);
        RemoveAppearance(field);
    }

    private static PdfDictionary? FindField(
        PdfArray fields,
        string fieldName)
    {
        foreach (var item in fields.Elements)
        {
            var field = ResolveDictionary(item);
            if (field is null)
            {
                continue;
            }

            if (field.Elements.GetString("/T").Equals(
                fieldName,
                StringComparison.Ordinal))
            {
                return field;
            }

            var children = field.Elements.GetArray("/Kids");
            if (children is not null)
            {
                var match = FindField(children, fieldName);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    private static PdfDictionary? ResolveDictionary(PdfItem item) =>
        item switch
        {
            PdfDictionary dictionary => dictionary,
            PdfReference reference => reference.Value as PdfDictionary,
            _ => null
        };

    private static void RemoveAppearance(PdfDictionary field)
    {
        field.Elements.Remove("/AP");
        var children = field.Elements.GetArray("/Kids");
        if (children is null)
        {
            return;
        }

        foreach (var childItem in children.Elements)
        {
            var child = ResolveDictionary(childItem);
            if (child is not null)
            {
                RemoveAppearance(child);
            }
        }
    }
}
