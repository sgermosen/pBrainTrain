using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Jmo.Backend.Models;
using Jmo.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Jmo.Backend.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataContext _context;

        public HomeController(DataContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var preguntas = await _context.Preguntas
                                          .Include(p=>p.Categoria)
                                          .Include(p => p.Respuestas)
                                          .ToListAsync();            

            return View(preguntas);
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
