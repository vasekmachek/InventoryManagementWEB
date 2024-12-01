using Microsoft.EntityFrameworkCore;
using InventoryManagementWEB.Models;


namespace InventoryManagementWEB.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems { get; set; }

    }
}