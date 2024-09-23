using PizzaShop.Model;

namespace PizzaShop.Services;

public class PizzaService : IPizzaService
{
    public static List<Pizza> Pizzas { get; } = new List<Pizza>();
    private int _nextId = 1;

    public void Add(Pizza pizza)
    {
        if (Pizzas.Any())
        {
            // Legmagasabb ID keresése a meglévő pizzák között
            int maxId = Pizzas.Max(p => p.Id);
            pizza.Id = maxId + 1;
        }
        else
        {
            // Ha nincs pizza a listában, az első ID 1 legyen
            pizza.Id = 1;
        }

        Pizzas.Add(pizza);
    }

    public List<Pizza> GetAllPizzas()
    {
        return Pizzas;
    }

    public Pizza? GetPizzaById(int id)
    {
        return Pizzas.FirstOrDefault(x => x.Id == id);
    }

    public void DeletePizza(Pizza pizza)
    {
        Pizzas.Remove(pizza);
    }
}