using FootballPredictionGame.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FootballPredictionGame.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Fixture> Fixtures => Set<Fixture>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<ResultProcessingLog> ResultProcessingLogs => Set<ResultProcessingLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.MobileNo).HasMaxLength(30);
            entity.Property(x => x.ProfilePhotoPath).HasMaxLength(300);
            entity.Property(x => x.MustChangePassword).HasDefaultValue(false);
        });

        builder.Entity<Fixture>(entity =>
        {
            entity.Property(x => x.TeamOneName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TeamTwoName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.TeamOneFlagPath).HasMaxLength(300);
            entity.Property(x => x.TeamTwoFlagPath).HasMaxLength(300);
            entity.Property(x => x.Stage).HasDefaultValue(FixtureStage.GroupA);
        });

        builder.Entity<Prediction>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.FixtureId }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany(x => x.Predictions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Fixture)
                .WithMany(x => x.Predictions)
                .HasForeignKey(x => x.FixtureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ResultProcessingLog>(entity =>
        {
            entity.HasOne(x => x.Fixture)
                .WithMany()
                .HasForeignKey(x => x.FixtureId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
