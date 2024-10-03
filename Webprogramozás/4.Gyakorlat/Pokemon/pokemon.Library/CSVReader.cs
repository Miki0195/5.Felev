using System;
using System.Text;
using pokemon.Library.Model;

namespace pokemon.Library
{
	public class CSVReader
	{
		private List<PokemonData> pokemons;
		public CSVReader()
		{
			pokemons = new List<PokemonData>();
		}

		public List<PokemonData> ParseCSV(string file)
		{
			pokemons.Clear();
            using (StreamReader sr = new StreamReader(file,
				Encoding.Default))
			{
				string? header = sr.ReadLine();
				while (!sr.EndOfStream)
				{
					string[] line = sr.ReadLine().Split(',');
					PokemonData pokemon = new PokemonData();

					pokemon.Id = int.Parse(line[0]);
					pokemon.Name = line[2];
					pokemon.Image = line[3];
					pokemon.Type = line[4];
                    pokemon.SubType = line[5];
					pokemon.Height = line[6];
                    pokemon.Weight = line[7];
					if (String.IsNullOrEmpty(line[19])) pokemon.NextEvolution = 0;
					else pokemon.NextEvolution = int.Parse(line[19]);

					pokemons.Add(pokemon);
                }
			}

			return pokemons;
		}
	}
}

