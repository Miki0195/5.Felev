using Microsoft.AspNetCore.Mvc.RazorPages;
using PizzaShop.Services;
using PizzaShop.Model;

namespace PizzaShop.Pages
{
    public class AdminModel : PageModel
    {
        public readonly IPizzaService pizzaService;
        public List<Pizza> Pizzas { get; private set; }

        public AdminModel(IPizzaService service)
        {
            pizzaService = service;
            Pizzas = pizzaService.GetAllPizzas();
        }

        // Add pizza handler
        public void OnPostAddPizza(Pizza pizza)
        {
            if (ModelState.IsValid)
            {
                pizzaService.Add(pizza);
            }

            Pizzas = pizzaService.GetAllPizzas(); // Frissítjük a listát
        }

        // Remove pizza handler
        public void OnPostRemovePizza(int pizzaId)
        {
            var pizzaToRemove = pizzaService.GetPizzaById(pizzaId);
            if (pizzaToRemove != null)
            {
                pizzaService.DeletePizza(pizzaToRemove);
            }

            Pizzas = pizzaService.GetAllPizzas(); // Frissítjük a listát a törlés után
        }

        // Update pizza handler
        public void OnPostUpdatePizza(Pizza pizza)
        {
            var pizzaToUpdate = pizzaService.GetPizzaById(pizza.Id);
            if (pizzaToUpdate != null)
            {
                pizzaToUpdate.Name = pizza.Name;
                pizzaToUpdate.Ingredients = pizza.Ingredients;
                pizzaToUpdate.Allergens = pizza.Allergens;
            }

            Pizzas = pizzaService.GetAllPizzas(); // Frissítjük a listát a frissítés után
        }
    }
}
