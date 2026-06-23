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
    public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
    public DbSet<AutomationSettings> AutomationSettings { get; set; }
    public DbSet<AutomationLog> AutomationLogs { get; set; }
    public DbSet<AutomationSuggestion> AutomationSuggestions { get; set; }
    public DbSet<UserLoginHistory> UserLoginHistories { get; set; }
    public DbSet<UserActiveSession> UserActiveSessions { get; set; }
    public DbSet<PredictionReminderLog> PredictionReminderLogs { get; set; }
    public DbSet<WeeklyPerformanceEmailLog> WeeklyPerformanceEmailLogs { get; set; }
    public DbSet<Ad> Ads { get; set; }
    public DbSet<AdSlide> AdSlides { get; set; }
    public DbSet<AdLog> AdLogs { get; set; }

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

        builder.Entity<PasswordResetOtp>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AutomationSettings>(entity =>
        {
            entity.ToTable("AutomationSettings");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ExecutionMode)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(x => x.WhatsAppGroupUrl)
                .HasMaxLength(500);

            entity.Property(x => x.WeeklyEmailSendDay)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");
        });

        builder.Entity<AutomationLog>(entity =>
        {
            entity.ToTable("AutomationLogs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.AutomationType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.EntityType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.EntityId)
                .HasMaxLength(100);

            entity.Property(x => x.ActionName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.ExecutionMode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Message)
                .HasMaxLength(500);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.AutomationType);
            entity.HasIndex(x => x.EntityType);
            entity.HasIndex(x => x.EntityId);
            entity.HasIndex(x => x.CreatedAt);
        });

        builder.Entity<AutomationSuggestion>(entity =>
        {
            entity.ToTable("AutomationSuggestions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.AutomationType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.EntityType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.EntityId)
                .HasMaxLength(100);

            entity.Property(x => x.SuggestedAction)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.AutomationType);
            entity.HasIndex(x => x.EntityType);
            entity.HasIndex(x => x.EntityId);
            entity.HasIndex(x => x.IsReviewed);
            entity.HasIndex(x => x.CreatedAt);
        });

        builder.Entity<UserLoginHistory>(entity =>
        {
            entity.ToTable("UserLoginHistories");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(x => x.FullName)
                .HasMaxLength(200);

            entity.Property(x => x.Email)
                .HasMaxLength(256);

            entity.Property(x => x.IpAddress)
                .HasMaxLength(100);

            entity.Property(x => x.SessionId)
                .HasMaxLength(100);

            entity.Property(x => x.FailureReason)
                .HasMaxLength(500);

            entity.Property(x => x.LoginAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => x.LoginAt);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => x.IsSuccess);
        });

        builder.Entity<UserActiveSession>(entity =>
        {
            entity.ToTable("UserActiveSessions");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(x => x.FullName)
                .HasMaxLength(200);

            entity.Property(x => x.Email)
                .HasMaxLength(256);

            entity.Property(x => x.SessionId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.IpAddress)
                .HasMaxLength(100);

            entity.Property(x => x.LoginAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(x => x.LastSeenAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Email);
            entity.HasIndex(x => x.SessionId);
            entity.HasIndex(x => x.IsActive);
            entity.HasIndex(x => x.LastSeenAt);
        });
        
        builder.Entity<PredictionReminderLog>(entity =>
        {
            entity.ToTable("PredictionReminderLogs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(x => x.ReminderType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.EmailTo)
                .HasMaxLength(256);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.FixtureId);
            entity.HasIndex(x => x.ReminderType);
            entity.HasIndex(x => x.IsSent);
            entity.HasIndex(x => x.CreatedAt);

            entity.HasOne(x => x.Fixture)
                .WithMany()
                .HasForeignKey(x => x.FixtureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WeeklyPerformanceEmailLog>(entity =>
        {
            entity.ToTable("WeeklyPerformanceEmailLogs");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(x => x.EmailTo)
                .HasMaxLength(256);

            entity.Property(x => x.EmailType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.EmailType);
            entity.HasIndex(x => x.WeekStartDate);
            entity.HasIndex(x => x.WeekEndDate);
            entity.HasIndex(x => x.IsSent);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        // Ads
        builder.Entity<Ad>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.ButtonText)
                .HasMaxLength(80);

            entity.Property(x => x.ButtonUrl)
                .HasMaxLength(800);

            entity.Property(x => x.CreatedByUserId)
                .HasMaxLength(450);

            entity.Property(x => x.UpdatedByUserId)
                .HasMaxLength(450);

            entity.Property(x => x.SelectionMode)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(AdSelectionMode.Ordered);

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.Property(x => x.IsDeleted)
                .HasDefaultValue(false);

            entity.Property(x => x.DisplayOrder)
                .HasDefaultValue(0);

            entity.Property(x => x.Priority)
                .HasDefaultValue(0);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasMany(x => x.Slides)
                .WithOne(x => x.Ad)
                .HasForeignKey(x => x.AdId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new
            {
                x.IsActive,
                x.IsDeleted,
                x.DisplayOrder
            });

            entity.HasIndex(x => new
            {
                x.StartAt,
                x.EndAt
            });

            entity.HasIndex(x => x.Priority);
        });

        // AdSlides
        builder.Entity<AdSlide>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.ImagePath)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.AudioPath)
                .HasMaxLength(500);

            entity.Property(x => x.ButtonText)
                .HasMaxLength(80);

            entity.Property(x => x.ButtonUrl)
                .HasMaxLength(800);

            entity.Property(x => x.DurationSeconds)
                .HasDefaultValue(4);

            entity.Property(x => x.DisplayOrder)
                .HasDefaultValue(0);

            entity.Property(x => x.IsActive)
                .HasDefaultValue(true);

            entity.Property(x => x.IsDeleted)
                .HasDefaultValue(false);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasIndex(x => new
            {
                x.AdId,
                x.IsActive,
                x.IsDeleted,
                x.DisplayOrder
            });
        });

        // AdLogs
        builder.Entity<AdLog>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .HasMaxLength(450);

            entity.Property(x => x.SessionId)
                .HasMaxLength(120);

            entity.Property(x => x.EventType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.DeviceType)
                .HasMaxLength(50);

            entity.Property(x => x.PageName)
                .HasMaxLength(100);

            entity.Property(x => x.PageUrl)
                .HasMaxLength(800);

            entity.Property(x => x.IpAddress)
                .HasMaxLength(80);

            entity.Property(x => x.UserAgent)
                .HasMaxLength(1000);

            entity.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.Ad)
                .WithMany()
                .HasForeignKey(x => x.AdId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.AdSlide)
                .WithMany()
                .HasForeignKey(x => x.AdSlideId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(x => new
            {
                x.AdId,
                x.EventType,
                x.CreatedAt
            });

            entity.HasIndex(x => new
            {
                x.UserId,
                x.CreatedAt
            });

            entity.HasIndex(x => new
            {
                x.SessionId,
                x.CreatedAt
            });

            entity.HasIndex(x => new
            {
                x.DeviceType,
                x.CreatedAt
            });
        });
    }
}
