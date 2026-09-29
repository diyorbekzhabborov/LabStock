using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using LabStock.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabStock.Services
{
    public class ExportService
    {
        private static ExportService? _instance;
        public static ExportService Instance => _instance ??= new ExportService();

        static ExportService()
        {
            // Set QuestPDF license for community use
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string GenerateDailyReportFileName(DateTime date, string extension)
        {
            return $"Отчёт_Лаборатория_День_{date:yyyy-MM-dd}.{extension}";
        }

        public string GenerateMonthlyReportFileName(int year, int month, string extension)
        {
            var date = new DateTime(year, month, 1);
            return $"Отчёт_Лаборатория_{date:MMMM_yyyy}.{extension}";
        }

        public string GenerateMovementsFileName(string extension)
        {
            return $"Журнал_Движения_Склада_{DateTime.Now:yyyyMMdd_HHmm}.{extension}";
        }

        // ==========================================
        // EXCEL EXPORTS (ClosedXML)
        // ==========================================
        public void ExportDailyReportToExcel(DailyReportDto report, string filePath)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Дневной отчёт");

            // Title
            ws.Cell("A1").Value = "Медицинская лаборатория «LabStock»";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#0284C7");

            ws.Cell("A2").Value = $"Ежедневный отчёт за {report.Date:dd.MM.yyyy}";
            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 12;

            ws.Cell("A3").Value = $"Всего выполнено анализов: {report.TotalAnalysesPerformed}";
            ws.Cell("A3").Style.Font.Italic = true;

            int row = 5;
            // Headers
            var headers = new[] { "Материал", "Ед. изм.", "Начальный остаток", "Поступления", "Расход (анализы)", "Ручные списания", "Конечный остаток" };
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(row, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0284C7");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            row++;
            foreach (var item in report.Items)
            {
                ws.Cell(row, 1).Value = item.MaterialName;
                ws.Cell(row, 2).Value = item.Unit;
                ws.Cell(row, 3).Value = (double)item.InitialStock;
                ws.Cell(row, 4).Value = (double)item.Receipts;
                ws.Cell(row, 5).Value = (double)item.AnalysisConsumption;
                ws.Cell(row, 6).Value = (double)item.ManualWriteoffs;
                ws.Cell(row, 7).Value = (double)item.FinalStock;

                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

                for (int c = 1; c <= 7; c++)
                {
                    ws.Cell(row, c).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    ws.Cell(row, c).Style.Border.BottomBorderColor = XLColor.FromHtml("#E2E8F0");
                }
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public void ExportMonthlyReportToExcel(MonthlyReportDto report, string filePath)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Ежемесячный отчёт");

            ws.Cell("A1").Value = "Медицинская лаборатория «LabStock»";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#0284C7");

            ws.Cell("A2").Value = $"Ежемесячный отчёт за {report.MonthName}";
            ws.Cell("A2").Style.Font.Bold = true;
            ws.Cell("A2").Style.Font.FontSize = 13;

            ws.Cell("A3").Value = $"Всего анализов: {report.TotalAnalyses} | Стоимость закупок: {report.TotalReceiptsCost:N2} сомони | Оценка склада: {report.TotalWarehouseValuation:N2} сомони";
            ws.Cell("A3").Style.Font.Italic = true;

            int row = 5;
            ws.Cell(row, 1).Value = "Топ расходуемых материалов:";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            var topHeaders = new[] { "Материал", "Категория", "Израсходовано", "Ед." };
            for (int col = 0; col < topHeaders.Length; col++)
            {
                var cell = ws.Cell(row, col + 1);
                cell.Value = topHeaders[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0369A1");
                cell.Style.Font.FontColor = XLColor.White;
            }

            row++;
            foreach (var top in report.TopMaterials)
            {
                ws.Cell(row, 1).Value = top.MaterialName;
                ws.Cell(row, 2).Value = top.Category;
                ws.Cell(row, 3).Value = (double)top.QuantityUsed;
                ws.Cell(row, 4).Value = top.Unit;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }

            row += 2;
            ws.Cell(row, 1).Value = "Динамика по дням месяца:";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            var timeHeaders = new[] { "Дата", "Анализов выполнено", "Списано расходников" };
            for (int col = 0; col < timeHeaders.Length; col++)
            {
                var cell = ws.Cell(row, col + 1);
                cell.Value = timeHeaders[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D9488");
                cell.Style.Font.FontColor = XLColor.White;
            }

            row++;
            foreach (var day in report.DailyTimeline)
            {
                ws.Cell(row, 1).Value = day.Date.ToString("dd.MM.yyyy");
                ws.Cell(row, 2).Value = day.AnalysesCount;
                ws.Cell(row, 3).Value = (double)day.MaterialsUsed;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public void ExportStockMovementsToExcel(List<StockMovement> movements, string filePath)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Движение склада");

            ws.Cell("A1").Value = "Журнал движения складских запасов";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;

            int row = 3;
            var headers = new[] { "Дата/Время", "Материал", "Партия", "Операция", "Количество", "Остаток после", "Пользователь", "Документ", "Примечание" };
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(row, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0284C7");
                cell.Style.Font.FontColor = XLColor.White;
            }

            row++;
            foreach (var m in movements)
            {
                ws.Cell(row, 1).Value = m.Date.ToString("dd.MM.yyyy HH:mm");
                ws.Cell(row, 2).Value = m.Material?.Name ?? "";
                ws.Cell(row, 3).Value = m.Lot?.LotNumber ?? "-";
                ws.Cell(row, 4).Value = m.OperationType.ToFriendlyName();
                ws.Cell(row, 5).Value = (double)m.Quantity;
                ws.Cell(row, 6).Value = (double)m.BalanceAfter;
                ws.Cell(row, 7).Value = m.User?.FullName ?? "";
                ws.Cell(row, 8).Value = m.ReferenceNumber ?? "";
                ws.Cell(row, 9).Value = m.Notes ?? "";

                ws.Cell(row, 5).Style.NumberFormat.Format = "+#,##0.00;-#,##0.00;0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        // ==========================================
        // PDF EXPORTS (QuestPDF)
        // ==========================================
        public void ExportDailyReportToPdf(DailyReportDto report, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("LabStock — Медицинская лаборатория").FontSize(16).Bold().FontColor("#0284C7");
                                c.Item().Text($"Ежедневный складской отчёт за {report.Date:dd.MM.yyyy}").FontSize(12).SemiBold().FontColor("#334155");
                            });
                            row.ConstantItem(120).AlignRight().Text($"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(8).FontColor("#64748B");
                        });
                        col.Item().PaddingVertical(5).LineHorizontal(1).LineColor("#CBD5E1");
                    });

                    page.Content().Column(col =>
                    {
                        // Stats summary
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                            {
                                c.Item().Text("Выполнено анализов").FontSize(8).FontColor("#64748B");
                                c.Item().Text(report.TotalAnalysesPerformed.ToString()).FontSize(14).Bold().FontColor("#0284C7");
                            });
                            row.Spacing(10);
                            row.RelativeItem().Border(1).BorderColor("#E2E8F0").Padding(8).Column(c =>
                            {
                                c.Item().Text("Позиций в отчёте").FontSize(8).FontColor("#64748B");
                                c.Item().Text(report.Items.Count.ToString()).FontSize(14).Bold().FontColor("#0D9488");
                            });
                        });

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Название
                                columns.ConstantColumn(40); // Ед.
                                columns.RelativeColumn(1); // Нач.
                                columns.RelativeColumn(1); // Приход
                                columns.RelativeColumn(1); // Анализ
                                columns.RelativeColumn(1); // Списание
                                columns.RelativeColumn(1); // Кон.
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#0284C7").Padding(5).Text("Материал").FontColor("#FFFFFF").Bold();
                                header.Cell().Background("#0284C7").Padding(5).Text("Ед.").FontColor("#FFFFFF").Bold();
                                header.Cell().Background("#0284C7").Padding(5).AlignRight().Text("Начало").FontColor("#FFFFFF").Bold();
                                header.Cell().Background("#0284C7").Padding(5).AlignRight().Text("Приход").FontColor("#FFFFFF").Bold();
                                header.Cell().Background("#0284C7").Padding(5).AlignRight().Text("Анализ").FontColor("#FFFFFF").Bold();
                                header.Cell().Background("#0284C7").Padding(5).AlignRight().Text("Списано").FontColor("#FFFFFF").Bold();
                                header.Cell().Background("#0284C7").Padding(5).AlignRight().Text("Остаток").FontColor("#FFFFFF").Bold();
                            });

                            foreach (var item in report.Items)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(item.MaterialName);
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).Text(item.Unit);
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text(item.InitialStock.ToString("0.##"));
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text(item.Receipts > 0 ? $"+{item.Receipts:0.##}" : "-");
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text(item.AnalysisConsumption > 0 ? $"-{item.AnalysisConsumption:0.##}" : "-");
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text(item.ManualWriteoffs > 0 ? $"-{item.ManualWriteoffs:0.##}" : "-");
                                table.Cell().BorderBottom(0.5f).BorderColor("#E2E8F0").Padding(4).AlignRight().Text(item.FinalStock.ToString("0.##")).Bold();
                            }
                        });
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("LabStock — Контроль запасов медицинской лаборатории").FontSize(8).FontColor("#94A3B8");
                        row.ConstantItem(50).AlignRight().Text(x =>
                        {
                            x.Span("Стр. ");
                            x.CurrentPageNumber();
                        });
                    });
                });
            }).GeneratePdf(filePath);
        }

        // ==========================================
        // CSV EXPORT (with UTF-8 BOM)
        // ==========================================
        public void ExportStockMovementsToCsv(List<StockMovement> movements, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Дата/Время;Материал;Партия;Операция;Количество;Остаток после;Пользователь;Документ;Примечание");

            foreach (var m in movements)
            {
                var line = $"{m.Date:dd.MM.yyyy HH:mm};" +
                           $"\"{m.Material?.Name?.Replace("\"", "\"\"")}\";" +
                           $"\"{m.Lot?.LotNumber}\";" +
                           $"{m.OperationType.ToFriendlyName()};" +
                           $"{m.Quantity:0.##};" +
                           $"{m.BalanceAfter:0.##};" +
                           $"\"{m.User?.FullName}\";" +
                           $"\"{m.ReferenceNumber}\";" +
                           $"\"{m.Notes?.Replace("\"", "\"\"")}\"";
                sb.AppendLine(line);
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
