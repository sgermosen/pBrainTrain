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
        private readonly IAnswerRepository _answerRepository;
        private readonly ICategoryRepository _categoryRepository;

        public QuestionsController(IQuestionRepository repository, IAnswerRepository answerRepository,
            ICategoryRepository categoryRepository)
        {
            _repository = repository;
            _answerRepository = answerRepository;
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
                CategoryId = vm.CategoryId,
                Questionant = vm.Questionant,
                ImagenUrl = pathUrl
            };


            var answers = new List<Answer>();

            answers.Add(new Answer
            {
                IsCorrect = vm.Answer1IsCorrect,
                Option = vm.Answer1,
            });
            answers.Add(new Answer
            {
                IsCorrect = vm.Answer2IsCorrect,
                Option = vm.Answer2,
            });
            answers.Add(new Answer
            {
                IsCorrect = vm.Answer3IsCorrect,
                Option = vm.Answer3,
            });
            answers.Add(new Answer
            {
                IsCorrect = vm.Answer4IsCorrect,
                Option = vm.Answer4,
            });

            _repository.AddQuestion(question, answers);

            await _repository.SaveAllAsync();

            return RedirectToAction("Detail", "Questions", new { id = question.Id });
        }
    }
}