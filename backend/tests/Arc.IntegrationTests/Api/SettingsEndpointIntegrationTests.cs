using System.Net;
using Arc.Domain.Models;
using System.Net.Http.Json;
using Arc.Api.DTOs.Settings;
using Microsoft.AspNetCore.Mvc.Testing;


namespace Arc.IntegrationTests.Api
{
    /// <summary>
    /// Integration tests for Settings API endpoints.
    /// Tests full HTTP workflow with authentication.
    /// </summary>
    public sealed class SettingsEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SettingsEndpointIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        private async Task<HttpClient> GetAuthenticatedClientAsync()
        {
            var client = _factory.CreateClient();
            var registerRequest = new Arc.Api.DTOs.Auth.RegisterRequestDto
            {
                Username = $"testuser_{Guid.NewGuid()}",
                Email = $"test_{Guid.NewGuid()}@example.com",
                Password = "Password123!",
                Role = "User"
            };
            var response = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
            response.EnsureSuccessStatusCode();
            // Copy auth cookie to client
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                var authCookie = cookies.FirstOrDefault(c => c.Contains("auth_token"));
                if (authCookie != null)
                {
                    client.DefaultRequestHeaders.Add("Cookie", authCookie.Split(';')[0]);
                }
            }
            return client;
        }

        [Fact]
        public async Task GetPreferences_WithAuthentication_ReturnsDefaultPreferences()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            // Act
            var response = await client.GetAsync("/api/settings/preferences");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var preferences = await response.Content.ReadFromJsonAsync<UserPreferencesResponseDto>();
            Assert.NotNull(preferences);
            Assert.Equal("system", preferences.Theme);
            Assert.True(preferences.Notifications.Email);
            Assert.False(preferences.Notifications.Push);
            Assert.True(preferences.Notifications.ExecutionComplete);
            Assert.True(preferences.Notifications.ExecutionFailed);
            Assert.Equal("en", preferences.Language);
            Assert.Equal("UTC", preferences.Timezone);
        }

        [Fact]
        public async Task UpdatePreferences_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            var request = new UpdateUserPreferencesRequestDto
            {
                Theme = "dark",
                Notifications = new NotificationPreferencesDto
                {
                    Email = false,
                    Push = true,
                    ExecutionComplete = false,
                    ExecutionFailed = true
                },
                Language = "es",
                Timezone = "America/New_York"
            };

            // Act
            var response = await client.PutAsJsonAsync("/api/settings/preferences", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify preferences were updated
            var getResponse = await client.GetAsync("/api/settings/preferences");
            var preferences = await getResponse.Content.ReadFromJsonAsync<UserPreferencesResponseDto>();
            Assert.NotNull(preferences);
            Assert.Equal("dark", preferences.Theme);
            Assert.False(preferences.Notifications.Email);
            Assert.True(preferences.Notifications.Push);
            Assert.False(preferences.Notifications.ExecutionComplete);
            Assert.True(preferences.Notifications.ExecutionFailed);
            Assert.Equal("es", preferences.Language);
            Assert.Equal("America/New_York", preferences.Timezone);
        }

        [Fact]
        public async Task UpdatePreferences_WithInvalidTheme_ReturnsBadRequest()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            var request = new UpdateUserPreferencesRequestDto
            {
                Theme = "invalid",
                Notifications = new NotificationPreferencesDto
                {
                    Email = true,
                    Push = false,
                    ExecutionComplete = true,
                    ExecutionFailed = true
                },
                Language = "en",
                Timezone = "UTC"
            };

            // Act
            var response = await client.PutAsJsonAsync("/api/settings/preferences", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePreferences_WithEmptyLanguage_ReturnsBadRequest()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            var request = new UpdateUserPreferencesRequestDto
            {
                Theme = "light",
                Notifications = new NotificationPreferencesDto
                {
                    Email = true,
                    Push = false,
                    ExecutionComplete = true,
                    ExecutionFailed = true
                },
                Language = "",
                Timezone = "UTC"
            };

            // Act
            var response = await client.PutAsJsonAsync("/api/settings/preferences", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePreferences_MultipleTimes_OverwritesPreviousValues()
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            var request1 = new UpdateUserPreferencesRequestDto
            {
                Theme = "light",
                Notifications = new NotificationPreferencesDto
                {
                    Email = true,
                    Push = false,
                    ExecutionComplete = true,
                    ExecutionFailed = true
                },
                Language = "en",
                Timezone = "UTC"
            };

            var request2 = new UpdateUserPreferencesRequestDto
            {
                Theme = "dark",
                Notifications = new NotificationPreferencesDto
                {
                    Email = false,
                    Push = true,
                    ExecutionComplete = false,
                    ExecutionFailed = false
                },
                Language = "fr",
                Timezone = "Europe/Paris"
            };

            // Act
            await client.PutAsJsonAsync("/api/settings/preferences", request1);
            await client.PutAsJsonAsync("/api/settings/preferences", request2);

            // Assert
            var getResponse = await client.GetAsync("/api/settings/preferences");
            var preferences = await getResponse.Content.ReadFromJsonAsync<UserPreferencesResponseDto>();
            Assert.NotNull(preferences);
            Assert.Equal("dark", preferences.Theme);
            Assert.False(preferences.Notifications.Email);
            Assert.True(preferences.Notifications.Push);
            Assert.Equal("fr", preferences.Language);
            Assert.Equal("Europe/Paris", preferences.Timezone);
        }

        [Theory]
        [InlineData("light")]
        [InlineData("dark")]
        [InlineData("system")]
        [InlineData("LIGHT")]
        [InlineData("Dark")]
        public async Task UpdatePreferences_WithValidThemeVariations_Succeeds(string theme)
        {
            // Arrange
            var client = await GetAuthenticatedClientAsync();

            var request = new UpdateUserPreferencesRequestDto
            {
                Theme = theme,
                Notifications = new NotificationPreferencesDto
                {
                    Email = true,
                    Push = false,
                    ExecutionComplete = true,
                    ExecutionFailed = true
                },
                Language = "en",
                Timezone = "UTC"
            };

            // Act
            var response = await client.PutAsJsonAsync("/api/settings/preferences", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var getResponse = await client.GetAsync("/api/settings/preferences");
            var preferences = await getResponse.Content.ReadFromJsonAsync<UserPreferencesResponseDto>();
            Assert.NotNull(preferences);
            Assert.Equal(theme.ToLowerInvariant(), preferences.Theme);
        }
    }
}