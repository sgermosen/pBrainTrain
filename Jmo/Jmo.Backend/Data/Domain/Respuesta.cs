using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Jmo.Backend.Data.Domain
{
    public class Respuesta
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Opcion { get; set; }

        public bool EsCorrecta { get; set; }

        public int PreguntaId { get; set; }
        public Pregunta Pregunta { get; set; }
    }
}
