using Entities.Enums;
using Entities.Models;
using Moq;
using Services.Interfaces.Absences;
using Services.Repositories;
using Services.Services.Absences;
using Xunit;

namespace Tests.Absences;

public class AbsenceServiceTests
{
    private readonly Mock<IAbsenceRepository> _absenceRepository = new();
    private readonly Mock<ITeacherRepository> _teacherRepository = new();
    private readonly Mock<IStudentRepository> _studentRepository = new();
    private readonly Mock<IClassScheduleEntryRepository> _scheduleRepository = new();
    private readonly Mock<IClassSubjectRepository> _classSubjectRepository = new();

    private readonly IAbsenceService _sut;

    public AbsenceServiceTests()
    {
        _sut = new AbsenceService(
            _absenceRepository.Object,
            _teacherRepository.Object,
            _studentRepository.Object,
            _scheduleRepository.Object,
            _classSubjectRepository.Object);
    }

    // ---------- helpers ----------

    private static ClassScheduleEntry CreateScheduleEntry(Guid classId, Guid classSubjectId, Guid teacherId, int periodNumber = 3)
    {
        return new ClassScheduleEntry
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            ClassSubjectId = classSubjectId,
            PeriodNumber = periodNumber,
            ClassSubject = new ClassSubject
            {
                Id = classSubjectId,
                ClassId = classId,
                TeacherId = teacherId
            }
        };
    }

    private static Student CreateStudent(Guid classId)
    {
        return new Student
        {
            Id = Guid.NewGuid(),
            ClassId = classId
        };
    }

    // ---------- MarkAsync ----------

    [Fact]
    public async Task MarkAsync_FirstClick_CreatesLatenessAbsence()
    {
        var classId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var classSubjectId = Guid.NewGuid();

        var scheduleEntry = CreateScheduleEntry(classId, classSubjectId, teacherId);
        var student = CreateStudent(classId);

        _scheduleRepository.Setup(r => r.GetByIdAsync(scheduleEntry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleEntry);
        _studentRepository.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _absenceRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Absence>()); // няма съществуващо отсъствие -> 1-ви клик

        Absence? created = null;
        _absenceRepository
            .Setup(r => r.CreateAsync(It.IsAny<Absence>(), It.IsAny<CancellationToken>()))
            .Callback<Absence, CancellationToken>((a, _) => created = a)
            .Returns(Task.CompletedTask);

        var result = await _sut.MarkAsync(teacherId, student.Id, scheduleEntry.Id);

        Assert.Equal(AbsenceType.Lateness, result.Type);
        Assert.Equal(AbsenceStatus.Unexcused, result.Status);
        Assert.NotNull(created);
        _absenceRepository.Verify(r => r.CreateAsync(It.IsAny<Absence>(), It.IsAny<CancellationToken>()), Times.Once);
        _absenceRepository.Verify(r => r.UpdateAsync(It.IsAny<Absence>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsync_SecondClick_UpgradesLatenessToAbsence()
    {
        var classId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var classSubjectId = Guid.NewGuid();

        var scheduleEntry = CreateScheduleEntry(classId, classSubjectId, teacherId);
        var student = CreateStudent(classId);

        var existing = new Absence
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            ClassScheduleEntryId = scheduleEntry.Id,
            ClassSubjectId = classSubjectId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Type = AbsenceType.Lateness,
            Status = AbsenceStatus.Unexcused
        };

        _scheduleRepository.Setup(r => r.GetByIdAsync(scheduleEntry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleEntry);
        _studentRepository.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _absenceRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Absence> { existing });

        var result = await _sut.MarkAsync(teacherId, student.Id, scheduleEntry.Id);

        Assert.Equal(AbsenceType.Absence, result.Type);
        Assert.Equal(AbsenceStatus.Unexcused, result.Status);
        _absenceRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _absenceRepository.Verify(r => r.CreateAsync(It.IsAny<Absence>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsync_ThirdClick_CyclesBackToLateness()
    {
        var classId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var classSubjectId = Guid.NewGuid();

        var scheduleEntry = CreateScheduleEntry(classId, classSubjectId, teacherId);
        var student = CreateStudent(classId);

        var existing = new Absence
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            ClassScheduleEntryId = scheduleEntry.Id,
            ClassSubjectId = classSubjectId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow,
            Type = AbsenceType.Absence,
            Status = AbsenceStatus.Unexcused
        };

        _scheduleRepository.Setup(r => r.GetByIdAsync(scheduleEntry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleEntry);
        _studentRepository.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _absenceRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Absence> { existing });

        var result = await _sut.MarkAsync(teacherId, student.Id, scheduleEntry.Id);

        Assert.Equal(AbsenceType.Lateness, result.Type);
        Assert.Equal(AbsenceStatus.Unexcused, result.Status);
        _absenceRepository.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        _absenceRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsync_OutsideEditWindow_ThrowsInvalidOperationException()
    {
        var classId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var classSubjectId = Guid.NewGuid();

        var scheduleEntry = CreateScheduleEntry(classId, classSubjectId, teacherId);
        var student = CreateStudent(classId);

        var existing = new Absence
        {
            Id = Guid.NewGuid(),
            StudentId = student.Id,
            ClassScheduleEntryId = scheduleEntry.Id,
            ClassSubjectId = classSubjectId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow.AddMinutes(-25), // извън 20-минутния прозорец
            Type = AbsenceType.Lateness,
            Status = AbsenceStatus.Unexcused
        };

        _scheduleRepository.Setup(r => r.GetByIdAsync(scheduleEntry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleEntry);
        _studentRepository.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);
        _absenceRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Absence> { existing });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.MarkAsync(teacherId, student.Id, scheduleEntry.Id));

        _absenceRepository.Verify(r => r.UpdateAsync(It.IsAny<Absence>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkAsync_TeacherNotAssignedToClassSubject_ThrowsUnauthorized()
    {
        var classId = Guid.NewGuid();
        var actualTeacherId = Guid.NewGuid();
        var wrongTeacherId = Guid.NewGuid();
        var classSubjectId = Guid.NewGuid();

        var scheduleEntry = CreateScheduleEntry(classId, classSubjectId, actualTeacherId);
        var student = CreateStudent(classId);

        _scheduleRepository.Setup(r => r.GetByIdAsync(scheduleEntry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleEntry);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.MarkAsync(wrongTeacherId, student.Id, scheduleEntry.Id));
    }

    [Fact]
    public async Task MarkAsync_StudentNotInClass_ThrowsInvalidOperationException()
    {
        var classId = Guid.NewGuid();
        var otherClassId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var classSubjectId = Guid.NewGuid();

        var scheduleEntry = CreateScheduleEntry(classId, classSubjectId, teacherId);
        var student = CreateStudent(otherClassId); // друг клас

        _scheduleRepository.Setup(r => r.GetByIdAsync(scheduleEntry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scheduleEntry);
        _studentRepository.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.MarkAsync(teacherId, student.Id, scheduleEntry.Id));
    }

    // ---------- ExcuseAsync ----------

    [Fact]
    public async Task ExcuseAsync_Principal_CanExcuseAnyAbsence()
    {
        var studentClassId = Guid.NewGuid();
        var student = CreateStudent(studentClassId);
        var absence = new Absence { Id = Guid.NewGuid(), StudentId = student.Id, Status = AbsenceStatus.Unexcused };

        _absenceRepository.Setup(r => r.GetByIdAsync(absence.Id, It.IsAny<CancellationToken>())).ReturnsAsync(absence);
        _studentRepository.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);

        var result = await _sut.ExcuseAsync(absence.Id, Guid.NewGuid(), UserRole.Principal, AbsenceExcuseReason.HealthReasons, "бележка");

        Assert.Equal(AbsenceStatus.Excused, result.Status);
        Assert.Equal(AbsenceExcuseReason.HealthReasons, result.ExcuseReason);
        _absenceRepository.Verify(r => r.UpdateAsync(absence, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcuseAsync_MainClassTeacher_CanExcuse()
    {
        var classId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var student = CreateStudent(classId);
        var absence = new Absence { Id = Guid.NewGuid(), StudentId = student.Id, Status = AbsenceStatus.Unexcused };

        var teacher = new Teacher { Id = teacherId, MainClass = new Class { Id = classId } };

        _absenceRepository.Setup(r => r.GetByIdAsync(absence.Id, It.IsAny<CancellationToken>())).ReturnsAsync(absence);
        _studentRepository.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _teacherRepository.Setup(r => r.GetByIdAsync(teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(teacher);

        var result = await _sut.ExcuseAsync(absence.Id, teacherId, UserRole.Teacher, AbsenceExcuseReason.FamilyReasons, null);

        Assert.Equal(AbsenceStatus.Excused, result.Status);
    }

    [Fact]
    public async Task ExcuseAsync_TeacherNotMainClassTeacher_ThrowsUnauthorized()
    {
        var studentClassId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var student = CreateStudent(studentClassId);
        var absence = new Absence { Id = Guid.NewGuid(), StudentId = student.Id, Status = AbsenceStatus.Unexcused };

        // учителят е класен на съвсем друг клас
        var teacher = new Teacher { Id = teacherId, MainClass = new Class { Id = Guid.NewGuid() } };

        _absenceRepository.Setup(r => r.GetByIdAsync(absence.Id, It.IsAny<CancellationToken>())).ReturnsAsync(absence);
        _studentRepository.Setup(r => r.GetByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _teacherRepository.Setup(r => r.GetByIdAsync(teacherId, It.IsAny<CancellationToken>())).ReturnsAsync(teacher);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ExcuseAsync(absence.Id, teacherId, UserRole.Teacher, AbsenceExcuseReason.Other, null));
    }

    // ---------- GetByClassAsync ----------

    [Fact]
    public async Task GetByClassAsync_ReturnsOnlyAbsencesForStudentsInThatClass()
    {
        var classId = Guid.NewGuid();
        var otherClassId = Guid.NewGuid();

        var studentInClass = CreateStudent(classId);
        var studentInOtherClass = CreateStudent(otherClassId);

        var absenceInClass = new Absence { Id = Guid.NewGuid(), StudentId = studentInClass.Id };
        var absenceInOtherClass = new Absence { Id = Guid.NewGuid(), StudentId = studentInOtherClass.Id };

        _studentRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Student> { studentInClass, studentInOtherClass });
        _absenceRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Absence> { absenceInClass, absenceInOtherClass });

        var result = await _sut.GetByClassAsync(classId);

        var single = Assert.Single(result);
        Assert.Equal(absenceInClass.Id, single.Id);
    }

    [Fact]
    public async Task GetByClassAsync_NoStudentsInClass_ReturnsEmpty()
    {
        var classId = Guid.NewGuid();

        _studentRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Student>());

        var result = await _sut.GetByClassAsync(classId);

        Assert.Empty(result);
        _absenceRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- DeleteAsync (soft delete от администрация) ----------

    [Fact]
    public async Task DeleteAsync_Principal_SoftDeletesAbsence()
    {
        var absence = new Absence { Id = Guid.NewGuid(), IsDeleted = false };

        _absenceRepository.Setup(r => r.GetByIdAsync(absence.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(absence);

        await _sut.DeleteAsync(absence.Id, UserRole.Principal);

        Assert.True(absence.IsDeleted);
        _absenceRepository.Verify(r => r.UpdateAsync(absence, It.IsAny<CancellationToken>()), Times.Once);
        _absenceRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonPrincipal_ThrowsUnauthorized()
    {
        var absence = new Absence { Id = Guid.NewGuid() };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteAsync(absence.Id, UserRole.Teacher));

        _absenceRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}