using System.ComponentModel.DataAnnotations;
using Entities.Enums;
using Entities.Models;
using Services.Dtos.Registration;
using Services.Interfaces.Registration;
using Services.Interfaces.Security;
using Services.Repositories;

namespace Services.Services.Registration;

public class RegistrationService : IRegistrationService
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    private readonly IAccountsRepository accountsRepository;
    private readonly IStudentRepository studentRepository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IParentRepository parentRepository;
    private readonly IClassRepository classRepository;
    private readonly ISubjectRepository subjectRepository;
    private readonly IRegistrationInvitationRepository invitationRepository;
    private readonly IInvitationTokenService invitationTokenService;
    private readonly IRegistrationEmailSender registrationEmailSender;
    private readonly IPasswordHasher passwordHasher;

    public RegistrationService(
        IAccountsRepository accountsRepository,
        IStudentRepository studentRepository,
        ITeacherRepository teacherRepository,
        IParentRepository parentRepository,
        IClassRepository classRepository,
        ISubjectRepository subjectRepository,
        IRegistrationInvitationRepository invitationRepository,
        IInvitationTokenService invitationTokenService,
        IRegistrationEmailSender registrationEmailSender,
        IPasswordHasher passwordHasher)
    {
        this.accountsRepository = accountsRepository;
        this.studentRepository = studentRepository;
        this.teacherRepository = teacherRepository;
        this.parentRepository = parentRepository;
        this.classRepository = classRepository;
        this.subjectRepository = subjectRepository;
        this.invitationRepository = invitationRepository;
        this.invitationTokenService = invitationTokenService;
        this.registrationEmailSender = registrationEmailSender;
        this.passwordHasher = passwordHasher;
    }

    public async Task<RegistrationImportResult> ImportStudentsAsync(
        IReadOnlyCollection<StudentImportDto> students,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(students);

        var result = new RegistrationImportResult { TotalRows = students.Count };
        var importedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parentStudentIdsByEmail = new Dictionary<string, HashSet<Guid>>(StringComparer.OrdinalIgnoreCase);
        var schoolClasses = (await classRepository.GetAllAsync(cancellationToken)).ToList();

        foreach (var studentImport in students)
        {
            var email = NormalizeEmail(studentImport.Email);
            if (!ValidateStudentImport(studentImport, email, importedEmails, result))
            {
                continue;
            }

            var existingUser = await accountsRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUser is not null)
            {
                continue;
            }

            var schoolClass = await GetOrCreateClassAsync(studentImport.ClassLabel, schoolClasses, cancellationToken);
            if (schoolClass is null)
            {
                AddIssue(result, studentImport.RowNumber, email, "Класът не може да бъде създаден.");
                continue;
            }

            var user = CreateImportedUser(
                studentImport.FirstName,
                studentImport.MiddleName,
                studentImport.LastName,
                email,
                NormalizeOptional(studentImport.PhoneNumber),
                UserRole.Student);
            await accountsRepository.CreateAsync(user, cancellationToken);
            result.CreatedUsers++;

            var student = new Student
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BirthDate = studentImport.BirthDate,
                ClassId = schoolClass.Id
            };

            await studentRepository.CreateAsync(student, cancellationToken);
            result.CreatedProfiles++;

            await CreateInvitationAndSendEmailAsync(
                email,
                UserRole.Student,
                RegistrationInvitationType.SetPassword,
                user.Id,
                Array.Empty<Guid>(),
                cancellationToken);
            result.CreatedInvitations++;

            foreach (var parentEmail in GetValidParentEmails(studentImport, result))
            {
                if (!parentStudentIdsByEmail.TryGetValue(parentEmail, out var studentIds))
                {
                    studentIds = new HashSet<Guid>();
                    parentStudentIdsByEmail[parentEmail] = studentIds;
                }

                studentIds.Add(student.Id);
            }
        }

        foreach (var parentStudentIds in parentStudentIdsByEmail)
        {
            await InviteOrLinkParentAsync(parentStudentIds.Key, parentStudentIds.Value, result, cancellationToken);
        }

        return result;
    }

    public async Task<RegistrationImportResult> ImportTeachersAsync(
        IReadOnlyCollection<TeacherImportDto> teachers,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(teachers);

        var result = new RegistrationImportResult { TotalRows = teachers.Count };
        var importedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var subjects = (await subjectRepository.GetAllAsync(cancellationToken)).ToList();

        foreach (var teacherImport in teachers)
        {
            var email = NormalizeEmail(teacherImport.Email);
            if (!ValidateTeacherImport(teacherImport, email, importedEmails, result))
            {
                continue;
            }

            var existingUser = await accountsRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUser is not null)
            {
                continue;
            }

            var user = CreateImportedUser(
                teacherImport.FirstName,
                teacherImport.MiddleName,
                teacherImport.LastName,
                email,
                NormalizeOptional(teacherImport.PhoneNumber),
                UserRole.Teacher);

            await accountsRepository.CreateAsync(user, cancellationToken);
            result.CreatedUsers++;

            var taughtSubjects = await GetOrCreateSubjectsAsync(teacherImport.Subjects, subjects, cancellationToken);
            var teacherId = Guid.NewGuid();
            var teacher = new Teacher
            {
                Id = teacherId,
                UserId = user.Id,
                BirthDate = teacherImport.BirthDate,
                TeacherSubjects = taughtSubjects
                    .Select(subject => new TeacherSubject
                    {
                        Id = Guid.NewGuid(),
                        TeacherId = teacherId,
                        SubjectId = subject.Id
                    })
                    .ToList()
            };

            await teacherRepository.CreateAsync(teacher, cancellationToken);
            result.CreatedProfiles++;

            await CreateInvitationAndSendEmailAsync(
                email,
                UserRole.Teacher,
                RegistrationInvitationType.SetPassword,
                user.Id,
                Array.Empty<Guid>(),
                cancellationToken);
            result.CreatedInvitations++;
        }

        return result;
    }

    public async Task<RegistrationImportResult> ImportParentsAsync(
        IReadOnlyCollection<ParentImportDto> parents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parents);

        var result = new RegistrationImportResult { TotalRows = parents.Count };
        var importedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var parentImport in parents)
        {
            var email = NormalizeEmail(parentImport.Email);
            if (!ValidateParentImport(parentImport, email, importedEmails, result))
            {
                continue;
            }

            await InviteOrLinkParentAsync(email, Array.Empty<Guid>(), result, cancellationToken);
        }

        return result;
    }

    public async Task CompleteSetPasswordAsync(
        CompleteSetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureValidPassword(request.Password);

        var invitation = await GetValidInvitationAsync(
            request.Token,
            RegistrationInvitationType.SetPassword,
            cancellationToken);

        if (invitation.UserId is null)
        {
            throw new InvalidOperationException("Invitation is not connected to a user account.");
        }

        if (invitation.Role is not UserRole.Student and not UserRole.Teacher)
        {
            throw new InvalidOperationException("This invitation cannot be used to set a student or teacher password.");
        }

        var user = await accountsRepository.GetByIdAsync(invitation.UserId.Value, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        user.Password = passwordHasher.HashPassword(request.Password);
        await accountsRepository.UpdateAsync(user, cancellationToken);

        invitation.UsedAtUtc = DateTime.UtcNow;
        await invitationRepository.UpdateAsync(invitation, cancellationToken);
    }

    public async Task CompleteParentSignUpAsync(
        CompleteParentSignUpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureValidName(request.FirstName, nameof(request.FirstName));
        EnsureValidOptionalName(request.MiddleName, nameof(request.MiddleName));
        EnsureValidName(request.LastName, nameof(request.LastName));
        EnsureValidPassword(request.Password);

        var invitation = await GetValidInvitationAsync(
            request.Token,
            RegistrationInvitationType.ParentSignUp,
            cancellationToken);

        if (invitation.Role != UserRole.Parent)
        {
            throw new InvalidOperationException("This invitation cannot be used for parent sign-up.");
        }

        var user = await accountsRepository.GetByEmailAsync(invitation.Email, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = NormalizeRequired(request.FirstName),
                MiddleName = NormalizeOptional(request.MiddleName),
                LastName = NormalizeRequired(request.LastName),
                Email = invitation.Email,
                PhoneNumber = NormalizeOptional(request.PhoneNumber),
                Password = passwordHasher.HashPassword(request.Password),
                Role = UserRole.Parent
            };

            await accountsRepository.CreateAsync(user, cancellationToken);
        }
        else if (user.Role != UserRole.Parent)
        {
            throw new InvalidOperationException("Invitation email is already used by a non-parent account.");
        }
        else
        {
            user.FirstName = NormalizeRequired(request.FirstName);
            user.MiddleName = NormalizeOptional(request.MiddleName);
            user.LastName = NormalizeRequired(request.LastName);
            user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
            user.Password = passwordHasher.HashPassword(request.Password);
            await accountsRepository.UpdateAsync(user, cancellationToken);
        }

            await CreateOrUpdateParentProfileAsync(user.Id, invitation.Students.Select(student => student.Id).ToList(), cancellationToken);

        invitation.UserId = user.Id;
        invitation.UsedAtUtc = DateTime.UtcNow;
        await invitationRepository.UpdateAsync(invitation, cancellationToken);
    }

    public async Task ValidateInvitationAsync(
        string token,
        RegistrationInvitationType type,
        CancellationToken cancellationToken = default)
    {
        await GetValidInvitationAsync(token, type, cancellationToken);
    }

    public async Task<PrincipalSeedResult> SeedPrincipalAsync(
        SeedPrincipalRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SeedUserAsync(request, UserRole.Principal, cancellationToken);
    }

    public async Task<PrincipalSeedResult> SeedUserAsync(
        SeedPrincipalRequest request,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureValidName(request.FirstName, nameof(request.FirstName));
        EnsureValidOptionalName(request.MiddleName, nameof(request.MiddleName));
        EnsureValidName(request.LastName, nameof(request.LastName));
        EnsureValidEmail(request.Email, nameof(request.Email));
        EnsureValidPassword(request.Password);

        var email = NormalizeEmail(request.Email);
        var existingUser = await accountsRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            if (existingUser.Role != role)
            {
                throw new InvalidOperationException($"A non-{role.ToString().ToLowerInvariant()} account already uses this email.");
            }

            return new PrincipalSeedResult
            {
                UserId = existingUser.Id,
                Created = false
            };
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = NormalizeRequired(request.FirstName),
            MiddleName = NormalizeOptional(request.MiddleName),
            LastName = NormalizeRequired(request.LastName),
            Email = email,
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            Password = passwordHasher.HashPassword(request.Password),
            Role = role
        };

        await accountsRepository.CreateAsync(user, cancellationToken);

        return new PrincipalSeedResult
        {
            UserId = user.Id,
            Created = true
        };
    }

    private async Task InviteOrLinkParentAsync(
        string parentEmail,
        IReadOnlyCollection<Guid> studentIds,
        RegistrationImportResult result,
        CancellationToken cancellationToken)
    {
        var existingUser = await accountsRepository.GetByEmailAsync(parentEmail, cancellationToken);
        if (existingUser is not null)
        {
            if (existingUser.Role != UserRole.Parent)
            {
                return;
            }

            var createdProfile = await CreateOrUpdateParentProfileAsync(existingUser.Id, studentIds, cancellationToken);
            if (createdProfile)
            {
                result.CreatedProfiles++;
            }

            return;
        }

        var activeInvitations = await invitationRepository.GetActiveByEmailAsync(parentEmail, cancellationToken);
        var activeParentInvitation = activeInvitations.FirstOrDefault(invitation =>
            invitation.Type == RegistrationInvitationType.ParentSignUp &&
            invitation.UsedAtUtc is null &&
            invitation.ExpiresAtUtc > DateTime.UtcNow);

        if (activeParentInvitation is not null)
        {
            foreach (var studentId in studentIds)
            {
                if (activeParentInvitation.Students.All(student => student.Id != studentId))
                {
                    var student = await studentRepository.GetByIdAsync(studentId, cancellationToken)
                        ?? throw new InvalidOperationException("Student was not found.");

                    activeParentInvitation.Students.Add(student);
                }
            }

            await invitationRepository.UpdateAsync(activeParentInvitation, cancellationToken);
            return;
        }

        await CreateInvitationAndSendEmailAsync(
            parentEmail,
            UserRole.Parent,
            RegistrationInvitationType.ParentSignUp,
            null,
            studentIds,
            cancellationToken);
        result.CreatedInvitations++;
    }

    private async Task<bool> CreateOrUpdateParentProfileAsync(
        Guid parentUserId,
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        var students = await GetStudentsByIdsAsync(studentIds, cancellationToken);
        var parents = await parentRepository.GetAllAsync(cancellationToken);
        var parent = parents.FirstOrDefault(existingParent => existingParent.UserId == parentUserId);

        if (parent is null)
        {
            parent = new Parent
            {
                Id = Guid.NewGuid(),
                UserId = parentUserId,
                Students = students.ToList()
            };

            await parentRepository.CreateAsync(parent, cancellationToken);
            return true;
        }

        var existingStudentIds = parent.Students
            .Select(student => student.Id)
            .ToHashSet();

        foreach (var student in students.Where(student => !existingStudentIds.Contains(student.Id)))
        {
            parent.Students.Add(student);
        }

        await parentRepository.UpdateAsync(parent, cancellationToken);
        return false;
    }

    private async Task<IReadOnlyList<Student>> GetStudentsByIdsAsync(
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        var students = new List<Student>();
        foreach (var studentId in studentIds.Distinct())
        {
            var student = await studentRepository.GetByIdAsync(studentId, cancellationToken)
                ?? throw new InvalidOperationException("Invitation points to a student that no longer exists.");

            students.Add(student);
        }

        return students;
    }

    private async Task CreateInvitationAndSendEmailAsync(
        string email,
        UserRole role,
        RegistrationInvitationType type,
        Guid? userId,
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        var token = invitationTokenService.GenerateToken();
        var invitation = new RegistrationInvitation
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserId = userId,
            Role = role,
            Type = type,
            TokenHash = invitationTokenService.HashToken(token),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(InvitationLifetime),
            Students = type == RegistrationInvitationType.ParentSignUp
                ? (await GetStudentsByIdsAsync(studentIds, cancellationToken)).ToList()
                : new List<Student>()
        };

        await invitationRepository.CreateAsync(invitation, cancellationToken);
        await registrationEmailSender.SendInvitationAsync(
            new RegistrationEmailRequest
            {
                Email = email,
                Role = role,
                Type = type,
                Token = token,
                ExpiresAtUtc = invitation.ExpiresAtUtc
            },
            cancellationToken);
    }

    private async Task<RegistrationInvitation> GetValidInvitationAsync(
        string token,
        RegistrationInvitationType type,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be empty.", nameof(token));
        }

        var tokenHash = invitationTokenService.HashToken(token);
        var invitation = await invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new InvalidOperationException("Invitation was not found.");

        if (invitation.Type != type)
        {
            throw new InvalidOperationException("Invitation type is invalid.");
        }

        if (invitation.UsedAtUtc is not null)
        {
            throw new InvalidOperationException("Invitation has already been used.");
        }

        if (invitation.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Invitation has expired.");
        }

        return invitation;
    }

    private static User CreateImportedUser(
        string firstName,
        string? middleName,
        string lastName,
        string email,
        string? phoneNumber,
        UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FirstName = NormalizeRequired(firstName),
            MiddleName = NormalizeOptional(middleName),
            LastName = NormalizeRequired(lastName),
            Email = email,
            PhoneNumber = phoneNumber,
            Password = string.Empty,
            Role = role
        };
    }

    private static bool ValidateStudentImport(
        StudentImportDto studentImport,
        string email,
        HashSet<string> importedEmails,
        RegistrationImportResult result)
    {
        var isValid = true;

        isValid &= ValidateImportedName(studentImport.FirstName, studentImport.RowNumber, email, "Името", result);
        isValid &= ValidateOptionalImportedName(studentImport.MiddleName, studentImport.RowNumber, email, "Презимето", result);
        isValid &= ValidateImportedName(studentImport.LastName, studentImport.RowNumber, email, "Фамилията", result);
        isValid &= ValidateImportedEmail(email, importedEmails, studentImport.RowNumber, result);

        if (!TryParseClassLabel(studentImport.ClassLabel, out _, out _))
        {
            AddIssue(result, studentImport.RowNumber, email, "Класът е задължителен и трябва да бъде във формат като '1 Б'.");
            isValid = false;
        }

        if (studentImport.BirthDate == default)
        {
            AddIssue(result, studentImport.RowNumber, email, "Датата на раждане е задължителна.");
            isValid = false;
        }

        return isValid;
    }

    private async Task<Class?> GetOrCreateClassAsync(
        string? classLabel,
        List<Class> schoolClasses,
        CancellationToken cancellationToken)
    {
        if (!TryParseClassLabel(classLabel, out var gradeNumber, out var letter))
        {
            return null;
        }

        var matchingClass = schoolClasses.FirstOrDefault(currentClass =>
            currentClass.GradeNumber == gradeNumber &&
            char.ToUpperInvariant(currentClass.Letter) == letter);

        if (matchingClass is not null)
        {
            return matchingClass;
        }

        var schoolClass = new Class
        {
            Id = Guid.NewGuid(),
            GradeNumber = gradeNumber,
            Letter = letter
        };

        await classRepository.CreateAsync(schoolClass, cancellationToken);
        schoolClasses.Add(schoolClass);

        return schoolClass;
    }

    private static bool TryParseClassLabel(string? classLabel, out int gradeNumber, out char letter)
    {
        gradeNumber = default;
        letter = default;

        if (string.IsNullOrWhiteSpace(classLabel))
        {
            return false;
        }

        var trimmedLabel = classLabel.Trim();
        var digitCount = 0;
        while (digitCount < trimmedLabel.Length && char.IsDigit(trimmedLabel[digitCount]))
        {
            digitCount++;
        }

        if (digitCount == 0 || !int.TryParse(trimmedLabel[..digitCount], out gradeNumber) || gradeNumber <= 0)
        {
            return false;
        }

        var letterPart = trimmedLabel[digitCount..].Trim(' ', '-', '/', '.');
        if (letterPart.Length == 0)
        {
            return false;
        }

        letter = char.ToUpperInvariant(letterPart[0]);
        return char.IsLetter(letter);
    }

    private static bool ValidateTeacherImport(
        TeacherImportDto teacherImport,
        string email,
        HashSet<string> importedEmails,
        RegistrationImportResult result)
    {
        var isValid = true;

        isValid &= ValidateImportedName(teacherImport.FirstName, teacherImport.RowNumber, email, "Името", result);
        isValid &= ValidateOptionalImportedName(teacherImport.MiddleName, teacherImport.RowNumber, email, "Презимето", result);
        isValid &= ValidateImportedName(teacherImport.LastName, teacherImport.RowNumber, email, "Фамилията", result);
        isValid &= ValidateImportedEmail(email, importedEmails, teacherImport.RowNumber, result);

        if (teacherImport.BirthDate == default)
        {
            AddIssue(result, teacherImport.RowNumber, email, "Датата на раждане е задължителна.");
            isValid = false;
        }

        if (!GetNormalizedSubjectNames(teacherImport.Subjects).Any())
        {
            AddIssue(result, teacherImport.RowNumber, email, "Необходим е поне един предмет.");
            isValid = false;
        }

        return isValid;
    }

    private async Task<IReadOnlyList<Subject>> GetOrCreateSubjectsAsync(
        IReadOnlyCollection<string> subjectNames,
        List<Subject> subjects,
        CancellationToken cancellationToken)
    {
        var result = new List<Subject>();

        foreach (var subjectName in GetNormalizedSubjectNames(subjectNames))
        {
            var subject = subjects.FirstOrDefault(existingSubject =>
                string.Equals(existingSubject.Name, subjectName, StringComparison.OrdinalIgnoreCase));

            if (subject is null)
            {
                subject = new Subject
                {
                    Id = Guid.NewGuid(),
                    Name = subjectName
                };

                await subjectRepository.CreateAsync(subject, cancellationToken);
                subjects.Add(subject);
            }

            result.Add(subject);
        }

        return result;
    }

    private static IReadOnlyList<string> GetNormalizedSubjectNames(IReadOnlyCollection<string>? subjectNames)
    {
        return (subjectNames ?? Array.Empty<string>())
            .Select(NormalizeOptional)
            .Where(subjectName => subjectName is not null)
            .Select(subjectName => subjectName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ValidateParentImport(
        ParentImportDto parentImport,
        string email,
        HashSet<string> importedEmails,
        RegistrationImportResult result)
    {
        return ValidateImportedEmail(email, importedEmails, parentImport.RowNumber, result);
    }

    private static bool ValidateImportedName(
        string? value,
        int? rowNumber,
        string? email,
        string fieldName,
        RegistrationImportResult result)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 100)
        {
            return true;
        }

        AddIssue(result, rowNumber, email, $"{fieldName} е задължително и не може да бъде по-дълго от 100 символа.");
        return false;
    }

    private static bool ValidateOptionalImportedName(
        string? value,
        int? rowNumber,
        string? email,
        string fieldName,
        RegistrationImportResult result)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 100)
        {
            return true;
        }

        AddIssue(result, rowNumber, email, $"{fieldName} не може да бъде по-дълго от 100 символа.");
        return false;
    }

    private static bool ValidateImportedEmail(
        string email,
        HashSet<string> importedEmails,
        int? rowNumber,
        RegistrationImportResult result)
    {
        if (!IsValidEmail(email))
        {
            AddIssue(result, rowNumber, email, "Имейлът е невалиден.");
            return false;
        }

        if (!importedEmails.Add(email))
        {
            AddIssue(result, rowNumber, email, "Имейлът се повтаря в таблицата.");
            return false;
        }

        return true;
    }

    private static IEnumerable<string> GetValidParentEmails(
        StudentImportDto studentImport,
        RegistrationImportResult result)
    {
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parentEmail in studentImport.ParentEmails ?? Array.Empty<string>())
        {
            var normalizedEmail = NormalizeEmail(parentEmail);
            if (!IsValidEmail(normalizedEmail))
            {
                AddIssue(result, studentImport.RowNumber, normalizedEmail, "Имейлът на родителя е невалиден.");
                continue;
            }

            emails.Add(normalizedEmail);
        }

        return emails;
    }

    private static void EnsureValidName(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 100)
        {
            throw new ArgumentException("Name is required and cannot exceed 100 characters.", parameterName);
        }
    }

    private static void EnsureValidOptionalName(string? value, string parameterName)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > 100)
        {
            throw new ArgumentException("Name cannot exceed 100 characters.", parameterName);
        }
    }

    private static void EnsureValidEmail(string? value, string parameterName)
    {
        if (!IsValidEmail(NormalizeEmail(value)))
        {
            throw new ArgumentException("Email is invalid.", parameterName);
        }
    }

    private static void EnsureValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters.", nameof(password));
        }
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string NormalizeEmail(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
               new EmailAddressAttribute().IsValid(email);
    }

    private static void AddIssue(RegistrationImportResult result, int? rowNumber, string? email, string message)
    {
        result.Issues.Add(new RegistrationImportIssue
        {
            RowNumber = rowNumber,
            Email = string.IsNullOrWhiteSpace(email) ? null : email,
            Message = message
        });
    }
}
