namespace Web.ViewModels;

public class PrincipalClassesViewModel
{
    public int SelectedGrade { get; set; } = 1;

    public List<int> GradeNumbers { get; set; } = new();

    public List<PrincipalClassCardViewModel> Classes { get; set; } = new();
}

public class PrincipalClassCardViewModel
{
    public Guid Id { get; set; }

    public int GradeNumber { get; set; }

    public char Letter { get; set; }

    public decimal? AverageGrade { get; set; }

    public string Name => $"{GradeNumber}{Letter}";
}
