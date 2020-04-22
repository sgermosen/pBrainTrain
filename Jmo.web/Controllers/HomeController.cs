using Jmo.Web.Models;
using Jmo.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;

namespace Jmo.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IQuestionRepository _repository;

        public HomeController(IQuestionRepository repository)
        {
            _repository = repository;
        }
        public IActionResult Index()
        {
            var questions = _repository.GetQuestions();

            return View(questions.OrderByDescending(p => p.Id));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
