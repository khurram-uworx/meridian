using Microsoft.EntityFrameworkCore;
using Uworx.Meridian.Entities;

namespace Uworx.Meridian.Infrastructure.Data;

public class MeridianDbContext : DbContext
{
    public MeridianDbContext(DbContextOptions<MeridianDbContext> options)
        : base(options)
    {
    }

    public DbSet<Learner> Learners => Set<Learner>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<EnrollmentOperation> EnrollmentOperations => Set<EnrollmentOperation>();
    public DbSet<EnrollmentOperationEvent> EnrollmentOperationEvents => Set<EnrollmentOperationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Learner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.JiraAccountId).IsRequired();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SourceType).IsRequired();
            entity.Property(e => e.SourceLocator).IsRequired();
            entity.Property(e => e.CoursePath).IsRequired();
            entity.Property(e => e.CourseYamlSnapshot).IsRequired();
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.JiraEpicKey).IsRequired();
            entity.Property(e => e.EnrolledAt).IsRequired();
            entity.Property(e => e.SourceRevision);

            entity.HasOne(e => e.Learner)
                .WithMany()
                .HasForeignKey(e => e.LearnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                .WithMany()
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuizId).IsRequired();
            entity.Property(e => e.Score).IsRequired();
            entity.Property(e => e.MaxScore).IsRequired();
            entity.Property(e => e.AttemptedAt).IsRequired();
            entity.Property(e => e.JiraCommentId);

            entity.HasOne(e => e.Enrollment)
                .WithMany()
                .HasForeignKey(e => e.EnrollmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnrollmentOperation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LearnerEmail).IsRequired();
            entity.Property(e => e.SourceType).IsRequired();
            entity.Property(e => e.SourceUri).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CurrentStage).IsRequired();
            entity.Property(e => e.CurrentMessage).IsRequired();
            entity.Property(e => e.IsWarning).IsRequired();
            entity.Property(e => e.StartedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
            entity.Property(e => e.SubPath);
            entity.Property(e => e.ProgressPercent);
            entity.Property(e => e.EnrollmentId);
            entity.Property(e => e.JiraEpicKey);
            entity.Property(e => e.ErrorMessage);
            entity.Property(e => e.CompletedAtUtc);

            entity.HasMany(e => e.Events)
                .WithOne(e => e.EnrollmentOperation)
                .HasForeignKey(e => e.EnrollmentOperationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnrollmentOperationEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.Stage).IsRequired();
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.IsWarning).IsRequired();
            entity.Property(e => e.ProgressPercent);
        });
    }
}
