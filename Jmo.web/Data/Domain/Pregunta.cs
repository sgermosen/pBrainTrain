using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Jmo.Web.Data.Domain
{
    public class Pregunta : IEntity
    {
        public int Id { get; set; }

        [StringLength(150)]
        public string Cuestionante { get; set; }

        [StringLength(250)]
        public string ImagenUrl { get; set; }

        public int CategoriaId { get; set; }
        public Categoria Categoria { get; set; }

        public ICollection<Respuesta> Respuestas { get; set; }

        public Pregunta()
        {
            Respuestas = new Collection<Respuesta>();
        }
    }
}
