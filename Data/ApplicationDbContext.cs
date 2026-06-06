using Microsoft.EntityFrameworkCore;
using MathAnalysisAI.Server.Models;
using MathAnalysisAI.Server.Data.Seed;

namespace MathAnalysisAI.Server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WrongQuestion> WrongQuestions { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<KnowledgePoint> KnowledgePoints { get; set; }
        public DbSet<KnowledgeDependency> KnowledgeDependencies { get; set; }
        public DbSet<Problem> Problems { get; set; }
        public DbSet<StudentSolution> StudentSolutions { get; set; }
        public DbSet<AnalysisResult> AnalysisResults { get; set; }
        public DbSet<AnalysisVisualization> AnalysisVisualizations { get; set; }
        public DbSet<MistakeRecord> MistakeRecords { get; set; }
        public DbSet<UserKnowledgeState> UserKnowledgeStates { get; set; }
        public DbSet<PromptProfile> PromptProfiles { get; set; }
        public DbSet<LLMRequestLog> LLMRequestLogs { get; set; }
        public DbSet<UserCourseStats> UserCourseStats { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<CourseMaterial> CourseMaterials { get; set; }
        public DbSet<MaterialChunk> MaterialChunks { get; set; }
        public DbSet<MaterialChunkKnowledgePoint> MaterialChunkKnowledgePoints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Subject>()
                .HasMany(x => x.Courses)
                .WithOne(x => x.Subject)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Course>()
                .HasMany(x => x.Chapters)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chapter>()
                .HasMany(x => x.KnowledgePoints)
                .WithOne(x => x.Chapter)
                .HasForeignKey(x => x.ChapterId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Course>()
                .HasMany<KnowledgePoint>()
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KnowledgeDependency>()
                .HasOne(x => x.FromKnowledgePoint)
                .WithMany(x => x.OutgoingDependencies)
                .HasForeignKey(x => x.FromKnowledgePointId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KnowledgeDependency>()
                .HasOne(x => x.ToKnowledgePoint)
                .WithMany(x => x.IncomingDependencies)
                .HasForeignKey(x => x.ToKnowledgePointId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Course>()
                .HasMany(x => x.Problems)
                .WithOne(x => x.Course)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chapter>()
                .HasMany(x => x.Problems)
                .WithOne(x => x.Chapter)
                .HasForeignKey(x => x.ChapterId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Problem>()
                .HasMany(x => x.StudentSolutions)
                .WithOne(x => x.Problem)
                .HasForeignKey(x => x.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentSolution>()
                .HasMany(x => x.AnalysisResults)
                .WithOne(x => x.StudentSolution)
                .HasForeignKey(x => x.StudentSolutionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Problem>()
                .HasMany(x => x.AnalysisResults)
                .WithOne(x => x.Problem)
                .HasForeignKey(x => x.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnalysisResult>()
                .HasMany(x => x.Visualizations)
                .WithOne(x => x.AnalysisResult)
                .HasForeignKey(x => x.AnalysisResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnalysisResult>()
                .HasMany(x => x.MistakeRecords)
                .WithOne(x => x.AnalysisResult)
                .HasForeignKey(x => x.AnalysisResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MistakeRecord>()
                .HasOne(x => x.KnowledgePoint)
                .WithMany(x => x.MistakeRecords)
                .HasForeignKey(x => x.KnowledgePointId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserKnowledgeState>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserKnowledgeStates)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserKnowledgeState>()
                .HasOne(x => x.KnowledgePoint)
                .WithMany(x => x.UserKnowledgeStates)
                .HasForeignKey(x => x.KnowledgePointId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCourseStats>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserCourseStats)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCourseStats>()
                .HasOne(x => x.Course)
                .WithMany(x => x.UserCourseStats)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PromptProfile>()
                .HasOne(x => x.Course)
                .WithMany(x => x.PromptProfiles)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LLMRequestLog>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<LLMRequestLog>()
                .HasOne(x => x.AnalysisResult)
                .WithMany()
                .HasForeignKey(x => x.AnalysisResultId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<StudentSolution>()
                .HasOne(x => x.User)
                .WithMany(x => x.StudentSolutions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Problem>()
                .HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CourseMaterial>()
                .HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseMaterial>()
                .HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MaterialChunk>()
                .HasOne(x => x.CourseMaterial)
                .WithMany(x => x.Chunks)
                .HasForeignKey(x => x.CourseMaterialId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaterialChunk>()
                .HasOne(x => x.Course)
                .WithMany()
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MaterialChunk>()
                .HasOne(x => x.Chapter)
                .WithMany()
                .HasForeignKey(x => x.ChapterId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MaterialChunk>()
                .HasOne(x => x.VerifiedByUser)
                .WithMany()
                .HasForeignKey(x => x.VerifiedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MaterialChunkKnowledgePoint>()
                .HasOne(x => x.MaterialChunk)
                .WithMany(x => x.KnowledgePointLinks)
                .HasForeignKey(x => x.MaterialChunkId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MaterialChunkKnowledgePoint>()
                .HasOne(x => x.KnowledgePoint)
                .WithMany()
                .HasForeignKey(x => x.KnowledgePointId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AppUser>()
                .HasIndex(x => x.Username)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
                .HasIndex(x => x.StudentNumber);

            modelBuilder.Entity<Course>()
                .HasIndex(x => new { x.SubjectId, x.Name })
                .IsUnique();

            modelBuilder.Entity<Chapter>()
                .HasIndex(x => new { x.CourseId, x.OrderIndex, x.Name });

            modelBuilder.Entity<KnowledgePoint>()
                .HasIndex(x => new { x.CourseId, x.ChapterId, x.Name });

            modelBuilder.Entity<KnowledgeDependency>()
                .HasIndex(x => new { x.FromKnowledgePointId, x.ToKnowledgePointId })
                .IsUnique();

            modelBuilder.Entity<UserKnowledgeState>()
                .HasIndex(x => new { x.UserId, x.KnowledgePointId })
                .IsUnique();

            modelBuilder.Entity<UserCourseStats>()
                .HasIndex(x => new { x.UserId, x.CourseId })
                .IsUnique();

            modelBuilder.Entity<UserCourseStats>()
                .Property(x => x.AccuracyRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<UserCourseStats>()
                .Property(x => x.RankingScore)
                .HasPrecision(10, 4);

            modelBuilder.Entity<PromptProfile>()
                .HasIndex(x => new { x.CourseId, x.Mode, x.Version });

            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(x => x.CreatedAt);

            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(x => new { x.ProblemId, x.CreatedAt });

            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(x => x.StudentSolutionId);

            modelBuilder.Entity<LLMRequestLog>()
                .HasIndex(x => new { x.Provider, x.ModelName, x.CreatedAt });

            modelBuilder.Entity<LLMRequestLog>()
                .HasIndex(x => x.UserId);

            modelBuilder.Entity<LLMRequestLog>()
                .HasIndex(x => x.AnalysisResultId);

            modelBuilder.Entity<CourseMaterial>()
                .HasIndex(x => x.CourseId);

            modelBuilder.Entity<CourseMaterial>()
                .HasIndex(x => x.ParseStatus);

            modelBuilder.Entity<CourseMaterial>()
                .HasIndex(x => x.UploadedAt);

            modelBuilder.Entity<CourseMaterial>()
                .HasIndex(x => new { x.CourseId, x.MaterialKind, x.Visibility });

            modelBuilder.Entity<CourseMaterial>()
                .HasIndex(x => new { x.CourseId, x.FileHash })
                .IsUnique()
                .HasFilter("[FileHash] IS NOT NULL");

            modelBuilder.Entity<MaterialChunk>()
                .HasIndex(x => new { x.CourseId, x.ChapterId, x.ChunkType });

            modelBuilder.Entity<MaterialChunk>()
                .HasIndex(x => new { x.CourseMaterialId, x.ChunkIndex })
                .IsUnique();

            modelBuilder.Entity<MaterialChunk>()
                .HasIndex(x => new { x.IsVerified, x.ChunkType });

            modelBuilder.Entity<MaterialChunk>()
                .HasIndex(x => new { x.VerifiedByUserId, x.VerifiedAt });

            modelBuilder.Entity<MaterialChunkKnowledgePoint>()
                .HasIndex(x => new { x.KnowledgePointId, x.RelationType });

            modelBuilder.Entity<MaterialChunkKnowledgePoint>()
                .HasIndex(x => new { x.MaterialChunkId, x.KnowledgePointId, x.RelationType })
                .IsUnique();

            modelBuilder.Entity<MaterialChunkKnowledgePoint>()
                .Property(x => x.Confidence)
                .HasPrecision(5, 4);

            modelBuilder.Entity<Subject>().HasData(PlatformSeedData.Subjects);
            modelBuilder.Entity<Course>().HasData(PlatformSeedData.Courses);
            modelBuilder.Entity<Chapter>().HasData(PlatformSeedData.Chapters);
            modelBuilder.Entity<KnowledgePoint>().HasData(PlatformSeedData.KnowledgePoints);
            modelBuilder.Entity<KnowledgeDependency>().HasData(PlatformSeedData.KnowledgeDependencies);
        }
    }
}
