namespace PizzaShop.Model;

/// <summary>
/// Represents a pizza in the PizzaShop application.
/// </summary>
public class Pizza
{
    /// <summary>The unique identifier for the pizza.</summary>
    public int Id { get; set; }

    /// <summary>The name of the pizza.</summary>
    public required string Name { get; set; }

    /// <summary>The price of the pizza.</summary>
    public required double Price { get; set; }

    /// <summary>The ingredients of the pizza.</summary>
    public required string Ingredients { get; set; }

    /// <summary>The allergens present in the pizza.</summary>
    public required string Allergens { get; set; }
}
