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

        var chats = await chatRepository.GetAllAsync(cancellationToken);
        return chats
            .Where(chat => chat.Users.Any(user => user.Id == userId))
            .Where(chat => !IsCustomGroupChat(chat))
            .Where(chat => CanUserSeeChat(requester, chat))
            .ToList();
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

        var query = users.Where(user => allowedContactIds.Contains(user.Id) && !user.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim();
            query = query.Where(user =>
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
            throw new ArgumentException("Requester user id cannot be empty.", nameof(requesterUserId));
        }

        if (targetUserId == Guid.Empty)
        {
            throw new ArgumentException("Target user id cannot be empty.", nameof(targetUserId));
        }

        if (requesterUserId == targetUserId)
        {
            throw new InvalidOperationException("Cannot start chat with the same user.");
        }

        var allowedContactIds = await GetAllowedDirectContactIdsAsync(requesterUserId, cancellationToken);
        if (!allowedContactIds.Contains(targetUserId))
        {
            throw new InvalidOperationException("This direct chat is not allowed for the current user role.");
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

        var requester = await GetUserOrThrowAsync(requesterUserId, cancellationToken);
        var target = await GetUserOrThrowAsync(targetUserId, cancellationToken);

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
            throw new ArgumentException("Class id cannot be empty.", nameof(classId));
        }

        var creator = await GetUserOrThrowAsync(creatorUserId, cancellationToken);
        if (creator.Role is not UserRole.Teacher and not UserRole.Principal)
        {
            throw new InvalidOperationException("Only teacher or principal can create class chat.");
        }

        if (creator.Role == UserRole.Teacher)
        {
            var teacherClassIds = await GetTeacherClassIdsByUserIdAsync(creatorUserId, cancellationToken);
            if (!teacherClassIds.Contains(classId))
            {
                throw new InvalidOperationException("Teacher is not related to this class.");
            }
        }

        var schoolClass = await classRepository.GetByIdAsync(classId, cancellationToken)
            ?? throw new InvalidOperationException("Class was not found.");

        var chatName = $"{ClassChatPrefix}{classId}";
        var chats = await chatRepository.GetAllAsync(cancellationToken);
        var existingClassChat = chats.FirstOrDefault(chat => chat.Name == chatName);

        if (existingClassChat is not null)
        {
            return existingClassChat;
        }

        var participantIds = schoolClass.Students
            .Select(student => student.UserId)
            .Append(creatorUserId)
            .Distinct()
            .ToList();

        var users = await userRepository.GetAllAsync(cancellationToken);
        var participants = users
            .Where(user => participantIds.Contains(user.Id))
            .ToList();

        var chat = new Chat
        {
            Id = Guid.NewGuid(),
            Name = chatName,
            Users = participants
        };

        await chatRepository.CreateAsync(chat, cancellationToken);
        return chat;
    }

    public async Task<Chat> GetOrCreateTeachersGroupChatAsync(Guid creatorUserId, CancellationToken cancellationToken = default)
    {
        var creator = await GetUserOrThrowAsync(creatorUserId, cancellationToken);
        if (creator.Role is not UserRole.Teacher and not UserRole.Principal)
        {
            throw new InvalidOperationException("Only teacher or principal can create teachers group chat.");
        }

        var chats = await chatRepository.GetAllAsync(cancellationToken);
        var existingTeachersGroup = chats.FirstOrDefault(chat => chat.Name == TeachersGroupChatName);

        if (existingTeachersGroup is not null)
        {
            return existingTeachersGroup;
        }

        var users = await userRepository.GetAllAsync(cancellationToken);
        var participants = users
            .Where(user => user.Role is UserRole.Teacher or UserRole.Principal)
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
            throw new ArgumentException("Message text cannot be empty.", nameof(text));
        }

        if (text.Length > 4000)
        {
            throw new ArgumentException("Message text cannot exceed 4000 characters.", nameof(text));
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
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        return await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");
    }

    private async Task EnsureUserCanAccessChatAsync(Guid userId, Guid chatId, CancellationToken cancellationToken)
    {
        var requester = await GetUserOrThrowAsync(userId, cancellationToken);
        var chat = await chatRepository.GetByIdAsync(chatId, cancellationToken)
            ?? throw new InvalidOperationException("Chat was not found.");

        if (chat.Users.All(user => user.Id != userId))
        {
            throw new UnauthorizedAccessException("User is not part of this chat.");
        }

        if (IsCustomGroupChat(chat))
        {
            throw new UnauthorizedAccessException("Custom group chats are disabled.");
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

    private static bool CanUserSeeChat(User requester, Chat chat)
    {
        return requester.Role != UserRole.Student || chat.Users.All(user => user.Role != UserRole.Principal);
    }

    private async Task<HashSet<Guid>> GetAllowedDirectContactIdsAsync(Guid requesterUserId, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return users
            .Where(user => user.Id != requesterUserId)
            .Where(user => !user.IsDeleted)
            .Where(user => user.Role is UserRole.Student or UserRole.Teacher)
            .Select(user => user.Id)
            .ToHashSet();
    }

    private static HashSet<Guid> GetAllowedForStudent(
        Guid requesterUserId,
        IReadOnlyList<Student> students,
        IReadOnlyList<ClassSubject> classSubjects,
        IReadOnlyList<Class> classes,
        IReadOnlyDictionary<Guid, Guid> teacherUserByTeacherId)
    {
        var student = students.FirstOrDefault(currentStudent => currentStudent.UserId == requesterUserId)
            ?? throw new InvalidOperationException("Student profile was not found.");

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
        IReadOnlyDictionary<Guid, Guid> teacherUserByTeacherId,
        IReadOnlySet<Guid> principalIds)
    {
        var parent = parents.FirstOrDefault(currentParent => currentParent.UserId == requesterUserId)
            ?? throw new InvalidOperationException("Parent profile was not found.");

        var childClassIds = parent.Students
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

        var allowed = teacherIds
            .Where(teacherUserByTeacherId.ContainsKey)
            .Select(teacherId => teacherUserByTeacherId[teacherId])
            .ToHashSet();

        foreach (var principalId in principalIds)
        {
            allowed.Add(principalId);
        }

        return allowed;
    }

    private static HashSet<Guid> GetAllowedForTeacher(
        Guid requesterUserId,
        IReadOnlyList<Teacher> teachers,
        IReadOnlyList<Student> students,
        IReadOnlyList<Parent> parents,
        IReadOnlyList<Class> classes,
        IReadOnlyList<ClassSubject> classSubjects,
        IReadOnlySet<Guid> principalIds)
    {
        var teacher = teachers.FirstOrDefault(currentTeacher => currentTeacher.UserId == requesterUserId)
            ?? throw new InvalidOperationException("Teacher profile was not found.");

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
            .Select(student => student.UserId)
            .ToHashSet();

        var parentUserIds = parents
            .Where(parent => parent.Students.Any(student => classIds.Contains(student.ClassId)))
            .Select(parent => parent.UserId)
            .ToHashSet();

        foreach (var parentUserId in parentUserIds)
        {
            studentUserIds.Add(parentUserId);
        }

        foreach (var principalId in principalIds)
        {
            studentUserIds.Add(principalId);
        }

        return studentUserIds;
    }

    private static HashSet<Guid> GetAllowedForPrincipal(IReadOnlyList<User> users)
    {
        return users
            .Where(user => user.Role is UserRole.Teacher or UserRole.Parent)
            .Select(user => user.Id)
            .ToHashSet();
    }

    private async Task<HashSet<Guid>> GetTeacherClassIdsByUserIdAsync(Guid teacherUserId, CancellationToken cancellationToken)
    {
        var teachers = await teacherRepository.GetAllAsync(cancellationToken);
        var classes = await classRepository.GetAllAsync(cancellationToken);
        var classSubjects = await classSubjectRepository.GetAllAsync(cancellationToken);

        var teacher = teachers.FirstOrDefault(currentTeacher => currentTeacher.UserId == teacherUserId)
            ?? throw new InvalidOperationException("Teacher profile was not found.");

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
