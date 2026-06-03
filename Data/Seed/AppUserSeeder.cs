using MathAnalysisAI.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Data.Seed
{
    public static class AppUserSeeder
    {
        public const string TestUsername = "test_student";
        public const string TestStudentNumber = "20260001";

        public static async Task<int?> SeedDevelopmentTestStudentAsync(
            ApplicationDbContext db,
            CancellationToken cancellationToken = default)
        {
            var existing = await db.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Username == TestUsername || x.StudentNumber == TestStudentNumber,
                    cancellationToken);

            if (existing != null)
            {
                return existing.Id;
            }

            var user = new AppUser
            {
                Username = TestUsername,
                RealName = "测试学生",
                StudentNumber = TestStudentNumber,
                Role = AppUserRole.Student,
                SchoolName = "测试学校",
                DepartmentName = "数学系",
                ClassName = "数学分析测试班",
                CreatedAt = DateTime.UtcNow
            };

            db.AppUsers.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            return user.Id;
        }
    }
}
