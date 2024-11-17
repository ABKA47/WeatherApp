using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Core.Models;

namespace WeatherApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<WeatherCache> WeatherCache { get; set; }
        public DbSet<FavoriteCity> FavoriteCities { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<FavoriteCity>()
                   .HasOne(fc => fc.User)
                   .WithMany(u => u.FavoriteCities)
                   .HasForeignKey(fc => fc.UserId);
        }
    }
}
