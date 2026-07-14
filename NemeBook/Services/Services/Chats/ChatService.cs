using Entities.Enums;
using Entities.Models;
using Services.Interfaces.Chats;
using Services.Repositories;

namespace Services.Services.Chats;

public class ChatService : IChatService
{
    private const string ClassChatPrefix = "CLASS:";
    private const string TeachersGroupChatName = "TEACHERS:GROUP";

    private readonly IChatRepository chatRepository;
    private readonly IMessageRepository messageRepository;
    private readonly IUserRepository userRepository;
    private readonly IStudentRepository studentRepository;
    private readonly IParentRepository parentRepository;
    private readonly ITeacherRepository teacherRepository;
    private readonly IClassRepository classRepository;
    private readonly IClassSubjectRepository classSubjectRepository;

    public ChatService(
        IChatRepository chatRepository,
        IMessageRepository messageRepository,
        IUserRepository userRepository,
        IStudentRepository studentRepository,
        IParentRepository parentRepository,
        ITeacherRepository teacherRepository,
        IClassRepository classRepository,
        IClassSubjectRepository classSubjectRepository)
    {
        this.chatRepository = chatRepository;
        this.messageRepository = messageRepository;
        this.userRepository = userRepository;
        this.studentRepository = studentRepository;
        this.parentRepository = parentRepository;
        this.teacherRepository = teacherRepository;
        this.classRepository = classRepository;
        this.classSubjectRepository = classSubjectRepository;
    }

    public async Task<IReadOnlyList<Chat>> GetChatsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var requester = await GetUserOrThrowAsync(userId, cancellationToken);
        if (requester.Role == UserRole.Student)
        {
            await EnsureStudentClassChatAsync(userId, cancellationToken);
        }
        else if (requester.Role == UserRole.Teacher)
        {
            await EnsureTeacherMainClassChatsAsync(userId, cancellationToken);
        }

        var chats = await chatRepository.GetAllAsync(cancellationToken);
        return chats
            .Where(chat => chat.Users.Any(user => user.Id == userId))
            .Where(chat => !IsCustomGroupChat(chat))
            .Where(chat => !IsDirectChatWithSelf(userId, chat))
            .Where(chat => !IsDirectChatWithInactiveParticipant(chat))
            .Where(chat => !IsDirectChatWithPrincipalForRestrictedRole(requester, chat))
            .Where(chat => CanUserSeeChat(requester, chat))
            .ToList();
    }

    public async Task<Chat> GetChatByIdAsync(Guid requesterUserId, Guid chatId, CancellationToken cancellationToken = default)
    {
        await EnsureUserCanAccessChatAsync(requesterUserId, chatId, cancellationToken);

        return await chatRepository.GetByIdAsync(chatId, cancellationToken)
            ?? throw new InvalidOperationException("Chat was not found.");
    }

    public async Task<IReadOnlyList<Message>> GetMessagesAsync(Guid requesterUserId, Guid chatId, CancellationToken cancellationToken = default)
    {
        await EnsureUserCanAccessChatAsync(requesterUserId, chatId, cancellationToken);

        var messages = await messageRepository.GetAllAsync(cancellationToken);
        return messages
            .Where(message => message.ChatId == chatId && !message.IsDeleted)
            .OrderBy(message => message.SentAt)
            .ToList();
    }

    public async Task<IReadOnlyList<User>> SearchAvailableContactsAsync(Guid requesterUserId, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var allowedContactIds = await GetAllowedDirectContactIdsAsync(requesterUserId, cancellationToken);
        var users = await userRepository.GetAllAsync(cancellationToken);

        var query = users.Where(user =>
            user.Id != requesterUserId &&
            allowedContactIds.Contains(user.Id) &&
            !user.IsDeleted &&
            user.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim();
            query = query.Where(user =>
                FormatSearchName(user).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                user.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (user.MiddleName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                user.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                user.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.Email)
            .ToList();
    }

    public async Task<Chat> GetOrCreateDirectChatAsync(Guid requesterUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        if (requesterUserId == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторът на потребителя заявител не може да бъде празен.", nameof(requesterUserId));
        }

        if (targetUserId == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторът на избрания потребител не може да бъде празен.", nameof(targetUserId));
        }

        if (requesterUserId == targetUserId)
        {
            throw new InvalidOperationException("Не може да започнете чат със същия потребител.");
        }

        var allowedContactIds = await GetAllowedDirectContactIdsAsync(requesterUserId, cancellationToken);
        if (!allowedContactIds.Contains(targetUserId))
        {
            throw new InvalidOperationException("Този директен чат не е разрешен за текущата потребителска роля.");
        }

        var requester = await GetUserOrThrowAsync(requesterUserId, cancellationToken);
        var target = await GetUserOrThrowAsync(targetUserId, cancellationToken);
        if (target.IsDeleted || !target.IsActive)
        {
            throw new InvalidOperationException("Избраният потребител не е активен.");
        }

        var chats = await chatRepository.GetAllAsync(cancellationToken);
        var existingDirectChat = chats.FirstOrDefault(chat =>
            string.IsNullOrWhiteSpace(chat.Name) &&
            chat.Users.Count == 2 &&
            chat.Users.Any(user => user.Id == requesterUserId) &&
            chat.Users.Any(user => user.Id == targetUserId));

        if (existingDirectChat is not null)
        {
            return existingDirectChat;
        }

        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            Users = new List<User> { requester, target }
        };

        await chatRepository.CreateAsync(chat, cancellationToken);
        return chat;
    }

    public async Task<Chat> GetOrCreateClassChatAsync(Guid creatorUserId, Guid classId, CancellationToken cancellationToken = default)
    {
        if (classId == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторът на класа не може да бъде празен.", nameof(classId));
        }

        var creator = await GetUserOrThrowAsync(creatorUserId, cancellationToken);
        if (creator.Role is not UserRole.Teacher and not UserRole.Principal)
        {
            throw new InvalidOperationException("Само учител или директор може да създаде чат на клас.");
        }

        if (creator.Role == UserRole.Teacher)
        {
            var teacherClassIds = await GetTeacherClassIdsByUserIdAsync(creatorUserId, cancellationToken);
            if (!teacherClassIds.Contains(classId))
            {
                throw new InvalidOperationException("Учителят не е свързан с този клас.");
            }
        }

        var schoolClass = await classRepository.GetByIdAsync(classId, cancellationToken)
            ?? throw new InvalidOperationException("Класът не беше намерен.");

        return await EnsureClassChatAsync(schoolClass, cancellationToken)
            ?? throw new InvalidOperationException("Класният чат не може да бъде създаден без участници.");
    }

    public async Task<Chat> GetOrCreateTeachersGroupChatAsync(Guid creatorUserId, CancellationToken cancellationToken = default)
    {
        var creator = await GetUserOrThrowAsync(creatorUserId, cancellationToken);
        if (creator.Role is not UserRole.Teacher and not UserRole.Principal)
        {
            throw new InvalidOperationException("Само учител или директор може да създаде групов чат за учители.");
        }

        var chats = await chatRepository.GetAllAsync(cancellationToken);
        var existingTeachersGroup = chats.FirstOrDefault(chat => chat.Name == TeachersGroupChatName);

        if (existingTeachersGroup is not null)
        {
            return existingTeachersGroup;
        }

        var users = await userRepository.GetAllAsync(cancellationToken);
        var participants = users
            .Where(user => user.IsActive && (user.Role is UserRole.Teacher or UserRole.Principal))
            .ToList();

        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            Name = TeachersGroupChatName,
            Users = participants
        };

        await chatRepository.CreateAsync(chat, cancellationToken);
        return chat;
    }

    public async Task<Message> SendMessageAsync(Guid senderUserId, Guid chatId, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Текстът на съобщението не може да бъде празен.", nameof(text));
        }

        if (text.Length > 4000)
        {
            throw new ArgumentException("Текстът на съобщението не може да бъде по-дълъг от 4000 символа.", nameof(text));
        }

        await EnsureUserCanAccessChatAsync(senderUserId, chatId, cancellationToken);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            SenderId = senderUserId,
            Text = text.Trim(),
            SentAt = DateTime.UtcNow,
            IsEdited = false,
            IsDeleted = false
        };

        await messageRepository.CreateAsync(message, cancellationToken);
        return message;
    }

    private async Task<User> GetUserOrThrowAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторът на потребителя не може да бъде празен.", nameof(userId));
        }

        return await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Потребителят не беше намерен.");
    }

    private async Task EnsureUserCanAccessChatAsync(Guid userId, Guid chatId, CancellationToken cancellationToken)
    {
        var requester = await GetUserOrThrowAsync(userId, cancellationToken);
        var chat = await chatRepository.GetByIdAsync(chatId, cancellationToken)
            ?? throw new InvalidOperationException("Чатът не беше намерен.");

        if (chat.Users.All(user => user.Id != userId))
        {
            throw new UnauthorizedAccessException("User is not part of this chat.");
        }

        if (IsCustomGroupChat(chat))
        {
            throw new UnauthorizedAccessException("Custom group chats are disabled.");
        }

        if (IsDirectChatWithSelf(userId, chat))
        {
            throw new UnauthorizedAccessException("Self chats are disabled.");
        }

        if (IsDirectChatWithInactiveParticipant(chat))
        {
            throw new UnauthorizedAccessException("Inactive direct chat participants are hidden.");
        }

        if (IsDirectChatWithPrincipalForRestrictedRole(requester, chat))
        {
            throw new UnauthorizedAccessException("Students and parents cannot access principal direct chats.");
        }

        if (!CanUserSeeChat(requester, chat))
        {
            throw new UnauthorizedAccessException("User is not allowed to access this chat.");
        }
    }

    private static bool IsCustomGroupChat(Chat chat)
    {
        return chat.Name?.StartsWith("GROUP:", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsDirectChatWithSelf(Guid userId, Chat chat)
    {
        return string.IsNullOrWhiteSpace(chat.Name)
            && chat.Users.Count <= 1
            && chat.Users.Any(user => user.Id == userId);
    }

    private static bool IsDirectChatWithInactiveParticipant(Chat chat)
    {
        return string.IsNullOrWhiteSpace(chat.Name)
            && chat.Users.Count == 2
            && chat.Users.Any(user => user.IsDeleted || !user.IsActive);
    }

    private static bool IsDirectChatWithPrincipalForRestrictedRole(User requester, Chat chat)
    {
        return requester.Role is UserRole.Student or UserRole.Parent
            && string.IsNullOrWhiteSpace(chat.Name)
            && chat.Users.Count == 2
            && chat.Users.Any(user => user.Role == UserRole.Principal);
    }

    private static bool CanUserSeeChat(User requester, Chat chat)
    {
        return true;
    }

    private async Task EnsureStudentClassChatAsync(Guid studentUserId, CancellationToken cancellationToken)
    {
        var students = await studentRepository.GetAllAsync(cancellationToken);
        var student = students.FirstOrDefault(currentStudent => currentStudent.UserId == studentUserId);
        if (student is null || student.User.IsDeleted || !student.User.IsActive)
        {
            return;
        }

        var schoolClass = await classRepository.GetByIdAsync(student.ClassId, cancellationToken);
        if (schoolClass is null)
        {
            return;
        }

        await EnsureClassChatAsync(schoolClass, cancellationToken);
    }

    private async Task EnsureTeacherMainClassChatsAsync(Guid teacherUserId, CancellationToken cancellationToken)
    {
        var teachers = await teacherRepository.GetAllAsync(cancellationToken);
        var teacher = teachers.FirstOrDefault(currentTeacher => currentTeacher.UserId == teacherUserId);
        if (teacher is null || teacher.User.IsDeleted || !teacher.User.IsActive)
        {
            return;
        }

        var classes = await classRepository.GetAllAsync(cancellationToken);
        var mainClasses = classes.Where(schoolClass => schoolClass.MainTeacherId == teacher.Id);
        foreach (var schoolClass in mainClasses)
        {
            await EnsureClassChatAsync(schoolClass, cancellationToken);
        }
    }

    private async Task<Chat?> EnsureClassChatAsync(Class schoolClass, CancellationToken cancellationToken)
    {
        var chatName = $"{ClassChatPrefix}{schoolClass.Id}";
        var chats = await chatRepository.GetAllAsync(cancellationToken);
        var existingClassChat = chats.FirstOrDefault(chat => chat.Name == chatName);
        var participants = await GetClassChatParticipantsAsync(schoolClass, cancellationToken);

        if (existingClassChat is null)
        {
            if (participants.Count == 0)
            {
                return null;
            }

            await chatRepository.CreateAsync(new Chat
            {
                Id = Guid.NewGuid(),
                Name = chatName,
                Users = participants.ToList()
            }, cancellationToken);
            return (await chatRepository.GetAllAsync(cancellationToken))
                .FirstOrDefault(chat => chat.Name == chatName);
        }

        var participantIds = participants.Select(user => user.Id).ToHashSet();
        var existingParticipantIds = existingClassChat.Users.Select(user => user.Id).ToHashSet();
        if (participantIds.SetEquals(existingParticipantIds))
        {
            return existingClassChat;
        }

        existingClassChat.Users = participants.ToList();
        await chatRepository.UpdateAsync(existingClassChat, cancellationToken);
        return existingClassChat;
    }

    private async Task<IReadOnlyList<User>> GetClassChatParticipantsAsync(Class schoolClass, CancellationToken cancellationToken)
    {
        var participantIds = schoolClass.Students
            .Where(student => student.User.IsActive && !student.User.IsDeleted)
            .Select(student => student.UserId)
            .ToHashSet();

        if (schoolClass.MainTeacher?.User is { IsActive: true, IsDeleted: false } mainTeacherUser)
        {
            participantIds.Add(mainTeacherUser.Id);
        }

        var users = await userRepository.GetAllAsync(cancellationToken);
        return users
            .Where(user => participantIds.Contains(user.Id) && user.IsActive && !user.IsDeleted)
            .ToList();
    }

    private async Task<HashSet<Guid>> GetAllowedDirectContactIdsAsync(Guid requesterUserId, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var requester = users.FirstOrDefault(user => user.Id == requesterUserId)
            ?? throw new InvalidOperationException("Потребителят не беше намерен.");
        var students = await studentRepository.GetAllAsync(cancellationToken);
        var parents = await parentRepository.GetAllAsync(cancellationToken);
        var teachers = await teacherRepository.GetAllAsync(cancellationToken);
        var classes = await classRepository.GetAllAsync(cancellationToken);
        var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);

        var teacherUserByTeacherId = teachers
            .Where(teacher => teacher.User.IsActive && !teacher.User.IsDeleted)
            .ToDictionary(teacher => teacher.Id, teacher => teacher.UserId);
        HashSet<Guid> allowedIds = requester.Role switch
        {
            UserRole.Student => GetAllowedForStudent(requesterUserId, students, classSubjects, classes, teacherUserByTeacherId),
            UserRole.Parent => GetAllowedForParent(requesterUserId, parents, classSubjects, classes, teacherUserByTeacherId),
            UserRole.Teacher => GetAllowedForTeacher(requesterUserId, teachers, students, parents, classes, classSubjects),
            UserRole.Principal => GetAllowedForPrincipal(users),
            _ => new HashSet<Guid>()
        };

        allowedIds.Remove(requesterUserId);
        return allowedIds;
    }

    private static HashSet<Guid> GetAllowedForStudent(
        Guid requesterUserId,
        IReadOnlyList<Student> students,
        IReadOnlyList<ClassSubject> classSubjects,
        IReadOnlyList<Class> classes,
        IReadOnlyDictionary<Guid, Guid> teacherUserByTeacherId)
    {
        var student = students.FirstOrDefault(currentStudent => currentStudent.UserId == requesterUserId)
            ?? throw new InvalidOperationException("Профилът на ученика не беше намерен.");

        var classTeacherIds = classSubjects
            .Where(classSubject => classSubject.ClassId == student.ClassId)
            .Where(classSubject => classSubject.TeacherId.HasValue)
            .Select(classSubject => classSubject.TeacherId!.Value)
            .ToHashSet();

        var mainTeacherId = classes
            .FirstOrDefault(currentClass => currentClass.Id == student.ClassId)
            ?.MainTeacherId;

        if (mainTeacherId.HasValue)
        {
            classTeacherIds.Add(mainTeacherId.Value);
        }

        return classTeacherIds
            .Where(teacherUserByTeacherId.ContainsKey)
            .Select(teacherId => teacherUserByTeacherId[teacherId])
            .ToHashSet();
    }

    private static HashSet<Guid> GetAllowedForParent(
        Guid requesterUserId,
        IReadOnlyList<Parent> parents,
        IReadOnlyList<ClassSubject> classSubjects,
        IReadOnlyList<Class> classes,
        IReadOnlyDictionary<Guid, Guid> teacherUserByTeacherId)
    {
        var parent = parents.FirstOrDefault(currentParent => currentParent.UserId == requesterUserId)
            ?? throw new InvalidOperationException("Профилът на родителя не беше намерен.");

        var childClassIds = parent.Students
            .Where(student => student.User.IsActive && !student.User.IsDeleted)
            .Select(student => student.ClassId)
            .ToHashSet();

        var teacherIds = classSubjects
            .Where(classSubject => childClassIds.Contains(classSubject.ClassId))
            .Where(classSubject => classSubject.TeacherId.HasValue)
            .Select(classSubject => classSubject.TeacherId!.Value)
            .ToHashSet();

        foreach (var mainTeacherId in classes
                     .Where(currentClass => childClassIds.Contains(currentClass.Id))
                     .Select(currentClass => currentClass.MainTeacherId))
        {
            if (mainTeacherId.HasValue)
            {
                teacherIds.Add(mainTeacherId.Value);
            }
        }

        return teacherIds
            .Where(teacherUserByTeacherId.ContainsKey)
            .Select(teacherId => teacherUserByTeacherId[teacherId])
            .ToHashSet();
    }

    private static HashSet<Guid> GetAllowedForTeacher(
        Guid requesterUserId,
        IReadOnlyList<Teacher> teachers,
        IReadOnlyList<Student> students,
        IReadOnlyList<Parent> parents,
        IReadOnlyList<Class> classes,
        IReadOnlyList<ClassSubject> classSubjects)
    {
        var teacher = teachers.FirstOrDefault(currentTeacher => currentTeacher.UserId == requesterUserId)
            ?? throw new InvalidOperationException("Профилът на учителя не беше намерен.");

        var classIds = classSubjects
            .Where(classSubject => classSubject.TeacherId == teacher.Id)
            .Select(classSubject => classSubject.ClassId)
            .ToHashSet();

        foreach (var classId in classes
                     .Where(currentClass => currentClass.MainTeacherId == teacher.Id)
                     .Select(currentClass => currentClass.Id))
        {
            classIds.Add(classId);
        }

        var studentUserIds = students
            .Where(student => classIds.Contains(student.ClassId))
            .Where(student => student.User.IsActive && !student.User.IsDeleted)
            .Select(student => student.UserId)
            .ToHashSet();

        var parentUserIds = parents
            .Where(parent => parent.User.IsActive && !parent.User.IsDeleted)
            .Where(parent => parent.Students.Any(student =>
                classIds.Contains(student.ClassId) &&
                student.User.IsActive &&
                !student.User.IsDeleted))
            .Select(parent => parent.UserId)
            .ToHashSet();

        foreach (var parentUserId in parentUserIds)
        {
            studentUserIds.Add(parentUserId);
        }

        return studentUserIds;
    }

    private static HashSet<Guid> GetAllowedForPrincipal(IReadOnlyList<User> users)
    {
        return users
            .Where(user => !user.IsDeleted)
            .Where(user => user.IsActive)
            .Select(user => user.Id)
            .ToHashSet();
    }

    private static string FormatSearchName(User user)
    {
        return string.Join(
            " ",
            new[] { user.FirstName, user.MiddleName, user.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private async Task<HashSet<Guid>> GetTeacherClassIdsByUserIdAsync(Guid teacherUserId, CancellationToken cancellationToken)
    {
        var teachers = await teacherRepository.GetAllAsync(cancellationToken);
        var classes = await classRepository.GetAllAsync(cancellationToken);
        var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);

        var teacher = teachers.FirstOrDefault(currentTeacher => currentTeacher.UserId == teacherUserId)
            ?? throw new InvalidOperationException("Профилът на учителя не беше намерен.");

        var classIds = classSubjects
            .Where(classSubject => classSubject.TeacherId == teacher.Id)
            .Select(classSubject => classSubject.ClassId)
            .ToHashSet();

        foreach (var classId in classes
                     .Where(currentClass => currentClass.MainTeacherId == teacher.Id)
                     .Select(currentClass => currentClass.Id))
        {
            classIds.Add(classId);
        }

        return classIds;
    }
}
