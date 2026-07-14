namespace Web.ViewModels;

public class PrincipalHomeViewModel
{
    public decimal? SchoolAverageGrade { get; set; }

    public List<PrincipalReportItemViewModel> Reports { get; set; } = new();

    public List<PrincipalClassAttentionViewModel> ClassesNeedingAttention { get; set; } = new();

    public List<PrincipalGradeDistributionViewModel> GradeDistribution { get; set; } = new();
}

public class PrincipalReportItemViewModel
{
    public string Label { get; set; } = string.Empty;

    public string ReportType { get; set; } = string.Empty;

    public string? Value { get; set; }

    public string CssClass { get; set; } = string.Empty;
}

public class PrincipalClassAttentionViewModel
{
    public string ClassName { get; set; } = string.Empty;

    public decimal AverageGrade { get; set; }
}

public class PrincipalGradeDistributionViewModel
{
    public int Grade { get; set; }

    public int Count { get; set; }

    public int BarHeight { get; set; }
}
