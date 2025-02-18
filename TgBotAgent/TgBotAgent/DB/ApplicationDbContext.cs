using Microsoft.EntityFrameworkCore;

namespace TgBotAgent.DB
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Setting> Settings { get; set; }
        public DbSet<UserLink> UserLinks { get; set; }
        public DbSet<MessageRecord> Messages { get; set; }
        public DbSet<BlacklistWord> BlacklistWords { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Подключение к PostgreSQL
            optionsBuilder.UseNpgsql("Host=localhost;Database=tgagent;Username=postgres;Password=12345Ob@");
        }
    }
}
