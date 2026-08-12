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

        var chunks = request.Items.Chunk(ItemsPerDocument).ToArray();
        using var document = PdfReader.Open(
            _templatePath,
            PdfDocumentOpenMode.Modify);
        FillDocument(document, request, chunks[0]);

        for (var pageIndex = 1; pageIndex < chunks.Length; pageIndex++)
        {
            using var pageDocument = PdfReader.Open(
                _templatePath,
                PdfDocumentOpenMode.Modify);
            FillDocument(pageDocument, request, chunks[pageIndex]);
            RenameFields(pageDocument, pageIndex + 1);

            using var pageStream = new MemoryStream();
            pageDocument.Save(pageStream, false);
            pageStream.Position = 0;
            using var importedPageDocument = PdfReader.Open(
                pageStream,
                PdfDocumentOpenMode.Import);
            foreach (var sourcePage in importedPageDocument.Pages)
            {
                var importedPage = document.AddPage(sourcePage);
                RegisterPageFields(document, importedPage);
            }
        }

        document.AcroForm?.Elements.SetBoolean("/NeedAppearances", true);
        using var output = new MemoryStream();
        document.Save(output, false);
        return
        [
            new GeneratedPdfDocument(
                $"components-{request.Date:yyyy-MM-dd}.pdf",
                output.ToArray())
        ];
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
        SetText(document, "job", "Аварийная");
        SetText(document, "d_1", request.IssuerPosition);
        SetText(document, "f_1", request.IssuerName);
        SetText(document, "d_2", request.AuthorPosition);
        SetText(document, "f_2", request.AuthorName);

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

    private static PdfDictionary? ResolveDictionary(PdfItem? item) =>
        item switch
        {
            PdfDictionary dictionary => dictionary,
            PdfReference reference => reference.Value as PdfDictionary,
            _ => null
        };

    private static void RenameFields(
        PdfDocument document,
        int pageNumber)
    {
        var fields = document.AcroForm?.Elements.GetArray("/Fields");
        if (fields is not null)
        {
            RenameFields(fields, pageNumber);
        }
    }

    private static void RenameFields(
        PdfArray fields,
        int pageNumber)
    {
        foreach (var item in fields.Elements)
        {
            var field = ResolveDictionary(item);
            if (field is null)
            {
                continue;
            }

            var name = field.Elements.GetString("/T");
            if (!string.IsNullOrEmpty(name))
            {
                field.Elements.SetString("/T", $"{name}_page_{pageNumber}");
            }

            var children = field.Elements.GetArray("/Kids");
            if (children is not null)
            {
                RenameFields(children, pageNumber);
            }
        }
    }

    private static void RegisterPageFields(
        PdfDocument document,
        PdfPage page)
    {
        var formFields = document.AcroForm?.Elements.GetArray("/Fields");
        var annotations = page.Elements.GetArray("/Annots");
        if (formFields is null || annotations is null)
        {
            return;
        }

        var registeredIds = formFields.Elements
            .OfType<PdfReference>()
            .Select(reference => reference.ObjectID)
            .ToHashSet();
        foreach (var annotationItem in annotations.Elements)
        {
            var field = ResolveDictionary(annotationItem);
            if (field is null)
            {
                continue;
            }

            while (ResolveDictionary(field.Elements["/Parent"]) is { } parent)
            {
                field = parent;
            }

            var reference = field.Reference;
            if (reference is not null && registeredIds.Add(reference.ObjectID))
            {
                formFields.Elements.Add(reference);
            }
        }
    }

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
