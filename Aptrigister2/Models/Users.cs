using Microsoft.AspNetCore.Identity;

namespace Aptrigister2.Models
{
    public class Users:IdentityUser
    {
        public string FullName { get; set; }
    }
}
