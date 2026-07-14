using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Entities.Enums;
using Services.Repositories;
using Web.ViewModels;
using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;
using S = DocumentFormat.OpenXml.Spreadsheet;

namespace Web.Services.Admin;

public class PrincipalReportsService : IPrincipalReportsService
{
    private readonly IAbsenceRepository absenceRepository;
    private readonly IClassRepository classRepository;
    private readonly IFeedbackRepository feedbackRepository;
    private readonly IGradeRepository gradeRepository;
    private readonly IStudentRepository studentRepository;

    public PrincipalReportsService(
        IAbsenceRepository absenceRepository,
        IClassRepository classRepository,
        IFeedbackRepository feedbackRepository,
        IGradeRepository gradeRepository,
        IStudentRepository studentRepository)
    {
        this.absenceRepository = absenceRepository;
        this.classRepository = classRepository;
        this.feedbackRepository = feedbackRepository;
        this.gradeRepository = gradeRepository;
        this.studentRepository = studentRepository;
    }

    public async Task<PrincipalReportsViewModel> BuildReportAsync(
        string? reportType,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? classId,
        Guid? studentId,
        CancellationToken cancellationToken = default)
    {
        var normalizedReportType = NormalizeReportType(reportType);
        var definition = GetReportDefinition(normalizedReportType);
        var classOptions = await GetClassOptionsAsync(cancellationToken);
        var selectedStudent = studentId.HasValue
            ? await studentRepository.GetByIdAsync(studentId.Value, cancellationToken)
            : null;
        var rows = await GetReportRowsAsync(normalizedReportType, fromDate, toDate, classId, studentId, cancellationToken);
        var linePoints = CreateLinePoints(rows, definition.IsGradeReport);
        var selectedClassName = classOptions
            .FirstOrDefault(classOption => classOption.Id == classId)
            ?.Name ?? "Всички класове";

        return new PrincipalReportsViewModel
        {
            ReportType = normalizedReportType,
            ReportTitle = definition.Title,
            ReportDescription = definition.Description,
            MetricLabel = definition.MetricLabel,
            TotalLabel = definition.TotalLabel,
            FromDate = fromDate?.Date,
            ToDate = toDate?.Date,
            ClassId = classId,
            StudentId = selectedStudent?.Id,
            SelectedClassName = selectedClassName,
            SelectedStudentName = selectedStudent is null
                ? null
                : FormatFullName(
                    selectedStudent.User.FirstName,
                    selectedStudent.User.MiddleName,
                    selectedStudent.User.LastName),
            PeriodLabel = FormatPeriodLabel(fromDate, toDate),
            TotalRecords = definition.IsGradeReport ? rows.Sum(row => row.Count) : rows.Sum(row => (int)row.Value),
            AverageValue = rows.Count == 0 ? null : Math.Round(rows.Average(row => row.Value), 2),
            MinimumValue = rows.Count == 0 ? null : rows.Min(row => row.Value),
            MaximumValue = rows.Count == 0 ? null : rows.Max(row => row.Value),
            ReportTypeOptions = CreateReportTypeOptions(),
            ClassOptions = classOptions,
            LinePoints = linePoints,
            YAxisLabels = CreateYAxisLabels(rows, definition.IsGradeReport),
        };
    }

    public async Task<PrincipalReportExportFile> ExportReportAsync(
        string? reportType,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? classId,
        Guid? studentId,
        CancellationToken cancellationToken = default)
    {
        var report = await BuildReportAsync(reportType, fromDate, toDate, classId, studentId, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Справка");

        worksheet.Cell("A1").Value = $"Справка: {report.ReportTitle}";
        worksheet.Range("A1:D1").Merge();
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;

        worksheet.Cell("A3").Value = "Период";
        worksheet.Cell("B3").Value = report.PeriodLabel;
        worksheet.Cell("A4").Value = "Клас";
        worksheet.Cell("B4").Value = report.SelectedClassName;
        worksheet.Cell("A5").Value = "Ученик";
        worksheet.Cell("B5").Value = report.SelectedStudentName ?? "Всички ученици";
        worksheet.Cell("A6").Value = report.TotalLabel;
        worksheet.Cell("B6").Value = report.TotalRecords;
        worksheet.Cell("A7").Value = report.MetricLabel;
        worksheet.Cell("B7").Value = report.AverageValue?.ToString("0.00") ?? string.Empty;

        worksheet.Range("A3:A7").Style.Font.Bold = true;

        worksheet.Cell("A10").Value = "Дата";
        worksheet.Cell("B10").Value = report.MetricLabel;
        worksheet.Cell("C10").Value = "Брой записи";
        worksheet.Range("A10:C10").Style.Font.Bold = true;
        worksheet.Range("A10:C10").Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF0E3");

        var row = 11;
        foreach (var item in report.LinePoints)
        {
            worksheet.Cell(row, 1).Value = item.Date;
            worksheet.Cell(row, 1).Style.DateFormat.Format = "dd.MM.yyyy";
            worksheet.Cell(row, 2).Value = item.Value;
            worksheet.Cell(row, 2).Style.NumberFormat.Format = "0.00";
            worksheet.Cell(row, 3).Value = item.Count;
            row++;
        }

        worksheet.Range(10, 1, Math.Max(row - 1, 10), 3).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        worksheet.Range(10, 1, Math.Max(row - 1, 10), 3).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        worksheet.Columns(1, 3).AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        if (report.LinePoints.Count > 0)
        {
            AddLineChart(stream, row - 1, report.ReportTitle, report.MetricLabel);
        }

        return new PrincipalReportExportFile
        {
            FileName = $"spravka-{report.ReportType}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Content = stream.ToArray(),
        };
    }

    public async Task<IReadOnlyList<PrincipalReportStudentSearchResult>> SearchStudentMatchesAsync(
        string? query,
        Guid? classId,
        CancellationToken cancellationToken = default)
    {
        var searchTerms = GetSearchTerms(query);
        var studentsQuery = (await studentRepository.GetAllAsync(cancellationToken))
            .Where(student => student.User.IsActive)
            .Where(student => !classId.HasValue || student.ClassId == classId.Value);

        foreach (var searchTerm in searchTerms)
        {
            var currentTerm = searchTerm;
            studentsQuery = studentsQuery.Where(student =>
                student.User.FirstName.Contains(currentTerm, StringComparison.OrdinalIgnoreCase) ||
                (student.User.MiddleName != null &&
                 student.User.MiddleName.Contains(currentTerm, StringComparison.OrdinalIgnoreCase)) ||
                student.User.LastName.Contains(currentTerm, StringComparison.OrdinalIgnoreCase));
        }

        return studentsQuery
            .OrderBy(student => student.User.FirstName)
            .ThenBy(student => student.User.MiddleName)
            .ThenBy(student => student.User.LastName)
            .Take(20)
            .Select(student => new PrincipalReportStudentSearchResult
            {
                StudentId = student.Id,
                FullName = FormatFullName(
                    student.User.FirstName,
                    student.User.MiddleName,
                    student.User.LastName),
                ClassName = $"{student.Class.GradeNumber}{student.Class.Letter}",
            })
            .ToList();
    }

    private async Task<List<PrincipalReportClassOptionViewModel>> GetClassOptionsAsync(CancellationToken cancellationToken)
    {
        return (await classRepository.GetAllAsync(cancellationToken))
            .OrderBy(schoolClass => schoolClass.GradeNumber)
            .ThenBy(schoolClass => schoolClass.Letter)
            .Select(schoolClass => new PrincipalReportClassOptionViewModel
            {
                Id = schoolClass.Id,
                Name = $"{schoolClass.GradeNumber} {schoolClass.Letter}",
            })
            .ToList();
    }

    private async Task<List<ReportRow>> GetReportRowsAsync(
        string reportType,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? classId,
        Guid? studentId,
        CancellationToken cancellationToken)
    {
        var from = fromDate?.Date;
        var to = toDate?.Date.AddDays(1);

        if (reportType == PrincipalReportTypes.Academic)
        {
            return (await gradeRepository.GetAllAsync(cancellationToken))
                .Where(grade => !from.HasValue || grade.CreatedAt >= from.Value)
                .Where(grade => !to.HasValue || grade.CreatedAt < to.Value)
                .Where(grade => !classId.HasValue || grade.Student.ClassId == classId.Value)
                .Where(grade => !studentId.HasValue || grade.StudentId == studentId.Value)
                .GroupBy(grade => grade.CreatedAt.Date)
                .OrderBy(group => group.Key)
                .Select(group => new ReportRow(group.Key, Math.Round(group.Average(grade => grade.Value), 2), group.Count()))
                .ToList();
        }

        if (reportType == PrincipalReportTypes.Feedback)
        {
            return (await feedbackRepository.GetAllAsync(cancellationToken))
                .Where(feedback => !from.HasValue || feedback.CreatedAt >= from.Value)
                .Where(feedback => !to.HasValue || feedback.CreatedAt < to.Value)
                .Where(feedback => !classId.HasValue || feedback.Student.ClassId == classId.Value)
                .Where(feedback => !studentId.HasValue || feedback.StudentId == studentId.Value)
                .GroupBy(feedback => feedback.CreatedAt.Date)
                .OrderBy(group => group.Key)
                .Select(group => new ReportRow(group.Key, group.Count(), group.Count()))
                .ToList();
        }

        var absences = (await absenceRepository.GetAllAsync(cancellationToken))
            .Where(absence => !from.HasValue || absence.CreatedAt >= from.Value)
            .Where(absence => !to.HasValue || absence.CreatedAt < to.Value)
            .Where(absence => !classId.HasValue || absence.Student.ClassId == classId.Value)
            .Where(absence => !studentId.HasValue || absence.StudentId == studentId.Value);

        if (reportType == PrincipalReportTypes.UnexcusedAbsences)
        {
            absences = absences.Where(absence => absence.Status == AbsenceStatus.Unexcused);
        }

        return absences
            .GroupBy(absence => absence.CreatedAt.Date)
            .OrderBy(group => group.Key)
            .Select(group => new ReportRow(group.Key, group.Count(), group.Count()))
            .ToList();
    }

    private static List<PrincipalReportLinePointViewModel> CreateLinePoints(
        IReadOnlyList<ReportRow> rows,
        bool isGradeReport)
    {
        if (rows.Count == 0)
        {
            return new List<PrincipalReportLinePointViewModel>();
        }

        var denominator = Math.Max(rows.Count - 1, 1);
        var maximumValue = isGradeReport ? 6 : Math.Max(rows.Max(row => row.Value), 1);

        return rows
            .Select((row, index) => new PrincipalReportLinePointViewModel
            {
                Date = row.Date,
                Label = row.Date.ToString("dd.MM"),
                Count = row.Count,
                Value = row.Value,
                X = rows.Count == 1 ? 50 : (int)Math.Round((decimal)index / denominator * 100),
                Y = isGradeReport
                    ? CalculateGradeLinePointY(row.Value)
                    : CalculateCountLinePointY(row.Value, maximumValue),
            })
            .ToList();
    }

    private static List<string> CreateYAxisLabels(IReadOnlyList<ReportRow> rows, bool isGradeReport)
    {
        if (isGradeReport)
        {
            return new List<string> { "6", "5", "4", "3", "2" };
        }

        var maximumValue = rows.Count == 0 ? 4 : Math.Max((int)Math.Ceiling(rows.Max(row => row.Value)), 4);
        return new List<string>
        {
            maximumValue.ToString(),
            Math.Round(maximumValue * 0.75m).ToString(),
            Math.Round(maximumValue * 0.5m).ToString(),
            Math.Round(maximumValue * 0.25m).ToString(),
            "0",
        };
    }

    private static int CalculateGradeLinePointY(decimal value)
    {
        var clampedGrade = Math.Clamp(value, 2m, 6m);
        return 100 - (int)Math.Round((clampedGrade - 2m) / 4m * 100m);
    }

    private static int CalculateCountLinePointY(decimal value, decimal maximumValue)
    {
        if (maximumValue <= 0)
        {
            return 100;
        }

        return 100 - (int)Math.Round(Math.Clamp(value / maximumValue, 0m, 1m) * 100m);
    }

    private static List<PrincipalReportTypeOptionViewModel> CreateReportTypeOptions()
    {
        return new List<PrincipalReportTypeOptionViewModel>
        {
            new PrincipalReportTypeOptionViewModel { Value = PrincipalReportTypes.Academic, Label = "Успех" },
            new PrincipalReportTypeOptionViewModel { Value = PrincipalReportTypes.Absences, Label = "Отсъствия" },
            new PrincipalReportTypeOptionViewModel { Value = PrincipalReportTypes.UnexcusedAbsences, Label = "Неизвинени отсъствия" },
            new PrincipalReportTypeOptionViewModel { Value = PrincipalReportTypes.Feedback, Label = "Отзиви" },
        };
    }

    private static ReportDefinition GetReportDefinition(string reportType)
    {
        return reportType switch
        {
            PrincipalReportTypes.Absences => new ReportDefinition(
                "Отсъствия",
                "Брой отсъствия по дати",
                "Брой отсъствия",
                "Общо отсъствия",
                false),
            PrincipalReportTypes.UnexcusedAbsences => new ReportDefinition(
                "Неизвинени отсъствия",
                "Брой неизвинени отсъствия по дати",
                "Брой неизвинени",
                "Общо неизвинени",
                false),
            PrincipalReportTypes.Feedback => new ReportDefinition(
                "Отзиви",
                "Брой похвали и забележки по дати",
                "Брой отзиви",
                "Общо отзиви",
                false),
            _ => new ReportDefinition(
                "Успех",
                "Среден успех по дати",
                "Среден успех",
                "Брой оценки",
                true),
        };
    }

    private static string NormalizeReportType(string? reportType)
    {
        return reportType switch
        {
            PrincipalReportTypes.Absences => PrincipalReportTypes.Absences,
            PrincipalReportTypes.UnexcusedAbsences => PrincipalReportTypes.UnexcusedAbsences,
            PrincipalReportTypes.Feedback => PrincipalReportTypes.Feedback,
            _ => PrincipalReportTypes.Academic,
        };
    }

    private static string[] GetSearchTerms(string? query)
    {
        return string.IsNullOrWhiteSpace(query)
            ? Array.Empty<string>()
            : query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string FormatFullName(string firstName, string? middleName, string lastName)
    {
        return string.Join(
            " ",
            new[]
            {
                firstName,
                middleName,
                lastName,
            }.Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static string FormatPeriodLabel(DateTime? fromDate, DateTime? toDate)
    {
        if (!fromDate.HasValue && !toDate.HasValue)
        {
            return "Всички периоди";
        }

        if (fromDate.HasValue && toDate.HasValue)
        {
            return $"{fromDate.Value:dd.MM.yyyy} - {toDate.Value:dd.MM.yyyy}";
        }

        return fromDate.HasValue
            ? $"От {fromDate.Value:dd.MM.yyyy}"
            : $"До {toDate!.Value:dd.MM.yyyy}";
    }

    private static void AddLineChart(MemoryStream stream, int lastDataRow, string title, string metricLabel)
    {
        stream.Position = 0;
        using var document = SpreadsheetDocument.Open(stream, true);
        var workbookPart = document.WorkbookPart;
        var sheet = workbookPart?.Workbook.Sheets?.Elements<S.Sheet>().FirstOrDefault(currentSheet => currentSheet.Name == "Справка");

        if (workbookPart is null || sheet?.Id is null)
        {
            return;
        }

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        var drawingsPart = worksheetPart.DrawingsPart ?? worksheetPart.AddNewPart<DrawingsPart>();

        if (drawingsPart.WorksheetDrawing is null)
        {
            drawingsPart.WorksheetDrawing = new Xdr.WorksheetDrawing();
        }

        if (worksheetPart.Worksheet.GetFirstChild<S.Drawing>() is null)
        {
            var drawingRelationshipId = worksheetPart.GetIdOfPart(drawingsPart);
            worksheetPart.Worksheet.Append(new S.Drawing { Id = drawingRelationshipId });
            worksheetPart.Worksheet.Save();
        }

        var chartPart = drawingsPart.AddNewPart<ChartPart>();
        var chartRelationshipId = drawingsPart.GetIdOfPart(chartPart);
        chartPart.ChartSpace = CreateChartSpace(lastDataRow, title, metricLabel);
        chartPart.ChartSpace.Save();

        drawingsPart.WorksheetDrawing.Append(CreateChartAnchor(chartRelationshipId));
        drawingsPart.WorksheetDrawing.Save();
    }

    private static C.ChartSpace CreateChartSpace(int lastDataRow, string title, string metricLabel)
    {
        const uint categoryAxisId = 48650112;
        const uint valueAxisId = 48672768;
        var categoryRange = $"'Справка'!$A$11:$A${lastDataRow}";
        var valueRange = $"'Справка'!$B$11:$B${lastDataRow}";

        return new C.ChartSpace(
            new C.EditingLanguage { Val = "bg-BG" },
            new C.Chart(
                CreateChartTitle(title),
                new C.PlotArea(
                    new C.Layout(),
                    new C.LineChart(
                        new C.Grouping { Val = C.GroupingValues.Standard },
                        new C.LineChartSeries(
                            new C.Index { Val = 0U },
                            new C.Order { Val = 0U },
                            new C.SeriesText(new C.StringLiteral(
                                new C.PointCount { Val = 1U },
                                new C.StringPoint { Index = 0U, NumericValue = new C.NumericValue(metricLabel) })),
                            new C.CategoryAxisData(new C.StringReference(new C.Formula(categoryRange))),
                            new C.Values(new C.NumberReference(new C.Formula(valueRange))),
                            new C.Marker(new C.Symbol { Val = C.MarkerStyleValues.Circle }),
                            new C.Smooth { Val = false }),
                        new C.AxisId { Val = categoryAxisId },
                        new C.AxisId { Val = valueAxisId }),
                    new C.CategoryAxis(
                        new C.AxisId { Val = categoryAxisId },
                        new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
                        new C.AxisPosition { Val = C.AxisPositionValues.Bottom },
                        new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
                        new C.CrossingAxis { Val = valueAxisId },
                        new C.Crosses { Val = C.CrossesValues.AutoZero },
                        new C.AutoLabeled { Val = true },
                        new C.LabelAlignment { Val = C.LabelAlignmentValues.Center },
                        new C.LabelOffset { Val = 100 }),
                    new C.ValueAxis(
                        new C.AxisId { Val = valueAxisId },
                        new C.Scaling(new C.Orientation { Val = C.OrientationValues.MinMax }),
                        new C.AxisPosition { Val = C.AxisPositionValues.Left },
                        new C.MajorGridlines(),
                        new C.NumberingFormat { FormatCode = "0.00", SourceLinked = false },
                        new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo },
                        new C.CrossingAxis { Val = categoryAxisId },
                        new C.Crosses { Val = C.CrossesValues.AutoZero },
                        new C.CrossBetween { Val = C.CrossBetweenValues.Between })),
                new C.Legend(
                    new C.LegendPosition { Val = C.LegendPositionValues.Bottom },
                    new C.Layout()),
                new C.PlotVisibleOnly { Val = true }));
    }

    private static C.Title CreateChartTitle(string title)
    {
        return new C.Title(
            new C.ChartText(
                new C.RichText(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(
                        new A.Run(
                            new A.RunProperties { Language = "bg-BG", FontSize = 1200 },
                            new A.Text(title))))),
            new C.Layout(),
            new C.Overlay { Val = false });
    }

    private static Xdr.TwoCellAnchor CreateChartAnchor(string chartRelationshipId)
    {
        return new Xdr.TwoCellAnchor(
            new Xdr.FromMarker(
                new Xdr.ColumnId("4"),
                new Xdr.ColumnOffset("0"),
                new Xdr.RowId("2"),
                new Xdr.RowOffset("0")),
            new Xdr.ToMarker(
                new Xdr.ColumnId("13"),
                new Xdr.ColumnOffset("0"),
                new Xdr.RowId("19"),
                new Xdr.RowOffset("0")),
            new Xdr.GraphicFrame(
                new Xdr.NonVisualGraphicFrameProperties(
                    new Xdr.NonVisualDrawingProperties { Id = 2U, Name = "Справка диаграма" },
                    new Xdr.NonVisualGraphicFrameDrawingProperties()),
                new Xdr.Transform(
                    new A.Offset { X = 0L, Y = 0L },
                    new A.Extents { Cx = 0L, Cy = 0L }),
                new A.Graphic(
                    new A.GraphicData(
                        new C.ChartReference { Id = chartRelationshipId })
                    {
                        Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart",
                    })),
            new Xdr.ClientData());
    }

    private sealed record ReportDefinition(
        string Title,
        string Description,
        string MetricLabel,
        string TotalLabel,
        bool IsGradeReport);

    private sealed record ReportRow(DateTime Date, decimal Value, int Count);
}
