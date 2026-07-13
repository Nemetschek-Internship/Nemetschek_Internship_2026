using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class NemeBookDbContext : DbContext
{
    public NemeBookDbContext(DbContextOptions<NemeBookDbContext> options)
        : base(options)
    {
    }

    public DbSet<Absence> Absences => Set<Absence>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ClassScheduleEntry> ClassScheduleEntries => Set<ClassScheduleEntry>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<ClassSubject> ClassSubjects => Set<ClassSubject>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Parent> Parents => Set<Parent>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RegistrationInvitation> RegistrationInvitations => Set<RegistrationInvitation>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureClasses(modelBuilder);
        ConfigureClassSubjects(modelBuilder);
        ConfigureClassScheduleEntries(modelBuilder);
        ConfigureGrades(modelBuilder);
        ConfigureAbsences(modelBuilder);
        ConfigureFeedbacks(modelBuilder);
        ConfigureEvents(modelBuilder);
        ConfigureChats(modelBuilder);
        ConfigureNotifications(modelBuilder);
        ConfigurePasswordResetTokens(modelBuilder);
        ConfigureRegistrationInvitations(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasQueryFilter(user => !user.IsDeleted);

            entity.HasIndex(user => user.Email).IsUnique();

            entity.Property(user => user.FirstName).HasMaxLength(100);
            entity.Property(user => user.MiddleName).HasMaxLength(100);
            entity.Property(user => user.LastName).HasMaxLength(100);
            entity.Property(user => user.Email).HasMaxLength(256);
            entity.Property(user => user.Password).HasMaxLength(512);
            entity.Property(user => user.PhoneNumber).HasMaxLength(30);
            entity.Property(user => user.IsActive).HasDefaultValue(false);
        });

        modelBuilder.Entity<Student>()
            .HasQueryFilter(student => !student.User.IsDeleted);

        modelBuilder.Entity<Student>()
            .HasOne(student => student.User)
            .WithOne(user => user.Student)
            .HasForeignKey<Student>(student => student.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Parent>()
            .HasQueryFilter(parent => !parent.User.IsDeleted);

        modelBuilder.Entity<Parent>()
            .HasOne(parent => parent.User)
            .WithOne(user => user.Parent)
            .HasForeignKey<Parent>(parent => parent.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Teacher>()
            .HasQueryFilter(teacher => !teacher.User.IsDeleted && teacher.User.IsActive);

        modelBuilder.Entity<Teacher>()
            .HasOne(teacher => teacher.User)
            .WithOne(user => user.Teacher)
            .HasForeignKey<Teacher>(teacher => teacher.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureClasses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasIndex(schoolClass => new { schoolClass.GradeNumber, schoolClass.Letter })
                .IsUnique();

            entity.HasOne(schoolClass => schoolClass.MainTeacher)
                .WithOne(teacher => teacher.MainClass)
                .HasForeignKey<Class>(schoolClass => schoolClass.MainTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(schoolClass => schoolClass.Students)
                .WithOne(student => student.Class)
                .HasForeignKey(student => student.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Parent>()
            .HasMany(parent => parent.Students)
            .WithMany(student => student.Parents)
            .UsingEntity(join => join.ToTable("ParentStudents"));
    }

    private static void ConfigureClassSubjects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasIndex(subject => subject.Name).IsUnique();
            entity.Property(subject => subject.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<TeacherSubject>(entity =>
        {
            entity.HasQueryFilter(teacherSubject => !teacherSubject.Teacher.User.IsDeleted && teacherSubject.Teacher.User.IsActive);

            entity.HasIndex(teacherSubject => new { teacherSubject.TeacherId, teacherSubject.SubjectId })
                .IsUnique();

            entity.HasOne(teacherSubject => teacherSubject.Teacher)
                .WithMany(teacher => teacher.TeacherSubjects)
                .HasForeignKey(teacherSubject => teacherSubject.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(teacherSubject => teacherSubject.Subject)
                .WithMany(subject => subject.TeacherSubjects)
                .HasForeignKey(teacherSubject => teacherSubject.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClassSubject>(entity =>
        {
            entity.HasIndex(classSubject => new { classSubject.ClassId, classSubject.SubjectId })
                .IsUnique();

            entity.HasOne(classSubject => classSubject.Class)
                .WithMany(schoolClass => schoolClass.ClassSubjects)
                .HasForeignKey(classSubject => classSubject.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(classSubject => classSubject.Subject)
                .WithMany(subject => subject.ClassSubjects)
                .HasForeignKey(classSubject => classSubject.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(classSubject => classSubject.Teacher)
                .WithMany(teacher => teacher.ClassSubjects)
                .HasForeignKey(classSubject => classSubject.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureClassScheduleEntries(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClassScheduleEntry>(entity =>
        {
            entity.HasIndex(scheduleEntry => new
                {
                    scheduleEntry.ClassId,
                    scheduleEntry.DayOfWeek,
                    scheduleEntry.PeriodNumber
                })
                .IsUnique();

            entity.HasOne(scheduleEntry => scheduleEntry.Class)
                .WithMany(schoolClass => schoolClass.ScheduleEntries)
                .HasForeignKey(scheduleEntry => scheduleEntry.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(scheduleEntry => scheduleEntry.ClassSubject)
                .WithMany(classSubject => classSubject.ScheduleEntries)
                .HasForeignKey(scheduleEntry => scheduleEntry.ClassSubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureGrades(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Grade>(entity =>
        {
            entity.Property(grade => grade.Value).HasPrecision(3, 2);
            entity.Property(grade => grade.Note).HasMaxLength(1000);
            entity.Property(grade => grade.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(grade => grade.Student)
                .WithMany(student => student.Grades)
                .HasForeignKey(grade => grade.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(grade => grade.ClassSubject)
                .WithMany(classSubject => classSubject.Grades)
                .HasForeignKey(grade => grade.ClassSubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAbsences(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Absence>(entity =>
        {
            entity.Property(absence => absence.ExcuseNote).HasMaxLength(1000);
            entity.Property(absence => absence.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(absence => absence.Date).HasColumnType("date");

            entity.HasOne(absence => absence.Student)
                .WithMany(student => student.Absences)
                .HasForeignKey(absence => absence.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(absence => absence.ClassSubject)
                .WithMany(classSubject => classSubject.Absences)
                .HasForeignKey(absence => absence.ClassSubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(absence => absence.ClassScheduleEntry)
                .WithMany(scheduleEntry => scheduleEntry.Absences)
                .HasForeignKey(absence => absence.ClassScheduleEntryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureFeedbacks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.Property(feedback => feedback.Description).HasMaxLength(1000);
            entity.Property(feedback => feedback.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(feedback => feedback.Date)
                .HasColumnType("date")
                .HasDefaultValueSql("CONVERT(date, GETUTCDATE())");

            entity.HasOne(feedback => feedback.Student)
                .WithMany(student => student.Feedbacks)
                .HasForeignKey(feedback => feedback.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(feedback => feedback.ClassSubject)
                .WithMany(classSubject => classSubject.Feedbacks)
                .HasForeignKey(feedback => feedback.ClassSubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(feedback => feedback.ClassScheduleEntry)
                .WithMany(scheduleEntry => scheduleEntry.Feedbacks)
                .HasForeignKey(feedback => feedback.ClassScheduleEntryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(schoolEvent => schoolEvent.Title).HasMaxLength(200);
            entity.Property(schoolEvent => schoolEvent.Description).HasMaxLength(4000);

            entity.HasOne(schoolEvent => schoolEvent.CreatedByUser)
                .WithMany()
                .HasForeignKey(schoolEvent => schoolEvent.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(schoolEvent => schoolEvent.ClassSubject)
                .WithMany(classSubject => classSubject.Events)
                .HasForeignKey(schoolEvent => schoolEvent.ClassSubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(schoolEvent => schoolEvent.Classes)
                .WithMany(schoolClass => schoolClass.Events)
                .UsingEntity(join => join.ToTable("ClassEvents"));
        });
    }

    private static void ConfigureChats(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.Property(chat => chat.Name).HasMaxLength(200);

            entity.HasMany(chat => chat.Users)
                .WithMany(user => user.Chats)
                .UsingEntity(join => join.ToTable("ChatUsers"));
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasQueryFilter(message => !message.IsDeleted);

            entity.Property(message => message.Text).HasMaxLength(4000);

            entity.HasOne(message => message.Chat)
                .WithMany(chat => chat.Messages)
                .HasForeignKey(message => message.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(message => message.Sender)
                .WithMany()
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(notification => notification.Text).HasMaxLength(500);

            entity.HasOne(notification => notification.User)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(notification => notification.Event)
                .WithMany()
                .HasForeignKey(notification => notification.EventId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(notification => notification.Grade)
                .WithMany()
                .HasForeignKey(notification => notification.GradeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(notification => notification.Absence)
                .WithMany()
                .HasForeignKey(notification => notification.AbsenceId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(notification => notification.Feedback)
                .WithMany()
                .HasForeignKey(notification => notification.FeedbackId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(notification => notification.Chat)
                .WithMany()
                .HasForeignKey(notification => notification.ChatId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(notification => notification.Message)
                .WithMany()
                .HasForeignKey(notification => notification.MessageId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigurePasswordResetTokens(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(token => token.Token).IsUnique();
            entity.HasIndex(token => token.UserId).IsUnique();
            entity.Property(token => token.Token).HasMaxLength(256);

            entity.HasOne(token => token.User)
                .WithOne(user => user.PasswordResetToken)
                .HasForeignKey<PasswordResetToken>(token => token.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRegistrationInvitations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistrationInvitation>(entity =>
        {
            entity.HasIndex(invitation => invitation.TokenHash).IsUnique();
            entity.HasIndex(invitation => invitation.Email);

            entity.Property(invitation => invitation.Email).HasMaxLength(256);
            entity.Property(invitation => invitation.TokenHash).HasMaxLength(512);

            entity.HasOne(invitation => invitation.User)
                .WithMany()
                .HasForeignKey(invitation => invitation.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(invitation => invitation.Students)
                .WithMany()
                .UsingEntity<Dictionary<string, object>>(
                    "RegistrationInvitationStudents",
                    right => right
                        .HasOne<Student>()
                        .WithMany()
                        .HasForeignKey("StudentId")
                        .OnDelete(DeleteBehavior.Restrict),
                    left => left
                        .HasOne<RegistrationInvitation>()
                        .WithMany()
                        .HasForeignKey("RegistrationInvitationId")
                        .OnDelete(DeleteBehavior.Cascade));
        });
    }
}
