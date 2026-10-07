using Microsoft.AspNetCore.Identity;

namespace Olomove.Models;

public class Role : IdentityRole<Guid>
{
    public Role() : base() { }

    public Role(string roleName) : base(roleName) { }
}