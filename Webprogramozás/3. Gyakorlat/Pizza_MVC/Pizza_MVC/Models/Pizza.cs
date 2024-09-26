using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pizza_MVC.Model
{
	public class Pizza
	{
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
        public required string Name { get; set; }
        public required int Price { get; set; }
        public required string Description { get; set; }
    }
}

