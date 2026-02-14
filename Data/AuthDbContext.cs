using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SpotFinder.Web.Data
{
    public class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
    }
}
