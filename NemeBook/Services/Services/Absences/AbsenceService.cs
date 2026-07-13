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
///  - 20-минутният прозорец за редакция се смята от Absence.CreatedAt (UTC).
///  - "3-ти клик" -> цикълът се връща обратно към Lateness (1-во състояние), не се трие записа.
///    Т.е. кликовете циклират: Lateness -> Absence -> Lateness -> Absence -> ... в рамките
///    на 20-минутния прозорец за редакция.
///  - Soft delete (IsDeleted) е отделен, административен flow (DeleteAsync метода тук),
///    предназначен за трайно уредени случаи, не за корекция на "клик по грешка".
/// </summary>
public class AbsenceService : IAbsenceService
{
    private static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(20);

    private readonly IAbsenceRepository _absenceRepository;
    private readonly ITeacherRepository _teacherRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IClassScheduleEntryRepository _classScheduleEntryRepository;
    private readonly IClassSubjectRepository _classSubjectRepository;

    public AbsenceService(
        IAbsenceRepository absenceRepository,
        ITeacherRepository teacherRepository,
        IStudentRepository studentRepository,
        IClassScheduleEntryRepository classScheduleEntryRepository,
        IClassSubjectRepository classSubjectRepository)
    {
        _absenceRepository = absenceRepository;
        _teacherRepository = teacherRepository;
        _studentRepository = studentRepository;
        _classScheduleEntryRepository = classScheduleEntryRepository;
        _classSubjectRepository = classSubjectRepository;
    }

    public async Task<IReadOnlyList<ClassSubject>> GetTeacherClassSubjectsAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        if (teacherId == Guid.Empty)
            throw new ArgumentException("Teacher id cannot be empty.", nameof(teacherId));

        var allClassSubjects = await _classSubjectRepository.GetAllAsync(cancellationToken);

        return allClassSubjects.Where(cs => cs.TeacherId == teacherId).ToList();
    }

    public async Task<ClassScheduleEntry?> GetCurrentScheduleEntryAsync(
        Guid classSubjectId,
        CancellationToken cancellationToken = default)
    {
        if (classSubjectId == Guid.Empty)
            throw new ArgumentException("Class subject id cannot be empty.", nameof(classSubjectId));

        // ВНИМАНИЕ: използва локалното време на сървъра (DateTime.Now), за да определи
        // текущия ден/час спрямо седмичната програма. Ако сървърът работи в различна
        // часова зона от училището, трябва да се смени с TimeZoneInfo конверсия.
        var now = DateTime.Now;
        var currentDay = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        var allEntries = await _classScheduleEntryRepository.GetAllAsync(cancellationToken);

        return allEntries.FirstOrDefault(e =>
            e.ClassSubjectId == classSubjectId &&
            e.DayOfWeek == currentDay &&
            e.StartsAt <= currentTime &&
            e.EndsAt >= currentTime);
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
            a.Date == date &&
            !a.IsDeleted);

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

        if (!IsWithinEditWindow(existing))
        {
            throw new InvalidOperationException(
                "20-минутният прозорец за редакция е изтекъл. За промяна се обърнете към класния ръководител или администрацията.");
        }

        if (existing.Type == AbsenceType.Lateness)
        {
            // 2-ри клик -> ъпгрейд до неизвинено отсъствие
            existing.Type = AbsenceType.Absence;
            existing.Status = AbsenceStatus.Unexcused;

            await _absenceRepository.UpdateAsync(existing, cancellationToken);
            return existing;
        }

        // 3-ти клик -> цикълът се връща обратно към първото състояние (закъснение).
        existing.Type = AbsenceType.Lateness;
        existing.Status = AbsenceStatus.Unexcused;

        await _absenceRepository.UpdateAsync(existing, cancellationToken);
        return existing;
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
        if (absence is null || absence.IsDeleted)
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

    public async Task DeleteAsync(
        Guid absenceId,
        UserRole actingUserRole,
        CancellationToken cancellationToken = default)
    {
        if (absenceId == Guid.Empty)
            throw new ArgumentException("Absence id cannot be empty.", nameof(absenceId));

        // Само администрация (Principal) може да трие отсъствия - и то само soft delete,
        // за да остане следа за одит при жалби от родители.
        if (actingUserRole != UserRole.Principal)
            throw new UnauthorizedAccessException("Only administration can delete absences.");

        var absence = await _absenceRepository.GetByIdAsync(absenceId, cancellationToken);
        if (absence is null || absence.IsDeleted)
            throw new InvalidOperationException("Absence not found.");

        absence.IsDeleted = true;

        await _absenceRepository.UpdateAsync(absence, cancellationToken);
    }

    public async Task<IReadOnlyList<Absence>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("Student id cannot be empty.", nameof(studentId));

        var allAbsences = await _absenceRepository.GetAllAsync(cancellationToken);

        return allAbsences.Where(a => a.StudentId == studentId && !a.IsDeleted).ToList();
    }

    public async Task<IReadOnlyList<Absence>> GetByClassAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        if (classId == Guid.Empty)
            throw new ArgumentException("Class id cannot be empty.", nameof(classId));

        // Absence няма директна връзка към Class, само към Student (който има ClassId),
        // затова кръстосваме двете колекции тук. Пак чрез GetAllAsync - същият компромис
        // заради базовия CRUD интерфейс на repository-тата (виж бележката най-горе).
        var allStudents = await _studentRepository.GetAllAsync(cancellationToken);
        var studentIdsInClass = allStudents
            .Where(s => s.ClassId == classId)
            .Select(s => s.Id)
            .ToHashSet();

        if (studentIdsInClass.Count == 0)
            return Array.Empty<Absence>();

        var allAbsences = await _absenceRepository.GetAllAsync(cancellationToken);

        return allAbsences.Where(a => studentIdsInClass.Contains(a.StudentId) && !a.IsDeleted).ToList();
    }

    private static bool IsWithinEditWindow(Absence absence)
    {
        return DateTime.UtcNow - absence.CreatedAt <= EditWindow;
    }

    private async Task<bool> IsMainClassTeacherAsync(Guid teacherId, Guid classId, CancellationToken cancellationToken)
    {
        var teacher = await _teacherRepository.GetByIdAsync(teacherId, cancellationToken);
        return teacher?.MainClass is not null && teacher.MainClass.Id == classId;
    }
}