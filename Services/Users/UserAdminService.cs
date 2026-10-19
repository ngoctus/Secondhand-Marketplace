using Microsoft.AspNetCore.Identity;
using Web2Hand.Data;
using Web2Hand.Models.Entities;

namespace Web2Hand.Services.Users;

public class UserAdminService : IUserAdminService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public UserAdminService(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }
}
