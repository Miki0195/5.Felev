using System;
using pokemon.Library;
using pokemon.Library.Model;

namespace pokemon.Logic
{
	public static class DataHandler
	{
		private static string csvFilePath = AppDomain.CurrentDomain.BaseDirectory + @"/Data/pokedex.csv";
		private static CSVReader CSVReader = new CSVReader();

		public static List<PokemonData> GetAllPokemons()
		{
			return CSVReader.ParseCSV(csvFilePath);
		}

		public static void AddNewPokemon(PokemonData data)
		{
			throw new NotImplementedException();
		}
	}
}

