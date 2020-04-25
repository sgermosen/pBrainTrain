using Jmo.Web.Data.Domain;
using Jmo.Web.Repositories;
using Jmo.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Jmo.Web.Controllers
{
    public class QuestionsController : Controller
    {
        private readonly IQuestionRepository _repository;
        private readonly IChoiseRepository _ChoiseRepository;
        private readonly ICategoryRepository _categoryRepository;

        public QuestionsController(IQuestionRepository repository, IChoiseRepository ChoiseRepository,
            ICategoryRepository categoryRepository)
        {
            _repository = repository;
            _ChoiseRepository = ChoiseRepository;
            _categoryRepository = categoryRepository;
        }

        public IActionResult Detail(int id)
        {
            var question = _repository.GetQuestion(id);

            return View(question);
        }

        public IActionResult Create()
        {
            var cat = _categoryRepository.GetCategories().ToList();
            ViewBag.Categories = new SelectList(cat, "Id", "Name");
            var vm = new QuestionViewModel
            {
                Title = "New Question"
            };
            return View("QuestionForm", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create(QuestionViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var cat = _categoryRepository.GetCategories().ToList();
                ViewBag.Categories = new SelectList(cat, "Id", "Name", vm.CategoryId);
            }
            var pathUrl = string.Empty;

            if (vm.Image != null && vm.Image.Length > 0)
            {
                var guid = Guid.NewGuid().ToString();
                var file = $"{guid}.jpg";

                pathUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\JuegosMentales", file);

                using (var stream = new FileStream(pathUrl, FileMode.Create))
                {
                    await vm.Image.CopyToAsync(stream);
                }

                pathUrl = $"~/images/JuegosMentales/{file}";
            }

            var question = new Question
            {
                AnswerRestrospective = vm.AnswerRestrospective,
                CategoryId = vm.CategoryId,
                Questionant = vm.Questionant,
                ImagenUrl = pathUrl
            };

           // var Choises = new List<Choise>();

            question.Choises.Add(new Choise
            {
                IsCorrect = vm.Choise1IsCorrect,
                Option = vm.Choise1,
            });
            question.Choises.Add(new Choise
            {
                IsCorrect = vm.Choise2IsCorrect,
                Option = vm.Choise2,
            });
            question.Choises.Add(new Choise
            {
                IsCorrect = vm.Choise3IsCorrect,
                Option = vm.Choise3,
            });
            question.Choises.Add(new Choise
            {
                IsCorrect = vm.Choise4IsCorrect,
                Option = vm.Choise4,
            });

            _repository.AddQuestion(question);

            await _repository.SaveAllAsync();

            return RedirectToAction("Detail", "Questions", new { id = question.Id });
        }
    }
}