using DataCenterManagement.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using System.Windows;

namespace DataCenterManagement.Services
{
    public static class WordExporter
    {
        public static void SetDateBookmarks(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path không được null hoặc rỗng.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            var today = DateOnly.FromDateTime(DateTime.Now);
            var currentMonth = today.Month;
            var currentYear = today.Year;

            // Tính ngày đầu tuần (Thứ 2) và cuối tuần (Chủ nhật)
            var startOfWeek = GetStartOfWeek(today);
            var endOfWeek = startOfWeek.AddDays(6);

            using (var doc = WordprocessingDocument.Open(filePath, true))
            {
                var mainPart = doc.MainDocumentPart ?? throw new InvalidDataException("Document không có MainDocumentPart.");

                if (mainPart.Document?.Body == null)
                    throw new InvalidDataException("Document không có Body hợp lệ.");

                // Gán giá trị cho các bookmark
                SetBookmarkValue(mainPart, "date", today.ToString("dd"));
                SetBookmarkValue(mainPart, "month", today.ToString("MM"));
                SetBookmarkValue(mainPart, "year", today.ToString("yyyy"));
                SetBookmarkValue(mainPart, "from", startOfWeek.ToString("dd/MM/yyyy"));
                SetBookmarkValue(mainPart, "to", endOfWeek.ToString("dd/MM/yyyy"));

                mainPart.Document.Save();
            }
        }

        private static DateOnly GetStartOfWeek(DateOnly date)
        {
            // Tính ngày đầu tuần (Thứ 2)
            var daysFromMonday = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysFromMonday < 0) daysFromMonday += 7; // Xử lý trường hợp Chủ nhật
            return date.AddDays(-daysFromMonday);
        }

        private static void SetBookmarkValue(MainDocumentPart mainPart, string bookmarkName, string value)
        {
            var bookmarkStart = mainPart.Document.Body?
                .Descendants<BookmarkStart>()
                .FirstOrDefault(b => !string.IsNullOrEmpty(b.Name?.Value) && b.Name.Value == bookmarkName);

            if (bookmarkStart == null) return; // Bỏ qua nếu không tìm thấy bookmark

            var bId = bookmarkStart.Id?.Value;
            if (bId == null) return;

            var bookmarkEnd = mainPart.Document.Body?
                .Descendants<BookmarkEnd>()
                .FirstOrDefault(be => be.Id?.Value == bId);

            // Xóa content cũ giữa bookmark start và end
            OpenXmlElement? current = bookmarkStart.NextSibling();
            while (current != null && current != bookmarkEnd)
            {
                var toRemove = current;
                current = current.NextSibling();
                toRemove.Remove();
            }

            // Tạo run mới với Times New Roman, size 14
            var runProps = new RunProperties();
            runProps.Append(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });
            runProps.Append(new FontSize { Val = "28" }); // 14pt = 28 half-points
            runProps.Append(new FontSizeComplexScript { Val = "28" });
            runProps.Append(new Bold());
            runProps.Append(new Italic());

            var run = new Run();
            run.Append(runProps);
            run.Append(new Text(value) { Space = SpaceProcessingModeValues.Preserve });

            // Chèn run mới ngay sau bookmark start
            bookmarkStart.InsertAfterSelf(run);
        }

        public static void ExportWeekToWord_AtBookmark_Formatted(string templatePath, string outputPath, string bookmarkName, IEnumerable<LichTrucRow> rows)
        {
            // Kiểm tra null parameters
            if (string.IsNullOrWhiteSpace(templatePath))
                throw new ArgumentException("Template path không được null hoặc rỗng.", nameof(templatePath));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path không được null hoặc rỗng.", nameof(outputPath));

            if (string.IsNullOrWhiteSpace(bookmarkName))
                throw new ArgumentException("Bookmark name không được null hoặc rỗng.", nameof(bookmarkName));

            ArgumentNullException.ThrowIfNull(rows);

            if (!File.Exists(templatePath))
                throw new FileNotFoundException("Template file not found.", templatePath);

            // copy template thành file output (ghi đè nếu cần)
            File.Copy(templatePath, outputPath, overwrite: true);

            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                var mainPart = doc.MainDocumentPart ?? throw new InvalidDataException("Template không có MainDocumentPart.");

                // Kiểm tra null cho Document và Body
                if (mainPart.Document?.Body == null)
                    throw new InvalidDataException("Template không có Document Body hợp lệ.");

                // Tìm BookmarkStart theo tên (Name)
                var bookmarkStart = mainPart.Document.Body
                    .Descendants<BookmarkStart>()
                    .FirstOrDefault(b => !string.IsNullOrEmpty(b.Name?.Value) && b.Name.Value == bookmarkName) ?? throw new InvalidOperationException($"Bookmark '{bookmarkName}' không tìm thấy trong template.");
                var bId = (bookmarkStart.Id?.Value) ?? throw new InvalidOperationException($"Bookmark '{bookmarkName}' không có ID hợp lệ.");
                var bookmarkEnd = mainPart.Document.Body
                    .Descendants<BookmarkEnd>()
                    .FirstOrDefault(be => be.Id?.Value == bId);

                // Xoá content cũ giữa start và end nếu có
                OpenXmlElement? current = bookmarkStart.NextSibling();
                while (current != null && current != bookmarkEnd)
                {
                    var toRemove = current;
                    current = current.NextSibling();
                    toRemove.Remove();
                }

                // Tạo bảng
                var table = new Table();

                // Table properties with spacing before/after = 0 and column widths
                var tblProps = new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" }, // 100% width
                    new TableLayout { Type = TableLayoutValues.Fixed },
                    new TableBorders(
                        new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 },
                        new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6 }
                    ),
                    new TableCellSpacing { Width = "0", Type = TableWidthUnitValues.Dxa }
                );
                table.AppendChild(tblProps);

                // Table Grid - define column widths (33% each)
                var tblGrid = new TableGrid(
                    new GridColumn { Width = "1667" }, // ~33% of 5000
                    new GridColumn { Width = "1667" }, // ~33% of 5000
                    new GridColumn { Width = "1666" }  // ~33% of 5000
                );
                table.AppendChild(tblGrid);

                // Header row
                var headerRow = new TableRow();
                headerRow.Append(
                    CreateCell("Thứ", isHeader: true, alignCenter: true),
                    CreateCell("Ca 1 - 3", isHeader: true, alignCenter: true),
                    CreateCell("Ca 2 - 4", isHeader: true, alignCenter: true)
                );
                table.AppendChild(headerRow);

                // Xử lý từng row với null safety
                foreach (var r in rows)
                {
                    if (r == null) continue; // Bỏ qua row null

                    string ngayDisplay;
                    try
                    {
                        // Nếu r.Ngay là DateOnly
                        ngayDisplay = $"{ToVietnameseWeekday(r.Ngay)} ({r.Ngay:dd/MM})";
                    }
                    catch
                    {
                        // fallback - xử lý trường hợp r.Ngay có thể null
                        ngayDisplay = r.Ngay.ToString() ?? "N/A";
                    }

                    var ten13 = r.TenCa13 ?? string.Empty;
                    var ten24 = r.TenCa24 ?? string.Empty;

                    var dataRow = new TableRow();
                    dataRow.Append(
                        CreateCell(ngayDisplay, isHeader: false, alignCenter: true),
                        CreateCell(ten13, isHeader: false, alignCenter: true),
                        CreateCell(ten24, isHeader: false, alignCenter: true)
                    );
                    table.AppendChild(dataRow);
                }

                // Chèn bảng ngay sau Paragraph chứa bookmarkStart nếu có
                var parentParagraph = bookmarkStart.Ancestors<Paragraph>().FirstOrDefault();
                if (parentParagraph != null)
                {
                    parentParagraph.InsertAfterSelf(table);
                }
                else if (bookmarkEnd != null)
                {
                    mainPart.Document.Body.InsertBefore(table, bookmarkEnd);
                }
                else
                {
                    mainPart.Document.Body.AppendChild(table);
                }

                mainPart.Document.Save();
            }
        }

        private static TableCell CreateCell(string? text, bool isHeader = false, bool alignCenter = false)
        {
            // Run properties with Times New Roman font, size 14
            var runProps = new RunProperties();
            if (isHeader) runProps.Append(new Bold());

            // Font settings
            runProps.Append(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });
            runProps.Append(new FontSize { Val = "28" }); // Font size 14 = 28 half-points
            runProps.Append(new FontSizeComplexScript { Val = "28" }); // For complex scripts

            var run = new Run();
            run.Append(runProps);

            // Xử lý null text
            var safeText = text ?? string.Empty;
            run.Append(new Text(safeText) { Space = SpaceProcessingModeValues.Preserve });

            // Paragraph properties with spacing before/after = 0
            var paraProps = new ParagraphProperties();
            paraProps.Append(new Justification() { Val = alignCenter ? JustificationValues.Center : JustificationValues.Left });
            paraProps.Append(new SpacingBetweenLines
            {
                Before = "0",
                After = "0",
                Line = "240", // Single line spacing
                LineRule = LineSpacingRuleValues.Auto
            });

            var para = new Paragraph();
            para.Append(paraProps);
            para.Append(run);

            // Table cell properties with preferred width 33%
            var tcProps = new TableCellProperties();
            tcProps.Append(new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1667" }); // ~33%
            tcProps.Append(new TableCellMarginDefault(
                new TopMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                new BottomMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                new StartMargin { Width = "120", Type = TableWidthUnitValues.Dxa },
                new EndMargin { Width = "120", Type = TableWidthUnitValues.Dxa }
            ));

            var cell = new TableCell(para);
            cell.Append(tcProps);
            return cell;
        }

        private static string ToVietnameseWeekday(DateOnly d)
        {
            return d.DayOfWeek switch
            {
                DayOfWeek.Monday => "Thứ hai",
                DayOfWeek.Tuesday => "Thứ ba",
                DayOfWeek.Wednesday => "Thứ tư",
                DayOfWeek.Thursday => "Thứ năm",
                DayOfWeek.Friday => "Thứ sáu",
                DayOfWeek.Saturday => "Thứ bảy",
                DayOfWeek.Sunday => "Chủ nhật",
                _ => d.DayOfWeek.ToString()
            };
        }
    }
}