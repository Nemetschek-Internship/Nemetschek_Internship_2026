using Entities.Enums;
using Entities.Models;

namespace Services.Interfaces.Absences;

public interface IAbsenceService
{
    /// <summary>
    /// Връща класовете/предметите, които преподава даден учител - използва се, за да може
    /// учителят да избере от списък "на кой клас въвеждам отсъствие/закъснение".
    /// </summary>
    Task<IReadOnlyList<ClassSubject>> GetTeacherClassSubjectsAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Намира "текущия учебен час" за избран ClassSubject - взима се автоматично от
    /// седмичната програма, спрямо текущия ден от седмицата и час. Връща null, ако в момента
    /// няма активен час за този ClassSubject (напр. извън учебно време).
    /// </summary>
    Task<ClassScheduleEntry?> GetCurrentScheduleEntryAsync(
        Guid classSubjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Маркира ученик за текущия учебен час. Работи на цикличен "клик" принцип:
    /// 1-ви клик -> закъснение (Lateness / Unexcused).
    /// 2-ри клик (същия ученик, час, дата) -> ъпгрейд до неизвинено отсъствие (Absence / Unexcused).
    /// 3-ти клик -> цикълът се връща обратно към закъснение (1-во състояние), и т.н.
    /// Разрешено е само в рамките на 20-минутен прозорец от създаването на записа
    /// - след това хвърля InvalidOperationException.
    /// </summary>
    /// <param name="teacherId">Id на учителя, който въвежда отсъствието (трябва да преподава ClassSubject-а).</param>
    /// <param name="studentId">Id на ученика.</param>
    /// <param name="classScheduleEntryId">Id на часа от седмичната програма (взима се автоматично като "текущ час").</param>
    Task<Absence> MarkAsync(
        Guid teacherId,
        Guid studentId,
        Guid classScheduleEntryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Извинява отсъствие/закъснение.
    /// Класният ръководител може да извинява само за своя клас.
    /// Администрацията може да извинява за всички класове.
    /// </summary>
    Task<Absence> ExcuseAsync(
        Guid absenceId,
        Guid actingUserId,
        UserRole actingUserRole,
        AbsenceExcuseReason excuseReason,
        string? excuseNote,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete на отсъствие - само за администрация (UserRole.Principal). Пази записа за одит
    /// (напр. при жалби от родители), само го маркира като IsDeleted = true.
    /// </summary>
    Task DeleteAsync(
        Guid absenceId,
        UserRole actingUserRole,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Absence>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Преглед на всички отсъствия/закъснения на учениците в даден клас (за всички предмети/дати).
    /// </summary>
    Task<IReadOnlyList<Absence>> GetByClassAsync(Guid classId, CancellationToken cancellationToken = default);
}