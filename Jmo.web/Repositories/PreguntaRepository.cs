using Jmo.Infraestructure;
using Jmo.Web.Data;
using Jmo.Web.Data.Domain;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Jmo.Web.Repositories
{
    public class PreguntaRepository : GenericRepository<Pregunta>, IPreguntaRepository
    {
        private readonly ApplicationDbContext _context;

        public PreguntaRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public IEnumerable<Pregunta> GetPreguntas()
        {
            return _context.Preguntas
                .Include(p => p.Categoria)
                .Include(p => p.Respuestas)
                .OrderBy(p => p.Cuestionante);
        }

        public Pregunta GetPregunta(int id)
        {
            return _context.Preguntas.Where(p => p.Id == id)
                .Include(p => p.Categoria)
                .Include(p => p.Respuestas).FirstOrDefault(p => p.Id == id);
        }

        public void AddPregunta(Pregunta pregunta)
        {
            _context.Preguntas.Add(pregunta);
        }

        public void UpdatePregunta(Pregunta pregunta)
        {
            _context.Update(pregunta);
        }

        public void RemovePregunta(Pregunta pregunta)
        {
            _context.Preguntas.Remove(pregunta);
        }
    }
}
