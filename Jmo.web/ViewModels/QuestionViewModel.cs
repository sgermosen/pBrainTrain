using Microsoft.AspNetCore.Http;

namespace Jmo.Web.ViewModels
{
    public class QuestionViewModel
    {
        //Necesario para cuando yo desida trabajar el edit
        public int Id { get; set; }

        public string Questionant { get; set; }

        public string Title { get; set; }

        public int CategoryId { get; set; }

        public IFormFile Image { get; set; }

        public string Choise1 { get; set; }
        public string Choise4 { get; set; }
        public string Choise2 { get; set; }
        public string Choise3 { get; set; }

        public bool Choise1IsCorrect { get; set; }
        public bool Choise4IsCorrect { get; set; }
        public bool Choise2IsCorrect { get; set; }
        public bool Choise3IsCorrect { get; set; }
        public string AnswerRestrospective { get; set; }
    }
}
