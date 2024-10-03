using System;
namespace pokemon.Library.Model
{
	public class PokemonData
	{

        public int Id { get; set; }
        public string Name { get; set; }
		public string Image { get; set; }
		public string Type { get; set; }
		public string SubType { get; set; }
		public string Height { get; set; }
		public string Weight { get; set; }
		public int NextEvolution { get; set; }

        public PokemonData()
		{
			
		}
	}
}

