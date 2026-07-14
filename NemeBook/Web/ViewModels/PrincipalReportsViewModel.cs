namespace Web.ViewModels;

public class PrincipalReportsViewModel
{
    public string ReportType { get; set; } = PrincipalReportTypes.Academic;

    public string ReportTitle { get; set; } = string.Empty;

    public string ReportDescription { get; set; } = string.Empty;

    public string MetricLabel { get; set; } = string.Empty;

    public string TotalLabel { get; set; } = string.Empty;

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public Guid? ClassId { get; set; }

    public Guid? StudentId { get; set; }

    public string SelectedClassName { get; set; } = "Всички класове";

    public string? SelectedStudentName { get; set; }

    public string PeriodLabel { get; set; } = "Всички периоди";

    public int TotalRecords { get; set; }

    public decimal? AverageValue { get; set; }

    public decimal? MinimumValue { get; set; }

    public decimal? MaximumValue { get; set; }

    public List<PrincipalReportTypeOptionViewModel> ReportTypeOptions { get; set; } = new List<PrincipalReportTypeOptionViewModel>();

    public List<PrincipalReportClassOptionViewModel> ClassOptions { get; set; } = new List<PrincipalReportClassOptionViewModel>();

    public List<PrincipalReportLinePointViewModel> LinePoints { get; set; } = new List<PrincipalReportLinePointViewModel>();

    public List<string> YAxisLabels { get; set; } = new List<string>();

    public bool HasData => TotalRecords > 0;
}

public static class PrincipalReportTypes
{
    public const string Academic = "academic";

    public const string Absences = "absences";

    public const string UnexcusedAbsences = "unexcusedAbsences";

    public const string Feedback = "feedback";
}

public class PrincipalReportTypeOptionViewModel
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

public class PrincipalReportClassOptionViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class PrincipalReportLinePointViewModel
{
    public DateTime Date { get; set; }

    public string Label { get; set; } = string.Empty;

    public int Count { get; set; }

    public decimal Value { get; set; }

    public int X { get; set; }

    public int Y { get; set; }
}

public class PrincipalReportStudentSearchResult
{
    public Guid StudentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;
}

public class PrincipalReportExportFile
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] Content { get; set; } = Array.Empty<byte>();
}
