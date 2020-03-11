using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Jmo.Web.ViewModels
{
    public class PreguntaViewModel
    {
        //Necesario para cuando yo desida trabajar el edit
        public int Id { get; set; }

        [StringLength(150)]
        public string Cuestionante { get; set; }

        public string Titulo { get; set; }

        [Display (Name="Categoria")]
        public int CategoriaId { get; set; }

        public IFormFile Imagen { get; set; }

    }
}
