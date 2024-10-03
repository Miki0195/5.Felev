using pokemon.Library;
using pokemon.Library.Model;

namespace pokemon.Console;

class Program
{
    static void Main(string[] args)
    {
        List<PokemonData> pokemons = Logic.DataHandler.GetAllPokemons();
        foreach (PokemonData pokemon in pokemons)
        {
            System.Console.WriteLine(pokemon.Name);
        }
        System.Console.ReadKey();
    }
}

