using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PizzaShop.Services;
using PizzaShop.Model;

namespace PizzaShop.Pages
{
	public class PizzasModel : PageModel
    {
        public readonly IPizzaService pizzaService;

        public Pizza? SelectedPizza { get; set; }

        public PizzasModel(IPizzaService service)
        {
            pizzaService = service;
        }
        public void OnGet()
        {
        }

        public void OnPostOrderPizza([FromForm] int id)
        {
            this.SelectedPizza = pizzaService.GetPizzaById(id);
        }

        public string GetPizzaImageFileName(Pizza pizza)
        {
            string fileName = pizza.Name.ToLower().Replace(" ", "-") + ".jpg";
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", fileName);

            if (System.IO.File.Exists(filePath))
            {
                return "/images/" + fileName;
            }
            else
            {
                return "/images/not-found.jpg";
            }
        }

    }
}
