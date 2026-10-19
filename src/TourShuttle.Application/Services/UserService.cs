using AutoMapper;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Users;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;

    public UserService(IUserRepository users, IMapper mapper)
    {
        _users = users;
        _mapper = mapper;
    }

    public async Task<Result<UserResponse>> GetByIdAsync(int id, int requesterId, UserRole requesterRole, CancellationToken ct)
    {
        // Resource-based authorization: the role alone is not enough, the requester must own the resource.
        if (id != requesterId && requesterRole != UserRole.Admin)
        {
            return Result<UserResponse>.Forbidden("You can only view your own profile.");
        }

        var user = await _users.GetByIdAsync(id, ct);
        return user is null
            ? Result<UserResponse>.NotFound($"User {id} was not found.")
            : Result<UserResponse>.Success(_mapper.Map<UserResponse>(user));
    }

    public async Task<IReadOnlyList<UserResponse>> ListAsync(UserRole? role, CancellationToken ct)
    {
        var users = await _users.ListAsync(role, ct);
        return _mapper.Map<IReadOnlyList<UserResponse>>(users);
    }
}
