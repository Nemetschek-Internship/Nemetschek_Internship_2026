namespace Services.Dtos.Registration;

public class StudentImportDto
{
    public int? RowNumber { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateOnly BirthDate { get; set; }

    public string? PhoneNumber { get; set; }

    public string ClassLabel { get; set; } = null!;

    public IReadOnlyCollection<string> ParentEmails { get; set; } = Array.Empty<string>();
}
