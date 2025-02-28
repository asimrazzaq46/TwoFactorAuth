using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TwoFactorAuthProj.Data.Configuration;
using TwoFactorAuthProj.Entities;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Data;

public class DataContext(DbContextOptions opt) :IdentityDbContext<AppUser>(opt)
{
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<OtpRecord> OtpRecords { get; set; }
    public DbSet<OtpFailedAttempt> OtpFailedAttempts { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

}
