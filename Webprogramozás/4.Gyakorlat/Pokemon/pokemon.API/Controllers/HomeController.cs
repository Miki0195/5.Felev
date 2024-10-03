using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using pokemon.Library.Model;
using pokemon.Logic;

namespace pokemon.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("GetAllPokemons")]
        public IEnumerable<PokemonData> Get()
        {
            return DataHandler.GetAllPokemons();
        }
    }
}

