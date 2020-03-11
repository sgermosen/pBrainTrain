using System.Collections.Generic;
using Jmo.Web.Data.Domain;

namespace Jmo.Web.Repositories
{
    public interface IPreguntaRepository : IGenericRepository<Pregunta>
    {
        IEnumerable<Pregunta> GetPreguntas();

        Pregunta GetPregunta(int id);

        void AddPregunta(Pregunta pregunta);

        void UpdatePregunta(Pregunta pregunta);

        void RemovePregunta(Pregunta pregunta);
    }
}
