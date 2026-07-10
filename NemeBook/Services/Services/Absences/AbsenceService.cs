using Entities.Enums;
using Entities.Models;
using Services.Interfaces.Absences;
using Services.Repositories;

namespace Services.Services.Absences;

/// <summary>
/// ЗАБЕЛЕЖКА ЗА ДОПУСКАНИЯ (моля коригирай ако не пасват на реалния код):
///  - IAbsenceRepository съдържа само базови CRUD методи (GetByIdAsync, GetAllAsync,
///    CreateAsync, UpdateAsync, DeleteAsync), затова филтрирането за "клик" логиката
///    и по ученик става тук, чрез GetAllAsync() + LINQ. При нужда от по-добра
///    производителност, добави индексирани query методи в repository-то по-късно.
///  - Приема се, че UserRole съдържа поне стойностите Principal и Teacher.
///  - Приема се, че ITeacherRepository / IStudentRepository / IClassScheduleEntryRepository
///    вече съществуват и имат метод GetByIdAsync(Guid, CancellationToken), аналогично на IClassRepository.
///  - Teacher.MainClass се използва, за да се определи дали учителят е класен ръководител
///    на класа, в който е ученикът.
///  - При 3-ти клик (отсъствието вече е Type = Absence) в момента се хвърля InvalidOperationException.
///    Кажи ми какво да прави тук (нищо, undo/toggle off и т.н.) - лесно се сменя.
/// </summary>
public class AbsenceService : IAbsenceService
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IClassScheduleEntryRepository _classScheduleEntryRepository;

    public AbsenceService(
        IAbsenceRepository absenceRepository,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        IClassScheduleEntryRepository classScheduleEntryRepository)
    {
        _absenceRepository = absenceRepository;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _classScheduleEntryRepository = classScheduleEntryRepository;
    }

    public async Task<Absence> MarkAsync(
        Guid teacherId,
        Guid studentId,
        Guid classScheduleEntryId,
        CancellationToken cancellationToken = default)
    {
        if (teacherId == Guid.Empty)
            throw new ArgumentException("Teacher id cannot be empty.", nameof(teacherId));

        if (studentId == Guid.Empty)
            throw new ArgumentException("Student id cannot be empty.", nameof(studentId));

        if (classScheduleEntryId == Guid.Empty)
            throw new ArgumentException("Class schedule entry id cannot be empty.", nameof(classScheduleEntryId));

        var scheduleEntry = await _classScheduleEntryRepository.GetByIdAsync(classScheduleEntryId, cancellationToken);
        if (scheduleEntry is null)
            throw new InvalidOperationException("Class schedule entry not found.");

        // Само учителят, който реално преподава този предмет в този клас, може да маркира отсъствия.
        if (scheduleEntry.ClassSubject.TeacherId != teacherId)
            throw new UnauthorizedAccessException("Teacher is not assigned to this class subject.");

        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
            throw new InvalidOperationException("Student not found.");

        if (student.ClassId != scheduleEntry.ClassId)
            throw new InvalidOperationException("Student does not belong to this class.");

        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        var allAbsences = await _absenceRepository.GetAllAsync(cancellationToken);

        var existing = allAbsences.FirstOrDefault(a =>
            a.StudentId == studentId &&
            a.ClassScheduleEntryId == classScheduleEntryId &&
            a.Date == date);

        if (existing is null)
        {
            // 1-ви клик -> закъснение
            var absence = new Absence
            {
                Id = Guid.NewGuid(),
                ClassSubjectId = scheduleEntry.ClassSubjectId,
                StudentId = studentId,
                ClassScheduleEntryId = scheduleEntry.Id,
                Date = date,
                LessonNumber = scheduleEntry.PeriodNumber,
                Type = AbsenceType.Lateness,
                Status = AbsenceStatus.Unexcused
            };

            await _absenceRepository.CreateAsync(absence, cancellationToken);
            return absence;
        }

        if (existing.Type == AbsenceType.Lateness)
        {
            // 2-ри клик -> ъпгрейд до неизвинено отсъствие
            existing.Type = AbsenceType.Absence;
            existing.Status = AbsenceStatus.Unexcused;

            await _absenceRepository.UpdateAsync(existing, cancellationToken);
            return existing;
        }

        // Вече е маркиран като Absence - поведението за 3-ти клик предстои да се уточни.
        throw new InvalidOperationException("Student is already marked as absent for this lesson.");
    }

    public async Task<Absence> ExcuseAsync(
        Guid absenceId,
        Guid actingUserId,
        UserRole actingUserRole,
        AbsenceExcuseReason excuseReason,
        string? excuseNote,
        CancellationToken cancellationToken = default)
    {
        // ВАЖНО: когато actingUserRole == Teacher, actingUserId трябва да е Teacher.Id
        // (не User.Id) - потвърдено, защото ITeacherRepository.GetByIdAsync очаква Teacher.Id.
        // Когато actingUserRole == Principal, actingUserId не се използва за проверка,
        // само ролята се проверява.

        if (absenceId == Guid.Empty)
            throw new ArgumentException("Absence id cannot be empty.", nameof(absenceId));

        if (actingUserId == Guid.Empty)
            throw new ArgumentException("Acting user id cannot be empty.", nameof(actingUserId));

        var absence = await _absenceRepository.GetByIdAsync(absenceId, cancellationToken);
        if (absence is null)
            throw new InvalidOperationException("Absence not found.");

        var student = await _studentRepository.GetByIdAsync(absence.StudentId, cancellationToken);
        if (student is null)
            throw new InvalidOperationException("Student not found.");

        var isAuthorized = actingUserRole switch
        {
            UserRole.Principal => true,
            UserRole.Teacher => await IsMainClassTeacherAsync(actingUserId, student.ClassId, cancellationToken),
            _ => false
        };

        if (!isAuthorized)
            throw new UnauthorizedAccessException("User is not allowed to excuse this absence.");

        absence.Status = AbsenceStatus.Excused;
        absence.ExcuseReason = excuseReason;
        absence.ExcuseNote = excuseNote ?? string.Empty;

        await _absenceRepository.UpdateAsync(absence, cancellationToken);
        return absence;
    }

    public async Task<IReadOnlyList<Absence>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("Student id cannot be empty.", nameof(studentId));

        var allAbsences = await _absenceRepository.GetAllAsync(cancellationToken);

        return allAbsences.Where(a => a.StudentId == studentId).ToList();
    }

    private async Task<bool> IsMainClassTeacherAsync(Guid teacherId, Guid classId, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId, cancellationToken);
        return teacher?.MainClass is not null && teacher.MainClass.Id == classId;
    }
}