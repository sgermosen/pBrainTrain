using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Jmo.Backend.Data.Domain
{
    public class Pregunta
    {
        public int Id { get; set; }

        [StringLength(150)]
        public string Cuestionante { get; set; }

        [StringLength(250)]
        public string ImagenUrl { get; set; }

        public int CategoriaId { get; set; }
        public Categoria Categoria { get; set; }
    }
}
