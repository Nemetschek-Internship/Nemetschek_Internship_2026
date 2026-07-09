using Microsoft.AspNetCore.Http;
using Services.Dtos.Registration;

namespace Web.ViewModels;

public class ProfileImportUploadViewModel
{
    public IFormFile? StudentsFile { get; set; }

    public IFormFile? ParentsFile { get; set; }

    public IFormFile? TeachersFile { get; set; }

    public List<ProfileImportSectionResult> Results { get; set; } = new List<ProfileImportSectionResult>();

    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public bool HasResults => Results.Count > 0;
}

public class ProfileImportSectionResult
{
    public string Title { get; set; } = string.Empty;

    public string? FileName { get; set; }

    public RegistrationImportResult? Result { get; set; }

    public string? ErrorMessage { get; set; }

    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);
}
