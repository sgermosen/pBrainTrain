using Jmo.Web.Data.Domain;
using Jmo.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Jmo.Web.Controllers
{
    public class AnswersController : Controller
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IAnswerRepository _repository;

        public AnswersController(IAnswerRepository repository, IQuestionRepository questionRepository)
        {
            _repository = repository;
            _questionRepository = questionRepository;
        }

        public async Task<IActionResult> Create(int id)
        {
            var preg = _questionRepository.GetQuestion(id);

            var resp = new Answer { QuestionId = preg.Id };

            return View(resp);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Answer rpt)
        {
            if (!ModelState.IsValid)
                return View();
            rpt.Id = 0;

          await  _repository.CreateAsync(rpt);
            
            return RedirectToAction("Detail", "Questions", new { id = rpt.QuestionId });
        }
    }
}