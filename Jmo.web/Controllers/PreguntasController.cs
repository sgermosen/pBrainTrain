using Jmo.Web.Data.Domain;
using Jmo.Web.Repositories;
using Jmo.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jmo.Web.Helpers;

namespace Jmo.Web.Controllers
{
    public class PreguntasController : Controller
    {
        private readonly IPreguntaRepository _repository;
        private readonly ICategoriaRepository _categoriaRepository;
        private readonly IUserHelper userHelper;

        public PreguntasController(IPreguntaRepository repository, IUserHelper userHelper, ICategoriaRepository categoriaRepository)
        {
            _repository = repository;
            this.userHelper = userHelper;
            _categoriaRepository = categoriaRepository;
        }
        public IActionResult Detail(int id)
        {
            var pregunta = _repository.GetPregunta(id);

            return View(pregunta);
        }

        public IActionResult Create()
        {
            var cat = _categoriaRepository.GetCategorias().ToList();
            ViewBag.Categorias = new SelectList(cat, "Id", "Nombre");
            var vm = new PreguntaViewModel
            {
                Titulo = "Nueva Pregunta"
            };
            return View("PreguntaForm", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PreguntaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var cat = _categoriaRepository.GetCategorias().ToList();
                ViewBag.Categorias = new SelectList(cat, "Id", "Nombre", vm.CategoriaId);
            }
            var pathUrl = string.Empty;

            if (vm.Imagen != null && vm.Imagen.Length > 0)
            {
                var guid = Guid.NewGuid().ToString();
                var file = $"{guid}.jpg";

                pathUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\JuegosMentales", file);

                using (var stream = new FileStream(pathUrl, FileMode.Create))
                {
                    await vm.Imagen.CopyToAsync(stream);
                }

                pathUrl = $"~/images/JuegosMentales/{file}";
            }

            var pregunta = new Pregunta
            {
                CategoriaId = vm.CategoriaId,
                Cuestionante = vm.Cuestionante,
                ImagenUrl = pathUrl
            };

          await  _repository.CreateAsync(pregunta); 
            return RedirectToAction("Index", "Home");
        }
    }
}