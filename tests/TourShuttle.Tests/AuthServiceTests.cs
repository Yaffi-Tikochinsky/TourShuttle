using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Auth;
using TourShuttle.Application.Interfaces;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _tokens = new();
    private User? _added;

    public AuthServiceTests()
    {
        _users.Setup(u => u.Add(It.IsAny<User>())).Callback<User>(u =>
        {
            _added = u;
            u.Id = 42; // what the database would assign on save
        });
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed-password");
        _tokens.Setup(t => t.CreateToken(It.IsAny<User>())).Returns(("signed-token", TestData.Now.AddHours(1)));
    }

    private AuthService CreateService() => new(_users.Object, _unitOfWork.Object, _hasher.Object, _tokens.Object,
        TestData.Mapper(), NullLogger<AuthService>.Instance);

    private static RegisterRequest Registration(string email = "  Yaffi@Example.COM ") => new()
    {
        FullName = "Yaffi Test",
        Email = email,
        Phone = "050-1234567",
        Password = "Secret123"
    };

    [Fact]
    public async Task Registration_creates_a_customer_with_a_normalized_email_and_a_hashed_password()
    {
        var result = await CreateService().RegisterAsync(Registration(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Customer, _added!.Role);
        Assert.Equal("yaffi@example.com", _added.Email);
        Assert.Equal("hashed-password", _added.PasswordHash);
        _hasher.Verify(h => h.Hash("Secret123"), Times.Once);
        Assert.Equal("signed-token", result.Value!.Token);
        Assert.Equal("Customer", result.Value.User.Role);
    }

    [Fact]
    public async Task Registering_an_existing_email_is_a_conflict()
    {
        _users.Setup(u => u.EmailExistsAsync("yaffi@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().RegisterAsync(Registration(), default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _users.Verify(u => u.Add(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Two_simultaneous_registrations_with_one_email_end_in_a_conflict()
    {
        // Both passed the "email exists?" check; the unique index on Email refuses the second insert.
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintException(new Exception("duplicate key")));

        var result = await CreateService().RegisterAsync(Registration(), default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task Unknown_email_and_wrong_password_get_the_same_answer()
    {
        var user = TestData.User(3, UserRole.Customer);
        _users.Setup(u => u.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(user.PasswordHash, "wrong")).Returns(false);
        var service = CreateService();

        var unknownEmail = await service.LoginAsync(new LoginRequest { Email = "nobody@test.demo", Password = "wrong" }, default);
        var wrongPassword = await service.LoginAsync(new LoginRequest { Email = user.Email, Password = "wrong" }, default);

        // Identical status and message: a caller cannot learn which emails are registered.
        Assert.Equal(ResultStatus.Unauthorized, unknownEmail.Status);
        Assert.Equal(ResultStatus.Unauthorized, wrongPassword.Status);
        Assert.Equal(unknownEmail.Error, wrongPassword.Error);
        _tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Correct_password_returns_a_token()
    {
        var user = TestData.User(3, UserRole.Customer);
        _users.Setup(u => u.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(user.PasswordHash, "Customer123!")).Returns(true);

        var result = await CreateService().LoginAsync(
            new LoginRequest { Email = user.Email.ToUpperInvariant(), Password = "Customer123!" }, default);

        Assert.True(result.IsSuccess);
        Assert.Equal("signed-token", result.Value!.Token);
        Assert.Equal(3, result.Value.User.Id);
    }
}
