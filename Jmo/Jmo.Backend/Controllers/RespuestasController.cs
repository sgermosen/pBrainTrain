using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jmo.Backend.Data;
using Jmo.Backend.Data.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Jmo.Backend.Controllers
{
    public class RespuestasController : Controller
    {
        private readonly DataContext _context;

        public RespuestasController(DataContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Create(int id)
        {
            var preg = await _context.Preguntas.FindAsync(id);

            var resp = new Respuesta { PreguntaId = preg.Id };

            return View(resp);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Respuesta rpt)
        {
            if (!ModelState.IsValid)
                return View();
            rpt.Id = 0;

            _context.Add(rpt);
            await _context.SaveChangesAsync();

            return RedirectToAction("Detail", "Preguntas", new { id = rpt.PreguntaId });
        }
    }
}