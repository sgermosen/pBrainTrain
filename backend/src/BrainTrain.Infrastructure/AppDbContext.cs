using BrainTrain.Domain;
using Microsoft.EntityFrameworkCore;

namespace BrainTrain.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Choice> Choices => Set<Choice>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<UserCategoryStat> UserCategoryStats => Set<UserCategoryStat>();
    public DbSet<DailyChallengeEntry> DailyChallengeEntries => Set<DailyChallengeEntry>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<Duel> Duels => Set<Duel>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(40);
            e.Property(x => x.AvatarCode).HasMaxLength(24);
            e.Property(x => x.DeviceId).HasMaxLength(128);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.DeviceId).IsUnique();
            // Cubre la consulta del leaderboard semanal: WHERE WeekStart = X ORDER BY WeeklyXp DESC
            e.HasIndex(x => new { x.WeekStartUtc, x.WeeklyXp }).IsDescending(false, true);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.Property(x => x.TokenHash).HasMaxLength(88);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DeviceToken>(e =>
        {
            e.Property(x => x.Token).HasMaxLength(512);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Category>(e =>
        {
            e.Property(x => x.Slug).HasMaxLength(40);
            e.Property(x => x.Name).HasMaxLength(60);
            e.Property(x => x.Emoji).HasMaxLength(8);
            e.Property(x => x.Color).HasMaxLength(9);
            e.Property(x => x.Description).HasMaxLength(300);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        b.Entity<Question>(e =>
        {
            e.Property(x => x.Text).HasMaxLength(600);
            e.Property(x => x.Explanation).HasMaxLength(800);
            e.Property(x => x.FunFact).HasMaxLength(500);
            e.Property(x => x.ImagePath).HasMaxLength(200);
            // Cubre la selección de preguntas por categoría/dificultad activas
            e.HasIndex(x => new { x.CategoryId, x.IsActive, x.Difficulty });
            e.HasOne(x => x.Category).WithMany(c => c.Questions)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Choice>(e =>
        {
            e.Property(x => x.Text).HasMaxLength(300);
            e.HasIndex(x => x.QuestionId);
            e.HasOne(x => x.Question).WithMany(q => q.Choices)
                .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<GameSession>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever(); // Guid generado en app: evita ida y vuelta
            e.Property(x => x.QuestionIdsCsv).HasMaxLength(400);
            e.HasIndex(x => new { x.UserId, x.StartedAtUtc });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UserCategoryStat>(e =>
        {
            e.HasKey(x => new { x.UserId, x.CategoryId });
        });

        b.Entity<DailyChallengeEntry>(e =>
        {
            e.HasKey(x => new { x.UserId, x.DateUtc });
        });

        b.Entity<Achievement>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(60);
            e.Property(x => x.Name).HasMaxLength(80);
            e.Property(x => x.Description).HasMaxLength(300);
            e.Property(x => x.Emoji).HasMaxLength(8);
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<UserAchievement>(e =>
        {
            e.HasKey(x => new { x.UserId, x.AchievementId });
            e.HasOne(x => x.User).WithMany(u => u.Achievements)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Achievement).WithMany()
                .HasForeignKey(x => x.AchievementId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Duel>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Code).HasMaxLength(8);
            e.Property(x => x.QuestionIdsCsv).HasMaxLength(400);
            e.HasIndex(x => x.Code).IsUnique();
            // Cubre el matchmaking aleatorio: duelos públicos esperando rival.
            e.HasIndex(x => new { x.IsOpenToPublic, x.Status });
            e.HasIndex(x => x.ChallengerUserId);
            e.HasIndex(x => x.OpponentUserId);
            e.HasOne(x => x.Challenger).WithMany().HasForeignKey(x => x.ChallengerUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Opponent).WithMany().HasForeignKey(x => x.OpponentUserId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<PurchaseReceipt>(e =>
        {
            e.Property(x => x.ProductId).HasMaxLength(120);
            e.Property(x => x.TransactionId).HasMaxLength(256);
            e.Property(x => x.ReceiptHash).HasMaxLength(88);
            e.HasIndex(x => new { x.Platform, x.TransactionId }).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
