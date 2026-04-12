using Microsoft.EntityFrameworkCore;
using AssistantEngineer.Models;

namespace AssistantEngineer.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) 
    : DbContext(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Floor> Floors { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public  DbSet<Window> Windows { get; set; }
    public DbSet<Wall> Walls { get; set; }
}