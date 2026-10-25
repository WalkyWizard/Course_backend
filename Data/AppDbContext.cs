using Microsoft.EntityFrameworkCore;
using SNovimGodom.Models;

namespace SNovimGodom.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}