using System.ComponentModel.DataAnnotations;

namespace Jmo.Web.Data.Domain
{
    public class Respuesta : IEntity
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Opcion { get; set; }

        public bool EsCorrecta { get; set; }

        public int PreguntaId { get; set; }
        public Pregunta Pregunta { get; set; }
    }
}
