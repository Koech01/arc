using System.Net;
using Arc.Domain.Models;
using Arc.Api.DTOs.Auth;
using System.Net.Http.Json;
using Arc.Api.DTOs.Notifications;
namespace Arc.IntegrationTests.Api;
using Arc.Application.Notifications;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;


/// <summary>
/// Integration tests for Notifications API endpoints.
/// Tests full HTTP workflow with authentication and user isolation.
/// </summary>
public sealed class NotificationEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NotificationEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetNotifications_WithAuthentication_ReturnsUserNotifications()
    {
        var client = _factory.CreateClient();
        var userId = await AuthenticateAsync(client);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        await repository.CreateAsync(Notification.Create(userId, "Test Title", "Test Message", NotificationType.Info));

        var response = await client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponseDto>>();
        Assert.NotNull(notifications);
        Assert.Single(notifications);
        Assert.Equal("Test Title", notifications[0].Title);
        Assert.False(notifications[0].Read);
    }

    [Fact]
    public async Task MarkAsRead_WithValidId_MarksNotificationAsRead()
    {
        var client = _factory.CreateClient();
        var userId = await AuthenticateAsync(client);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var notification = Notification.Create(userId, "Test Title", "Test Message", NotificationType.Info);
        await repository.CreateAsync(notification);

        var response = await client.PutAsync($"/api/notifications/{notification.Id.Value}/read", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedNotification = await repository.GetByIdAsync(notification.Id);
        Assert.NotNull(updatedNotification);
        Assert.True(updatedNotification.IsRead);
    }

    [Fact]
    public async Task MarkAsRead_WithInvalidId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsync(client);

        var response = await client.PutAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAsRead_WithDifferentUser_ReturnsForbidden()
    {
        var ownerClient = _factory.CreateClient();
        var ownerId = await AuthenticateAsync(ownerClient);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var notification = Notification.Create(ownerId, "Test Title", "Test Message", NotificationType.Info);
        await repository.CreateAsync(notification);

        var otherClient = _factory.CreateClient();
        await AuthenticateAsync(otherClient);

        var response = await otherClient.PutAsync($"/api/notifications/{notification.Id.Value}/read", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllAsRead_WithMultipleNotifications_MarksAllAsRead()
    {
        var client = _factory.CreateClient();
        var userId = await AuthenticateAsync(client);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        await repository.CreateAsync(Notification.Create(userId, "Title 1", "Message 1", NotificationType.Info));
        await repository.CreateAsync(Notification.Create(userId, "Title 2", "Message 2", NotificationType.Success));

        var response = await client.PutAsync("/api/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var notifications = await repository.ListByUserAsync(userId);
        Assert.All(notifications, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task GetNotifications_ReturnsNotificationsInDescendingOrder()
    {
        var client = _factory.CreateClient();
        var userId = await AuthenticateAsync(client);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        var notification1 = new Notification(
            NotificationId.Create(),
            userId,
            "First",
            "Message 1",
            NotificationType.Info,
            false,
            DateTime.UtcNow.AddMinutes(-10));

        var notification2 = new Notification(
            NotificationId.Create(),
            userId,
            "Second",
            "Message 2",
            NotificationType.Info,
            false,
            DateTime.UtcNow.AddMinutes(-5));

        await repository.CreateAsync(notification1);
        await repository.CreateAsync(notification2);

        var response = await client.GetAsync("/api/notifications");

        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponseDto>>();
        Assert.NotNull(notifications);
        Assert.Equal(2, notifications.Count);
        Assert.Equal("Second", notifications[0].Title);
        Assert.Equal("First", notifications[1].Title);
    }

    private static async Task<UserId> AuthenticateAsync(HttpClient client)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"notify-{Guid.NewGuid():N}",
            email = $"notify-{Guid.NewGuid():N}@example.com",
            password = "Password123!"
        });

        registerResponse.EnsureSuccessStatusCode();
        var authCookie = registerResponse.Headers
            .GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("auth_token="));

        if (string.IsNullOrWhiteSpace(authCookie))
        {
            throw new InvalidOperationException("Authentication cookie was not issued.");
        }

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", authCookie.Split(';')[0]);

        var meResponse = await client.GetFromJsonAsync<UserDto>("/api/auth/me");
        return UserId.From(Guid.Parse(meResponse!.Id));
    }
}