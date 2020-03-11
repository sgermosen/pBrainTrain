using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using API.Models;
using Domain;

namespace API.Controllers
{
    public class TournamentTeamsController : ApiController
    {
        private DataContext db = new DataContext();

        // GET: api/TournamentTeams
        public IQueryable<TournamentTeam> GetTournamentTeams()
        {
            return db.TournamentTeams;
        }

        // GET: api/TournamentTeams/5
        [ResponseType(typeof(TournamentTeam))]
        public async Task<IHttpActionResult> GetTournamentTeam(int id)
        {
            var tournamentTeams = await db.TournamentTeams.Where(tt => tt.TournamentGroupId==id).ToListAsync();
            //if (tournamentTeam == null)
            //{
            //    return NotFound();
            //}
            var list = new List<TournamentTeamRespose>();
            foreach (var tournamentTeam in tournamentTeams.OrderBy(tt=>tt.Position))
            {
                list.Add(new TournamentTeamRespose
                { AgainstGoals = tournamentTeam.AgainstGoals,
                    FavorGoals = tournamentTeam.FavorGoals,
                    MatchesLost = tournamentTeam.MatchesLost,
                    MatchesPlayed = tournamentTeam.MatchesPlayed,
                    MatchesTied = tournamentTeam.MatchesTied,
                    MatchesWon= tournamentTeam.MatchesWon,
                    Points = tournamentTeam.Points,
                    Position = tournamentTeam.Position,
                    Team = tournamentTeam.Team,
                    TeamId = tournamentTeam.TeamId,
                    TournamentGroupId = tournamentTeam.TournamentGroupId,
                 TournamentTeamId = tournamentTeam.TournamentTeamId,
                });
            }
            return Ok(list);
        }

        // PUT: api/TournamentTeams/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutTournamentTeam(int id, TournamentTeam tournamentTeam)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tournamentTeam.TournamentTeamId)
            {
                return BadRequest();
            }

            db.Entry(tournamentTeam).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TournamentTeamExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/TournamentTeams
        [ResponseType(typeof(TournamentTeam))]
        public async Task<IHttpActionResult> PostTournamentTeam(TournamentTeam tournamentTeam)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.TournamentTeams.Add(tournamentTeam);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = tournamentTeam.TournamentTeamId }, tournamentTeam);
        }

        // DELETE: api/TournamentTeams/5
        [ResponseType(typeof(TournamentTeam))]
        public async Task<IHttpActionResult> DeleteTournamentTeam(int id)
        {
            TournamentTeam tournamentTeam = await db.TournamentTeams.FindAsync(id);
            if (tournamentTeam == null)
            {
                return NotFound();
            }

            db.TournamentTeams.Remove(tournamentTeam);
            await db.SaveChangesAsync();

            return Ok(tournamentTeam);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool TournamentTeamExists(int id)
        {
            return db.TournamentTeams.Count(e => e.TournamentTeamId == id) > 0;
        }
    }
}