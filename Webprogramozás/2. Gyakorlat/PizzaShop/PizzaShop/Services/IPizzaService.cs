using PizzaShop.Model;

namespace PizzaShop.Services;

/// <summary>
/// Defines the operations for managing pizzas in the PizzaShop application.
/// </summary>
public interface IPizzaService
{
    /// <summary>
    /// Adds a new pizza to the collection.
    /// </summary>
    /// <param name="pizza">The pizza to be added to the collection.</param>
    void Add(Pizza pizza);

    /// <summary>
    /// Retrieves a pizza by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the pizza.</param>
    /// <returns>The pizza with the specified identifier, or null if not found.</returns>
    Pizza? GetPizzaById(int id);

    /// <summary>
    /// Retrieves all pizzas in the collection.
    /// </summary>
    /// <returns>A list of all pizzas.</returns>
    List<Pizza> GetAllPizzas();

    /// <summary>
    /// Deletes a specified pizza from the collection.
    /// </summary>
    /// <param name="pizza">The pizza to delete.</param>
    void DeletePizza(Pizza pizza);

    /// <summary>
    /// Seeds the pizza collection with a predefined set of pizzas.
    /// </summary>
    void Seed()
    {
        Add(new Pizza()
        {
            Name = "Margherita",
            Price = 8.99,
            Ingredients = "Tomato, Mozzarella, Basil",
            Allergens = "Gluten, Dairy"
        });
        Add(new Pizza()
        {
            Name = "Pepperoni",
            Price = 9.99,
            Ingredients = "Tomato, Mozzarella, Pepperoni",
            Allergens = "Gluten, Dairy"
        });
        Add(new Pizza()
        {
            Name = "BBQ Chicken",
            Price = 10.99,
            Ingredients = "BBQ Sauce, Chicken, Mozzarella, Red Onions",
            Allergens = "Gluten, Dairy"
        });
        Add(new Pizza()
        {
            Name = "Hawaiian",
            Price = 9.99,
            Ingredients = "Tomato, Mozzarella, Ham, Pineapple",
            Allergens = "Gluten, Dairy"
        });
        Add(new Pizza()
        {
            Name = "Veggie",
            Price = 8.99,
            Ingredients = "Tomato, Mozzarella, Bell Peppers, Onions, Olives",
            Allergens = "Gluten, Dairy"
        });
        Add(new Pizza()
        {
            Name = "Meat Lovers",
            Price = 11.99,
            Ingredients = "Tomato, Mozzarella, Pepperoni, Sausage, Bacon, Ham",
            Allergens = "Gluten, Dairy"
        });
        Add(new Pizza()
        {
            Name = "Four Cheese",
            Price = 10.99,
            Ingredients = "Tomato, Mozzarella, Cheddar, Parmesan, Gorgonzola",
            Allergens = "Gluten, Dairy"
        });
        Add(new Pizza()
        {
            Name = "Buffalo Chicken",
            Price = 10.99,
            Ingredients = "Buffalo Sauce, Chicken, Mozzarella, Blue Cheese",
            Allergens = "Gluten, Dairy"
        });
        Add(new Pizza()
        {
            Name = "Mushroom",
            Price = 9.49,
            Ingredients = "Tomato, Mozzarella, Mushrooms, Garlic",
            Allergens = "Gluten, Dairy"
        });
        Add(new Pizza()
        {
            Name = "Spinach and Feta",
            Price = 9.99,
            Ingredients = "Tomato, Mozzarella, Spinach, Feta Cheese",
            Allergens = "Gluten, Dairy"
        });
    }
}
