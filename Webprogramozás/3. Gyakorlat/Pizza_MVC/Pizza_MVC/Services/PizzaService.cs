using System;
using Pizza_MVC.Model;
using Pizza_MVC.Models.Db;

namespace Pizza_MVC.Services
{
    public class PizzaService : IPizzaService
    {
        private readonly PizzaDbContext _pizzaDbContext;
        public PizzaService(PizzaDbContext pizzaDbContext)
        {
            _pizzaDbContext = pizzaDbContext;
        }

        public void Add(Pizza pizza)
        {
            _pizzaDbContext.Pizzas.Add(pizza);
            _pizzaDbContext.SaveChanges();
        }

        public void DeletePizza(Pizza pizza)
        {
            _pizzaDbContext.Pizzas.Remove(pizza);
            _pizzaDbContext.SaveChanges();
        }

        public Pizza? GetPizzaById(int id)
        {
            return _pizzaDbContext.Pizzas.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<Pizza> GetPizzas()
        {
            return _pizzaDbContext.Pizzas;
        }
    }
}

