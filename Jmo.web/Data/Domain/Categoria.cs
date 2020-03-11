using System.ComponentModel.DataAnnotations;

namespace Jmo.Web.Data.Domain
{
    public class Categoria : IEntity
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(250)]
        public string ImagenUrl { get; set; }
    }
}
