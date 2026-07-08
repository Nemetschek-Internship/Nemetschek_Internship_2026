using System;
using System.Threading;
using System.Threading.Tasks;
using Entities.Enums;
using Entities.Models;
using Moq;
using Services.Interfaces;
using Services.Repositories;
using Services.Services.Notifications;
using Xunit;

namespace Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task SendEmailNotificationAsync_WhenTeacherExists_SendsNotificationEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var teacherUser = new User
        {
            Id = userId,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@example.com",
            Role = UserRole.Teacher
        };

        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(teacherUser);

        var notificationRepositoryMock = new Mock<INotificationRepository>();
        var studentRepositoryMock = new Mock<IStudentRepository>();
        var emailServiceMock = new Mock<IEmailService>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<NotificationService>>();

        var notificationService = new NotificationService(
            notificationRepositoryMock.Object,
            userRepositoryMock.Object,
            studentRepositoryMock.Object,
            emailServiceMock.Object,
            loggerMock.Object);

        var title = "Class update";
        var message = "There is a new assignment for your class.";

        // Act
        await notificationService.SendEmailNotificationAsync(userId, title, message);

        // Assert
        emailServiceMock.Verify(
            x => x.SendNotificationEmailAsync(
                teacherUser.Email,
                "Jane Doe",
                title,
                message,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailNotificationAsync_WhenTeacherNotFound_DoesNotSendEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userRepositoryMock = new Mock<IUserRepository>();
        userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var notificationRepositoryMock = new Mock<INotificationRepository>();
        var studentRepositoryMock = new Mock<IStudentRepository>();
        var emailServiceMock = new Mock<IEmailService>();
        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<NotificationService>>();

        var notificationService = new NotificationService(
            notificationRepositoryMock.Object,
            userRepositoryMock.Object,
            studentRepositoryMock.Object,
            emailServiceMock.Object,
            loggerMock.Object);

        // Act
        await notificationService.SendEmailNotificationAsync(userId, "Test", "Test message");

        // Assert
        emailServiceMock.Verify(
            x => x.SendNotificationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
