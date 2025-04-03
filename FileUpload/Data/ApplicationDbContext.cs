
using FileUpload.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileUpload.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<TaskFile> TaskFiles { get; set; }
    }
}
