using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwoFactorAuthProj.Entities;
using TwoFactorAuthProj.Enums;

namespace TwoFactorAuthProj.Data.Configuration;

public class AppuserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(x => x.AuthType).HasConversion(
            o => o.ToString(),
            o => (AuthType)Enum.Parse(typeof(AuthType), o!)
            );
    }
}
