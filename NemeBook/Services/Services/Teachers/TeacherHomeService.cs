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

    public async Task<TeacherHomeViewModel?> GetHomeAsync(
        Guid userId,
        Guid? classId = null,
        bool selectDefaultClass = true,
        CancellationToken cancellationToken = default)
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

        var accessibleClasses = allClasses
            .Where(schoolClass =>
                schoolClass.MainTeacherId == teacher.Id ||
                assignedClassSubjects.Any(classSubject => classSubject.ClassId == schoolClass.Id))
            .OrderBy(schoolClass => schoolClass.GradeNumber)
            .ThenBy(schoolClass => schoolClass.Letter)
            .ToList();

        var selectedClass = classId.HasValue
            ? accessibleClasses.FirstOrDefault(schoolClass => schoolClass.Id == classId.Value)
            : selectDefaultClass
                ? accessibleClasses
                    .OrderByDescending(schoolClass => schoolClass.MainTeacherId == teacher.Id)
                    .ThenBy(schoolClass => schoolClass.GradeNumber)
                    .ThenBy(schoolClass => schoolClass.Letter)
                    .FirstOrDefault()
                : null;
        var selectedClassSubject = selectedClass is null
            ? null
            : assignedClassSubjects
                .OrderBy(classSubject => classSubject.Subject.Name)
                .FirstOrDefault(classSubject => classSubject.ClassId == selectedClass.Id);
        var students = selectedClass?.Students
            .OrderBy(student => student.User.FirstName)
            .ThenBy(student => student.User.LastName)
            .ToList() ?? new List<Student>();
        var className = selectedClass is null ? "0" : FormatClassName(selectedClass);
        var selectedClassSubjectIds = selectedClass is null
            ? new HashSet<Guid>()
            : assignedClassSubjects
                .Where(classSubject => classSubject.ClassId == selectedClass.Id)
                .Select(classSubject => classSubject.Id)
                .ToHashSet();
        var recordCountsByStudentId = selectedClassSubjectIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await GetRecordCountsAsync(students, selectedClassSubjectIds, cancellationToken);

        return new TeacherHomeViewModel
        {
            TeacherName = FormatUserName(teacher.User),
            TeacherInitials = FormatInitials(teacher.User.FirstName, teacher.User.LastName),
            MainMeta = className == "0" ? "Учител" : $"Клас {className}",
            ClassId = selectedClass?.Id,
            SubjectId = selectedClassSubject?.SubjectId,
            ClassName = className,
            StudentCount = students.Count,
            TeachingClasses = BuildTeachingClasses(accessibleClasses, assignedClassSubjects, teacher.Id),
            Students = BuildStudents(students, recordCountsByStudentId),
            SelectedStudent = new TeacherSelectedStudentViewModel()
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

    private static IReadOnlyList<TeacherClassListItem> BuildTeachingClasses(
        IReadOnlyList<Class> classes,
        IReadOnlyCollection<ClassSubject> assignedClassSubjects,
        Guid teacherId)
    {
        return classes
            .Select(schoolClass =>
            {
                var subjectNames = assignedClassSubjects
                    .Where(classSubject => classSubject.ClassId == schoolClass.Id)
                    .Select(classSubject => classSubject.Subject.Name)
                    .Distinct()
                    .OrderBy(subjectName => subjectName)
                    .ToList();

                return new TeacherClassListItem
                {
                    Id = schoolClass.Id,
                    Name = FormatClassName(schoolClass),
                    SubjectNames = subjectNames.Count == 0
                        ? "Класен ръководител"
                        : string.Join(", ", subjectNames),
                    StudentCount = schoolClass.Students.Count,
                    IsMainClass = schoolClass.MainTeacherId == teacherId
                };
            })
            .ToList();
    }

    private static IReadOnlyList<TeacherStudentListItem> BuildStudents(
        IReadOnlyList<Student> students,
        IReadOnlyDictionary<Guid, int> recordCountsByStudentId)
    {
        return students
            .Select(student => new TeacherStudentListItem
            {
                Id = student.Id,
                FullName = FormatUserName(student.User),
                Initials = FormatInitials(student.User.FirstName, student.User.LastName),
                RecordCount = recordCountsByStudentId.GetValueOrDefault(student.Id),
                IsSelected = false
            })
            .ToList();
    }

    private async Task<Dictionary<Guid, int>> GetRecordCountsAsync(
        IReadOnlyCollection<Student> students,
        IReadOnlyCollection<Guid> selectedClassSubjectIds,
        CancellationToken cancellationToken)
    {
        var studentIds = students
            .Select(student => student.Id)
            .ToHashSet();

        var grades = await gradeRepository.GetAllAsync(cancellationToken);
        var absences = await absenceRepository.GetAllAsync(cancellationToken);
        var feedbacks = await feedbackRepository.GetAllAsync(cancellationToken);

        return studentIds.ToDictionary(
            studentId => studentId,
            studentId =>
                grades.Count(grade =>
                    grade.StudentId == studentId && selectedClassSubjectIds.Contains(grade.ClassSubjectId))
                + absences.Count(absence =>
                    absence.StudentId == studentId && selectedClassSubjectIds.Contains(absence.ClassSubjectId))
                + feedbacks.Count(feedback =>
                    feedback.StudentId == studentId && selectedClassSubjectIds.Contains(feedback.ClassSubjectId)));
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
