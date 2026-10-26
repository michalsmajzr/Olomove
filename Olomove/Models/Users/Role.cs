using Microsoft.AspNetCore.Identity;

namespace Olomove.Models.Users;

public class Role : IdentityRole<Guid>
{
    public Role() : base() { }

    public Role(string roleName) : base(roleName) { }
}