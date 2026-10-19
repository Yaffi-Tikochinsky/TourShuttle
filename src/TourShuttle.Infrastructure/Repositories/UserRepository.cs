using Microsoft.EntityFrameworkCore;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;
using TourShuttle.Infrastructure.Persistence;

namespace TourShuttle.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        _db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<IReadOnlyList<User>> ListAsync(UserRole? role, CancellationToken ct)
    {
        var query = _db.Users.AsNoTracking();

        if (role is not null)
        {
            query = query.Where(u => u.Role == role);
        }

        return await query.OrderBy(u => u.FullName).ToListAsync(ct);
    }

    public void Add(User user) => _db.Users.Add(user);
}
