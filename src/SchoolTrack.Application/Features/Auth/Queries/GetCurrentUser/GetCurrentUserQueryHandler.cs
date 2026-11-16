using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Shared.DTOs.Auth;

namespace SchoolTrack.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetCurrentUserQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> HandleAsync(GetCurrentUserQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.School)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{query.UserId}' was not found.");
        }

        return new UserDto(
            Id: user.Id,
            SchoolId: user.SchoolId,
            Email: user.Email,
            FullName: user.FullName,
            PhoneNumber: user.PhoneNumber,
            Role: user.Role,
            IsActive: user.IsActive,
            SchoolName: user.School?.Name);
    }
}

