using Entities.Enums;
using Entities.Models;
using Entities.ViewModels.Teachers;
using Services.Interfaces.Teachers;
using Services.Repositories;

namespace Services.Services.Teachers;

public class TeacherHomeService : ITeacherHomeService
{
    private readonly IAbsenceRepository absenceRepository;
    private readonly IAccountsRepository accountsRepository;
    private readonly IClassRepository classRepository;
    private readonly IClassSubjectRepository classSubjectRepository;
    private readonly IFeedbackRepository feedbackRepository;
    private readonly IGradeRepository gradeRepository;
    private readonly ITeacherRepository teacherRepository;

    public TeacherHomeService(
        ITeacherRepository teacherRepository,
        IAccountsRepository accountsRepository,
        IClassRepository classRepository,
        IClassSubjectRepository classSubjectRepository,
        IGradeRepository gradeRepository,
        IAbsenceRepository absenceRepository,
        IFeedbackRepository feedbackRepository)
    {
        this.teacherRepository = teacherRepository;
        this.accountsRepository = accountsRepository;
        this.classRepository = classRepository;
        this.classSubjectRepository = classSubjectRepository;
        this.gradeRepository = gradeRepository;
        this.absenceRepository = absenceRepository;
        this.feedbackRepository = feedbackRepository;
    }

    public async Task<TeacherHomeViewModel?> GetHomeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        var teachers = await teacherRepository.GetAllAsync(cancellationToken);
        var teacher = teachers.FirstOrDefault(existingTeacher => existingTeacher.UserId == userId);
        if (teacher is null)
        {
            var user = await accountsRepository.GetByIdAsync(userId, cancellationToken);
            return user?.Role == UserRole.Teacher
                ? CreateEmptyTeacherHome(user)
                : null;
        }

        var allClasses = await classRepository.GetAllAsync(cancellationToken);
        var allClassSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);
        var assignedClassSubjects = allClassSubjects
            .Where(classSubject => classSubject.TeacherId == teacher.Id)
            .ToList();

        var selectedClass = allClasses
            .Where(schoolClass => schoolClass.MainTeacherId == teacher.Id)
            .OrderBy(schoolClass => schoolClass.GradeNumber)
            .ThenBy(schoolClass => schoolClass.Letter)
            .FirstOrDefault()
            ?? allClasses
                .Where(schoolClass => assignedClassSubjects.Any(classSubject => classSubject.ClassId == schoolClass.Id))
                .OrderBy(schoolClass => schoolClass.GradeNumber)
                .ThenBy(schoolClass => schoolClass.Letter)
                .FirstOrDefault();
        var selectedClassSubject = selectedClass is null
            ? null
            : assignedClassSubjects
                .OrderBy(classSubject => classSubject.Subject.Name)
                .FirstOrDefault(classSubject => classSubject.ClassId == selectedClass.Id);
        var students = selectedClass?.Students
            .OrderBy(student => student.User.FirstName)
            .ThenBy(student => student.User.LastName)
            .ToList() ?? new List<Student>();
        var selectedStudent = students.FirstOrDefault();
        var className = selectedClass is null ? "0" : FormatClassName(selectedClass);
        var selectedStudentRecordCount = selectedStudent is null
            ? 0
            : await GetRecordCountAsync(selectedStudent.Id, assignedClassSubjects, cancellationToken);

        return new TeacherHomeViewModel
        {
            TeacherName = FormatUserName(teacher.User),
            TeacherInitials = FormatInitials(teacher.User.FirstName, teacher.User.LastName),
            MainMeta = className == "0" ? "Учител" : $"Клас {className}",
            ClassId = selectedClass?.Id,
            SubjectId = selectedClassSubject?.SubjectId,
            ClassName = className,
            StudentCount = students.Count,
            Students = BuildStudents(students, selectedStudent?.Id),
            SelectedStudent = BuildSelectedStudent(selectedStudent, className, selectedStudentRecordCount)
        };
    }

    private static TeacherHomeViewModel CreateEmptyTeacherHome(User user)
    {
        return new TeacherHomeViewModel
        {
            TeacherName = FormatUserName(user),
            TeacherInitials = FormatInitials(user.FirstName, user.LastName)
        };
    }

    private static IReadOnlyList<TeacherStudentListItem> BuildStudents(
        IReadOnlyList<Student> students,
        Guid? selectedStudentId)
    {
        return students
            .Select(student => new TeacherStudentListItem
            {
                Id = student.Id,
                FullName = FormatUserName(student.User),
                Initials = FormatInitials(student.User.FirstName, student.User.LastName),
                IsSelected = selectedStudentId.HasValue && student.Id == selectedStudentId.Value
            })
            .ToList();
    }

    private async Task<int> GetRecordCountAsync(
        Guid studentId,
        IReadOnlyCollection<ClassSubject> assignedClassSubjects,
        CancellationToken cancellationToken)
    {
        var assignedClassSubjectIds = assignedClassSubjects
            .Select(classSubject => classSubject.Id)
            .ToHashSet();

        var grades = await gradeRepository.GetAllAsync(cancellationToken);
        var absences = await absenceRepository.GetAllAsync(cancellationToken);
        var feedbacks = await feedbackRepository.GetAllAsync(cancellationToken);

        return grades.Count(grade =>
                grade.StudentId == studentId && assignedClassSubjectIds.Contains(grade.ClassSubjectId))
            + absences.Count(absence =>
                absence.StudentId == studentId && assignedClassSubjectIds.Contains(absence.ClassSubjectId))
            + feedbacks.Count(feedback =>
                feedback.StudentId == studentId && assignedClassSubjectIds.Contains(feedback.ClassSubjectId));
    }

    private static TeacherSelectedStudentViewModel BuildSelectedStudent(
        Student? student,
        string className,
        int recordCount)
    {
        if (student is null)
        {
            return new TeacherSelectedStudentViewModel();
        }

        return new TeacherSelectedStudentViewModel
        {
            Id = student.Id,
            FullName = FormatUserName(student.User),
            Initials = FormatInitials(student.User.FirstName, student.User.LastName),
            ClassName = className,
            RecordCount = recordCount
        };
    }

    private static string FormatClassName(Class schoolClass)
    {
        return $"{schoolClass.GradeNumber}{schoolClass.Letter}";
    }

    private static string FormatUserName(User user)
    {
        return string.Join(
            " ",
            new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static string FormatInitials(string firstName, string lastName)
    {
        var firstInitial = string.IsNullOrWhiteSpace(firstName) ? "?" : firstName[..1].ToUpperInvariant();
        var lastInitial = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName[..1].ToUpperInvariant();

        return $"{firstInitial}{lastInitial}";
    }
}
