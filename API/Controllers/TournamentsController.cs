using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using API.Models;
using Domain;

namespace API.Controllers
{
    [RoutePrefix("api/Tournaments")]
    public class TournamentsController : ApiController
    {
        private DataContextLocal db = new DataContextLocal();

        [Route("GetMatchesToPredict/{tournamentId}/{userId}")]
        public async Task<IHttpActionResult> GetMatchesToPredict(int tournamentId, int userId)
        {
            //usando linq para hacer 
            var qry = await (from t in db.Tournaments
                join d in db.Dates on t.TournamentId equals d.TournamentId
                join m in db.Matches on d.DateId equals m.DateId
                where t.TournamentId == tournamentId && m.StatusId != 3 && m.DateTime > DateTime.Now
                select new { m }).ToListAsync();

            var predictions = await db.Predictions.Where(p => p.UserId == userId).ToListAsync();
            var matches = new List<MatchResponse>();

            foreach (var item in qry)
            {
                var matchResponse = new MatchResponse
                {
                    DateId = item.m.DateId,
                    DateTime = item.m.DateTime,
                    Local = item.m.Local,
                    LocalGoals = item.m.LocalGoals,
                    LocalId = item.m.LocalId,
                    MatchId = item.m.MatchId,
                    StatusId = item.m.StatusId,
                    TournamentGroupId = item.m.TournamentGroupId,
                    Visitor = item.m.Visitor,
                    VisitorGoals = item.m.VisitorGoals,
                    VisitorId = item.m.VisitorId,
                };

                var prediction = predictions.Where(p => p.MatchId == item.m.MatchId).FirstOrDefault();

                if (prediction != null)
                {
                    matchResponse.LocalGoals = prediction.LocalGoals;
                    matchResponse.VisitorGoals = prediction.VisitorGoals;
                    matchResponse.WasPredicted = true;
                }
                //else //el boleano se asume en false
                //{
                //    matchResponse.WasPredicted = false;
                //}

                matches.Add(matchResponse);
            }

            return Ok(matches.OrderBy(m => m.DateTime));


        }

        // GET: api/Tournaments
        public async Task<IHttpActionResult> GetTournaments()
        {
            var tournaments = await db.Tournaments.Where(t => t.IsActive).OrderBy(t => t.Order).ToListAsync();
            var list = new List<TournamentResponse>();
            foreach (var tournament in tournaments)
            {
                list.Add(new TournamentResponse
                {
                    Dates=tournament.Dates.ToList(),
                    Groups=tournament.Groups.ToList(),
                    Logo= tournament.Logo,
                    Name= tournament.Name,
                    TournamentId= tournament.TournamentId
                });
            }

            return Ok(list);
        }

        // GET: api/Tournaments/5
        [ResponseType(typeof(Tournament))]
        public async Task<IHttpActionResult> GetTournament(int id)
        {
            Tournament tournament = await db.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            return Ok(tournament);
        }

        // PUT: api/Tournaments/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutTournament(int id, Tournament tournament)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tournament.TournamentId)
            {
                return BadRequest();
            }

            db.Entry(tournament).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TournamentExists(id))
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

        // POST: api/Tournaments
        [ResponseType(typeof(Tournament))]
        public async Task<IHttpActionResult> PostTournament(Tournament tournament)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Tournaments.Add(tournament);
            await db.SaveChangesAsync();

            return CreatedAtRoute("DefaultApi", new { id = tournament.TournamentId }, tournament);
        }

        // DELETE: api/Tournaments/5
        [ResponseType(typeof(Tournament))]
        public async Task<IHttpActionResult> DeleteTournament(int id)
        {
            Tournament tournament = await db.Tournaments.FindAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }

            db.Tournaments.Remove(tournament);
            await db.SaveChangesAsync();

            return Ok(tournament);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool TournamentExists(int id)
        {
            return db.Tournaments.Count(e => e.TournamentId == id) > 0;
        }
    }
}