using AutoMapper;
using Microsoft.Extensions.Logging;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Auth;
using TourShuttle.Application.DTOs.Users;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.Services;

public class AuthService : IAuthService
{
    // One message for every login failure, so a caller cannot learn which emails are registered.
    private const string InvalidCredentials = "The email or password is incorrect.";
    private const string EmailTaken = "An account with this email already exists.";

    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokens;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository users, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher,
        IJwtTokenService tokens, IMapper mapper, ILogger<AuthService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        if (await _users.EmailExistsAsync(email, ct))
        {
            return Result<AuthResponse>.Conflict(EmailTaken);
        }

        // Self-registration always creates a customer; admins and drivers are created by the system.
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Customer
        };

        _users.Add(user);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException)
        {
            // Two registrations with the same email passed the check above at the same moment;
            // the unique index on Email let only one of them in.
            return Result<AuthResponse>.Conflict(EmailTaken);
        }

        _logger.LogInformation("User {UserId} registered", user.Id);
        return Result<AuthResponse>.Success(BuildResponse(user));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(NormalizeEmail(request.Email), ct);

        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            _logger.LogInformation("Failed login attempt");
            return Result<AuthResponse>.Unauthorized(InvalidCredentials);
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return Result<AuthResponse>.Success(BuildResponse(user));
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expiresAt) = _tokens.CreateToken(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = _mapper.Map<UserResponse>(user)
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
