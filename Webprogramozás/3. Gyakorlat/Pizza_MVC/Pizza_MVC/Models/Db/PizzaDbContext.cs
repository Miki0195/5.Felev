using System;
using Microsoft.EntityFrameworkCore;
using Pizza_MVC.Model;

namespace Pizza_MVC.Models.Db
{
	public class PizzaDbContext : DbContext
	{
		public PizzaDbContext(DbContextOptions<PizzaDbContext> options) : base(options)
		{
		}

		public DbSet<Pizza> Pizzas { get; set; }
	}
}

