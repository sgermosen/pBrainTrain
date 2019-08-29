using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jmo.Backend.Data;
using Jmo.Backend.Data.Domain;
using Jmo.Backend.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Jmo.Backend.Controllers
{
    public class PreguntasController : Controller
    {
        private readonly DataContext _context;

        public PreguntasController(DataContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Detail(int id)
        {
            var pregunta = await _context.Preguntas.Where(p=> p.Id==id)
                                                    .Include(p=>p.Categoria).Include(p => p.Respuestas)
                                                    .FirstOrDefaultAsync();
                       
            return View(pregunta);
        }

        public async Task<IActionResult> Create()
        {
            var cat = await _context.Categorias.ToListAsync();
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
            if(!ModelState.IsValid)
            {
                var cat = await _context.Categorias.ToListAsync();
                ViewBag.Categorias = new SelectList(cat, "Id", "Nombre",vm.CategoriaId);
            }
            var pathUrl = string.Empty;

            if(vm.Imagen !=null && vm.Imagen.Length>0)
            {
                var guid = Guid.NewGuid().ToString();
                var file = $"{guid}.jpg";

                pathUrl = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\JuegosMentales", file);

                using (var stream = new FileStream(pathUrl,FileMode.Create))
                {
                    await vm.Imagen.CopyToAsync(stream);
                }

                pathUrl = $"~/images/JuegosMentales/{file}";
            }
            
            var pregunta = new Pregunta
            {
                CategoriaId=vm.CategoriaId,
                Cuestionante= vm.Cuestionante, ImagenUrl= pathUrl
            };

            _context.Add(pregunta);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index","Home");
        }
    }
}