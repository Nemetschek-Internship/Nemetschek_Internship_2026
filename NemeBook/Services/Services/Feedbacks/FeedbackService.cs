using Entities.Models;
using Services.Dtos.Feedbacks;
using Services.Interfaces.Classes;
using Services.Interfaces.ClassSubjects;
using Services.Interfaces.Feedbacks;
using Services.Interfaces.Students;
using Services.Repositories;

namespace Services.Services.Feedbacks;

public class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly IStudentService _studentService;
    private readonly IClassService _classService;
    private readonly IClassSubjectService _classSubjectService;

    public FeedbackService(
        IFeedbackRepository feedbackRepository,
        IStudentService studentService,
        IClassService classService,
        IClassSubjectService classSubjectService)
    {
        _feedbackRepository = feedbackRepository;
        _studentService = studentService;
        _classService = classService;
        _classSubjectService = classSubjectService;
    }

    public async Task<Guid> CreateAsync(
        CreateFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var student = await _studentService.GetByIdAsync(request.StudentId)
            ?? throw new InvalidOperationException("Ученикът не е намерен.");

        var classSubjects = await _classSubjectService.GetAllAsync();
        var classSubject = classSubjects.FirstOrDefault(cs => cs.Id == request.ClassSubjectId)
            ?? throw new InvalidOperationException("Предметът за класа не е намерен.");

        if (classSubject.ClassId != student.ClassId)
        {
            throw new InvalidOperationException(
                "Избраният предмет не принадлежи на класа на ученика.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new InvalidOperationException("Описанието е задължително.");
        }

        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            StudentId = request.StudentId,
            ClassSubjectId = request.ClassSubjectId,
            ClassScheduleEntryId = request.ClassScheduleEntryId,
            Date = request.Date,
            CreatedAt = DateTime.UtcNow,
            Type = request.Type,
            Description = request.Description.Trim()
        };

        await _feedbackRepository.CreateAsync(feedback, cancellationToken);
        return feedback.Id;
    }

    public async Task<StudentFeedbackViewModel> GetByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentService.GetByIdAsync(studentId);
        var items = await _feedbackRepository.GetByStudentAsync(studentId, cancellationToken);

        return new StudentFeedbackViewModel
        {
            StudentId = studentId,
            StudentName = student is null
                ? string.Empty
                : $"{student.User.FirstName} {student.User.LastName}",
            Items = items.Select(Map).ToList()
        };
    }

    public async Task<ClassFeedbackViewModel> GetByClassAsync(
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var classEntity = await _classService.GetByIdAsync(classId);
        var items = await _feedbackRepository.GetByClassAsync(classId, cancellationToken);

        return new ClassFeedbackViewModel
        {
            ClassId = classId,
            ClassName = classEntity is null
                ? string.Empty
                : $"{classEntity.GradeNumber}{classEntity.Letter}",
            Items = items.Select(Map).ToList()
        };
    }

    private static FeedbackDto Map(Feedback f) => new()
    {
        Id = f.Id,
        StudentId = f.StudentId,
        StudentName = f.Student is null
            ? string.Empty
            : $"{f.Student.User.FirstName} {f.Student.User.LastName}",
        SubjectName = f.ClassSubject?.Subject?.Name ?? string.Empty,
        Date = f.Date,
        CreatedAt = f.CreatedAt,
        Type = f.Type,
        Description = f.Description
    };
}