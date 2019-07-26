using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Jmo.Backend.Data.Domain
{
    public class Categoria
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(250)]
        public string ImagenUrl { get; set; }
    }
}
