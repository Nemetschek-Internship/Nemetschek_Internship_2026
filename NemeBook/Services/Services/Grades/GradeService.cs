using Entities.Enums;
using Entities.Models;
using Entities.ViewModels.Grades;
using Microsoft.Extensions.Logging;
using Services.Interfaces.ClassSubjects;
using Services.Interfaces.Classes;
using Services.Interfaces.Grades;
using Services.Interfaces.Subjects;
using Services.Interfaces.Teachers;
using Services.Repositories;

namespace Services.Services.Grades;

public class GradeService : IGradeService
{
    private readonly IGradeRepository _gradeRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IClassService _classService;
    private readonly IClassSubjectService _classSubjectService;
    private readonly ISubjectService _subjectService;
    private readonly ITeacherService _teacherService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GradeService> _logger;

    public GradeService(
        IGradeRepository gradeRepository,
        IStudentRepository studentRepository,
        IClassService classService,
        IClassSubjectService classSubjectService,
        ISubjectService subjectService,
        ITeacherService teacherService,
        IUserRepository userRepository,
        ILogger<GradeService> logger)
    {
        _gradeRepository = gradeRepository;
        _studentRepository = studentRepository;
        _classService = classService;
        _classSubjectService = classSubjectService;
        _subjectService = subjectService;
        _teacherService = teacherService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<StudentGradesViewModel> GetStudentGradesAsync(
        Guid studentId,
        GradeFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting grades for student {StudentId}", studentId);

        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
            throw new InvalidOperationException("Student not found.");

        var classEntity = await _classService.GetByIdAsync(student.ClassId, cancellationToken);
        if (classEntity is null)
            throw new InvalidOperationException("Class not found.");

        var gradeFilter = filter is not null ? new GradeFilter
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            Type = filter.Type
        } : null;

        var grades = await _gradeRepository.GetGradesByStudentIdAsync(
            studentId,
            gradeFilter,
            cancellationToken);

        // Събираме всички нужни ID-та
        var classSubjectIds = grades.Select(g => g.ClassSubjectId).Distinct().ToList();
        var studentIds = grades.Select(g => g.StudentId).Distinct().ToList();
        var teacherIds = new List<Guid>();
        var subjectIds = new List<Guid>();

        var allClassSubjects = await _classSubjectService.GetAllAsync(cancellationToken);
        var relevantClassSubjects = allClassSubjects
            .Where(cs => classSubjectIds.Contains(cs.Id))
            .ToList();

        foreach (var cs in relevantClassSubjects)
        {
            if (cs.TeacherId.HasValue)
            {
                teacherIds.Add(cs.TeacherId.Value);
            }

            subjectIds.Add(cs.SubjectId);
        }

        // Subjects
        var allSubjects = await _subjectService.GetAllAsync(cancellationToken);
        var subjectsDict = allSubjects
            .Where(s => subjectIds.Contains(s.Id))
            .ToDictionary(s => s.Id, s => s.Name);

        // Teachers
        var allTeachers = await _teacherService.GetAllAsync(cancellationToken);
        var teacherUserIds = allTeachers
            .Where(t => teacherIds.Contains(t.Id))
            .Select(t => t.UserId)
            .ToList();

        var allUsers = await _userRepository.GetAllAsync(cancellationToken);
        var teacherNames = allUsers
            .Where(u => teacherUserIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var teacherNameByTeacherId = allTeachers
            .Where(t => teacherIds.Contains(t.Id))
            .ToDictionary(
                t => t.Id,
                t => teacherNames.GetValueOrDefault(t.UserId, "Неизвестно"));

        // Student names
        var allStudents = await _studentRepository.GetAllAsync(cancellationToken);
        var studentUserIds = allStudents
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => s.UserId)
            .ToList();

        var studentNames = allUsers
            .Where(u => studentUserIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var studentNameByStudentId = allStudents
            .Where(s => studentIds.Contains(s.Id))
            .ToDictionary(
                s => s.Id,
                s => studentNames.GetValueOrDefault(s.UserId, "Неизвестно"));

        var classSubjectSubjectDict = relevantClassSubjects
            .ToDictionary(
                cs => cs.Id,
                cs => subjectsDict.GetValueOrDefault(cs.SubjectId, "Неизвестно"));

        var classSubjectTeacherDict = relevantClassSubjects
            .ToDictionary(
                cs => cs.Id,
                cs => cs.TeacherId.HasValue
                    ? teacherNameByTeacherId.GetValueOrDefault(cs.TeacherId.Value, "Неизвестно")
                    : "Няма назначен учител");

        var gradeDtos = grades.Select(g => new GradeDto
        {
            Id = g.Id,
            Value = g.Value,
            Type = g.Type,
            Note = g.Note,
            Date = g.CreatedAt,
            SubjectId = relevantClassSubjects.FirstOrDefault(cs => cs.Id == g.ClassSubjectId)?.SubjectId ?? Guid.Empty,
            SubjectName = classSubjectSubjectDict.GetValueOrDefault(g.ClassSubjectId, "Неизвестно"),
            TeacherId = relevantClassSubjects.FirstOrDefault(cs => cs.Id == g.ClassSubjectId)?.TeacherId ?? Guid.Empty,
            TeacherName = classSubjectTeacherDict.GetValueOrDefault(g.ClassSubjectId, "Неизвестно"),
            StudentId = g.StudentId,
            StudentName = studentNameByStudentId.GetValueOrDefault(g.StudentId, "Неизвестно")
        }).ToList();

        var viewModel = new StudentGradesViewModel
        {
            StudentId = studentId,
            StudentName = $"{student.User.FirstName} {student.User.LastName}",
            ClassName = $"{classEntity.GradeNumber}{classEntity.Letter}"
        };

        var gradesBySubject = gradeDtos
            .GroupBy(g => g.SubjectName)
            .ToDictionary(
                g => g.Key,
                g => g.ToList());

        viewModel.GradesBySubject = gradesBySubject;

        foreach (var subjectGroup in gradesBySubject)
        {
            var avg = subjectGroup.Value.Average(g => g.Value);
            viewModel.AverageBySubject[subjectGroup.Key] = Math.Round(avg, 2);
        }

        var allGradeValues = gradeDtos.Select(g => g.Value).ToList();
        viewModel.OverallAverage = allGradeValues.Any()
            ? Math.Round(allGradeValues.Average(), 2)
            : 0;

        viewModel.TypeSummary = gradeDtos
            .GroupBy(g => g.Type)
            .Select(g => new GradeTypeSummaryDto
            {
                Type = g.Key,
                Count = g.Count(),
                Average = Math.Round(g.Average(x => x.Value), 2)
            })
            .OrderBy(g => g.Type)
            .ToList();

        return viewModel;
    }

    public async Task<ClassGradesViewModel> GetClassGradesAsync(
        Guid classId,
        Guid subjectId,
        GradeFilterRequest? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting grades for class {ClassId}, subject {SubjectId}", classId, subjectId);

        var classEntity = await _classService.GetByIdAsync(classId, cancellationToken);
        if (classEntity is null)
            throw new InvalidOperationException("Class not found.");

        var subject = await _subjectService.GetByIdAsync(subjectId, cancellationToken);
        if (subject is null)
            throw new InvalidOperationException("Subject not found.");

        var allClassSubjects = await _classSubjectService.GetAllAsync(cancellationToken);
        var classSubject = allClassSubjects.FirstOrDefault(cs => cs.ClassId == classId && cs.SubjectId == subjectId);
        if (classSubject is null)
            throw new InvalidOperationException("Subject is not taught in this class.");

        var teacherName = "Няма назначен учител";
        if (classSubject.TeacherId.HasValue)
        {
            var teacher = await _teacherService.GetByIdAsync(classSubject.TeacherId.Value, cancellationToken);
            if (teacher is null)
                throw new InvalidOperationException("Teacher not found.");

            var teacherUser = await _userRepository.GetByIdAsync(teacher.UserId, cancellationToken);
            teacherName = teacherUser is not null
                ? $"{teacherUser.FirstName} {teacherUser.LastName}"
                : "Неизвестно";
        }

        var gradeFilter = filter is not null ? new GradeFilter
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            Type = filter.Type
        } : null;

        var grades = await _gradeRepository.GetGradesByClassSubjectIdAsync(
            classSubject.Id,
            gradeFilter,
            cancellationToken);

        var allStudents = await _studentRepository.GetAllAsync(cancellationToken);
        var studentsInClass = allStudents
            .Where(s => s.ClassId == classId)
            .ToList();

        var studentUserIds = studentsInClass.Select(s => s.UserId).ToList();
        var allUsers = await _userRepository.GetAllAsync(cancellationToken);
        var userNames = allUsers
            .Where(u => studentUserIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var studentNameMap = studentsInClass
            .ToDictionary(
                s => s.Id,
                s => userNames.GetValueOrDefault(s.UserId, "Неизвестно"));

        var gradeDtos = grades.Select(g => new GradeDto
        {
            Id = g.Id,
            Value = g.Value,
            Type = g.Type,
            Note = g.Note,
            Date = g.CreatedAt,
            SubjectId = subjectId,
            SubjectName = subject.Name,
            TeacherId = classSubject.TeacherId ?? Guid.Empty,
            TeacherName = teacherName,
            StudentId = g.StudentId,
            StudentName = studentNameMap.GetValueOrDefault(g.StudentId, "Неизвестно")
        }).ToList();

        var viewModel = new ClassGradesViewModel
        {
            ClassId = classId,
            ClassName = $"{classEntity.GradeNumber}{classEntity.Letter}",
            SubjectId = subjectId,
            SubjectName = subject.Name,
            TeacherId = classSubject.TeacherId ?? Guid.Empty,
            TeacherName = teacherName
        };

        foreach (var student in studentsInClass)
        {
            var studentGrades = gradeDtos
                .Where(g => g.StudentId == student.Id)
                .ToList();

            var row = new StudentGradeRowDto
            {
                StudentId = student.Id,
                StudentName = studentNameMap.GetValueOrDefault(student.Id, "Неизвестно"),
                Grades = studentGrades,
                Average = studentGrades.Any()
                    ? Math.Round(studentGrades.Average(g => g.Value), 2)
                    : 0,
                TotalGrades = studentGrades.Count,
                LatestGrade = studentGrades.OrderByDescending(g => g.Date).FirstOrDefault()
            };

            viewModel.StudentGrades.Add(row);
        }

        var statistics = await _gradeRepository.GetStatisticsByClassSubjectIdAsync(
            classSubject.Id,
            cancellationToken);

        viewModel.Summary = new ClassGradeSummaryDto
        {
            TotalStudents = studentsInClass.Count,
            ClassAverage = statistics.Average,
            MinGrade = statistics.MinGrade,
            MaxGrade = statistics.MaxGrade,
            GradeDistribution = statistics.GradeDistribution
        };

        return viewModel;
    }

    public async Task<IReadOnlyList<StudentGradeRowDto>> GetStudentRankingAsync(
        Guid classId,
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        var classGrades = await GetClassGradesAsync(classId, subjectId, null, cancellationToken);

        return classGrades.StudentGrades
            .OrderByDescending(s => s.Average)
            .ToList();
    }

    public async Task<decimal> GetStudentAverageAsync(
        Guid studentId,
        Guid? subjectId = null,
        CancellationToken cancellationToken = default)
    {
        var grades = await _gradeRepository.GetGradesByStudentIdAsync(
            studentId,
            null,
            cancellationToken);

        var filteredGrades = grades.AsEnumerable();

        if (subjectId.HasValue)
        {
            var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
            if (student is null)
                return 0;

            var allClassSubjects = await _classSubjectService.GetAllAsync(cancellationToken);
            var classSubject = allClassSubjects.FirstOrDefault(cs =>
                cs.ClassId == student.ClassId && cs.SubjectId == subjectId.Value);

            if (classSubject is not null)
            {
                filteredGrades = filteredGrades.Where(g => g.ClassSubjectId == classSubject.Id);
            }
        }

        var values = filteredGrades.Select(g => g.Value).ToList();
        return values.Any() ? Math.Round(values.Average(), 2) : 0;
    }
}
