using Microsoft.EntityFrameworkCore;
using AssistantEngineer.Models;

namespace AssistantEngineer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) 
    : DbContext(options)
{
    public DbSet<Room> Rooms { get; set; }
    public  DbSet<Window> Windows { get; set; }
}