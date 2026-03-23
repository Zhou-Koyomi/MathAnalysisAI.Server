using Microsoft.EntityFrameworkCore;
using MathAnalysisAI.Models;

namespace MathAnalysisAI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<WrongQuestion> WrongQuestions { get; set; }
    }
}