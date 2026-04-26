using ClosedXML.Excel;

namespace AssistantEngineer.Infrastructure.Integrations.Reports.Excel;

internal static class ExcelWorkbookWriter
{
    public static byte[] SaveToBytes(
        XLWorkbook workbook,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();

        cancellationToken.ThrowIfCancellationRequested();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    public static void WriteHeader(
        IXLWorksheet worksheet,
        params string[] headers)
    {
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }
    }

    public static void FormatTable(
        IXLWorksheet worksheet,
        int columnCount)
    {
        var header = worksheet.Range(1, 1, 1, columnCount);

        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightGray;

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();
    }

    public static void WriteTitle(
        IXLWorksheet worksheet,
        string title)
    {
        worksheet.Cell(1, 1).Value = title;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
    }
}