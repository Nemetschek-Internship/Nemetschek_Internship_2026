using Entities.Enums;
using Entities.Models;

namespace Services.Interfaces.Absences;

public interface IAbsenceService
{
    /// <summary>
    /// Маркира ученик за текущия учебен час.
    /// 1-ви клик -> закъснение (Lateness / Unexcused).
    /// 2-ри клик (същия ученик, час, дата) -> ъпгрейд до неизвинено отсъствие (Absence / Unexcused).
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

    Task<IReadOnlyList<Absence>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
}
