using CPE_Review_Hub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityUserContext<ApplicationUser>(options)
{
    public DbSet<AcademicLevel> AcademicLevels => Set<AcademicLevel>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<ReviewMaterial> ReviewMaterials => Set<ReviewMaterial>();
    public DbSet<LessonImage> LessonImages => Set<LessonImage>();
    public DbSet<LessonActivity> LessonActivities => Set<LessonActivity>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Choice> Choices => Set<Choice>();
    public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
    public DbSet<ActivitySubmission> ActivitySubmissions => Set<ActivitySubmission>();
    public DbSet<SubmissionAttachment> SubmissionAttachments => Set<SubmissionAttachment>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Keep indexed Identity keys within MySQL's utf8mb4 index limits.
        builder.Entity<ApplicationUser>().Property(u => u.Id).HasMaxLength(128);
        builder.Entity<ApplicationUser>().HasIndex(u => u.NormalizedEmail).IsUnique();
        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property(x => x.LoginProvider).HasMaxLength(128);
            entity.Property(x => x.ProviderKey).HasMaxLength(128);
        });
        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property(x => x.LoginProvider).HasMaxLength(128);
            entity.Property(x => x.Name).HasMaxLength(128);
        });

        builder.Entity<AcademicLevel>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasData(
                new AcademicLevel { Id = 200, Name = "Sophomore" },
                new AcademicLevel { Id = 300, Name = "Junior" },
                new AcademicLevel { Id = 400, Name = "Senior" });
        });
        builder.Entity<Subject>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.AcademicLevelId, x.IsPublished });
            entity.HasOne(x => x.AcademicLevel).WithMany(x => x.Subjects)
                .HasForeignKey(x => x.AcademicLevelId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Lesson>(entity =>
        {
            entity.HasIndex(x => new { x.SubjectId, x.SortOrder });
            entity.HasOne(x => x.Subject).WithMany(x => x.Lessons)
                .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ReviewMaterial>(entity =>
        {
            entity.Property(x => x.Content).HasColumnType("longtext");
            entity.HasIndex(x => new { x.LessonId, x.SortOrder });
            entity.HasOne(x => x.Lesson).WithMany(x => x.ReviewMaterials)
                .HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<LessonImage>(entity =>
        {
            entity.HasIndex(x => new { x.LessonId, x.DisplayOrder });
            entity.HasOne(x => x.Lesson).WithMany(x => x.Images)
                .HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<LessonActivity>(entity =>
        {
            entity.HasIndex(x => x.LessonId).IsUnique();
            entity.HasOne(x => x.Lesson).WithOne(x => x.Activity)
                .HasForeignKey<LessonActivity>(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Assessment>(entity =>
        {
            entity.HasIndex(x => new { x.SubjectId, x.Type });
            entity.HasIndex(x => new { x.LessonId, x.Type }).IsUnique();
            entity.HasOne(x => x.Subject).WithMany(x => x.Assessments)
                .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Lesson).WithMany(x => x.Assessments)
                .HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Question>(entity =>
        {
            entity.HasIndex(x => x.SourceKey).IsUnique();
            entity.HasIndex(x => new { x.SubjectId, x.LessonId, x.IsActive });
            entity.HasOne(x => x.Subject).WithMany(x => x.Questions)
                .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Lesson).WithMany(x => x.Questions)
                .HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Choice>(entity =>
        {
            entity.HasIndex(x => new { x.QuestionId, x.SortOrder });
            entity.HasOne(x => x.Question).WithMany(x => x.Choices)
                .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<AssessmentQuestion>(entity =>
        {
            entity.HasKey(x => new { x.AssessmentId, x.QuestionId });
            entity.HasIndex(x => new { x.AssessmentId, x.SortOrder }).IsUnique();
            entity.HasOne(x => x.Assessment).WithMany(x => x.AssessmentQuestions)
                .HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question).WithMany(x => x.AssessmentQuestions)
                .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ActivitySubmission>(entity =>
        {
            entity.Property(x => x.Response).HasColumnType("longtext");
            entity.HasIndex(x => new { x.UserId, x.LessonActivityId }).IsUnique();
            entity.HasOne(x => x.LessonActivity).WithMany(x => x.Submissions)
                .HasForeignKey(x => x.LessonActivityId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany(x => x.ActivitySubmissions)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<SubmissionAttachment>(entity =>
        {
            entity.Property(x => x.Data).HasColumnType("longblob");
            entity.HasIndex(x => x.ActivitySubmissionId);
            entity.HasOne(x => x.ActivitySubmission).WithMany(x => x.Attachments)
                .HasForeignKey(x => x.ActivitySubmissionId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<AssessmentAttempt>(entity =>
        {
            entity.Property(x => x.Percentage).HasPrecision(5, 2);
            entity.HasIndex(x => new { x.UserId, x.AssessmentId, x.SubmittedAtUtc });
            entity.HasOne(x => x.Assessment).WithMany(x => x.Attempts)
                .HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany(x => x.AssessmentAttempts)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<AttemptAnswer>(entity =>
        {
            entity.HasKey(x => new { x.AssessmentAttemptId, x.QuestionId });
            entity.HasOne(x => x.AssessmentAttempt).WithMany(x => x.Answers)
                .HasForeignKey(x => x.AssessmentAttemptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question).WithMany(x => x.AttemptAnswers)
                .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Choice).WithMany(x => x.AttemptAnswers)
                .HasForeignKey(x => x.ChoiceId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
