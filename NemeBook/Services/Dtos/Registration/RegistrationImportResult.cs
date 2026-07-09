namespace Services.Dtos.Registration;

public class RegistrationImportResult
{
    public int TotalRows { get; set; }

    public int CreatedUsers { get; set; }

    public int CreatedProfiles { get; set; }

    public int CreatedInvitations { get; set; }

    public List<RegistrationImportIssue> Issues { get; set; } = new List<RegistrationImportIssue>();
}
