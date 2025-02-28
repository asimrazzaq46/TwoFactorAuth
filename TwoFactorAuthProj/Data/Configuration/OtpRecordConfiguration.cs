using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TwoFactorAuthProj.Entities;

namespace TwoFactorAuthProj.Data.Configuration
{
    public class OtpRecordConfiguration : IEntityTypeConfiguration<OtpRecord>
    {
        public void Configure(EntityTypeBuilder<OtpRecord> builder)
        {
            builder.Property(o => o.Expiry)
                .HasColumnType("DATETIME2(3)");
        }
    }
}
