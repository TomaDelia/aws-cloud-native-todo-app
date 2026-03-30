using Microsoft.EntityFrameworkCore;
using Backend.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

public class ToDoContext : DbContext
{
    public ToDoContext(DbContextOptions<ToDoContext> options) : base(options)
    {
    }

    public DbSet<ToDoItem> Items { get; set; }  // <--- qui dici "questa tabella si chiama Items"

    public DbSet<User> Users { get; set; }
}
