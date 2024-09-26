using System;
using Pizza_MVC.Model;

namespace Pizza_MVC.Services
{
	public interface IPizzaService
	{
		IEnumerable<Pizza> GetPizzas();
		Pizza? GetPizzaById(int id);
		void Add(Pizza pizza);
		void DeletePizza(Pizza pizza);

		void Seed()
		{
			Add(new Pizza { Name = "Margherita", Price = 5, Description = "Good" });
            Add(new Pizza { Name = "Pepperoni", Price = 6, Description = "Better" });
            Add(new Pizza { Name = "Meat", Price = 8, Description = "Best" });
        }
	}
}

