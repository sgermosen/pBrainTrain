using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Jmo.Web.ViewModels
{
    public class QuestionViewModel
    {
        //Necesario para cuando yo desida trabajar el edit
        public int Id { get; set; }

        [StringLength(150)]
        public string Questionant { get; set; }

        public string Title { get; set; }

        [Display (Name="Category")]
        public int CategoryId { get; set; }

        public IFormFile Image { get; set; }

        public string Answer1 { get; set; }
        public string Answer4 { get; set; }
        public string Answer2 { get; set; }
        public string Answer3 { get; set; }

        public bool Answer1IsCorrect { get; set; }
        public bool Answer4IsCorrect { get; set; }
        public bool Answer2IsCorrect { get; set; }
        public bool Answer3IsCorrect { get; set; }

    }
}
