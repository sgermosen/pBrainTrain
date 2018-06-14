using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using pBrainTrain.Backend.Models;
using pBrainTrain.Domain;
using pBrainTrain.Backend.Helpers;

namespace pBrainTrain.Backend.Controllers
{
    [Authorize(Roles = "Admin,Moderator")]
    public class QuestionsController : Controller
    {
        private LocalDataContext db = new LocalDataContext();

        #region Answers
        public async Task<ActionResult> AnswerDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Answer answer = await db.Answers.FindAsync(id);
            if (answer == null)
            {
                return HttpNotFound();
            }
            return View(answer);
        }

        // GET: Answers/Create
        public async Task<ActionResult> AnswerCreate(int id)
        {
            var aView = new AnswerView { QuestionId = id };
            return View(aView);
        }

    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AnswerCreate(AnswerView view)
        {
            if (ModelState.IsValid)
            {
                var pic = string.Empty;
                const string folder = "~/Content/GamePics";

                if (view.ImageFile != null)
                {
                    pic = Files.UploadPhoto(view.ImageFile, folder, "");
                    pic = string.Format("{0}/{1}", folder, pic);
                }

                var answer = new Answer
                {
                    Name = view.Name,
                    QuestionId = view.QuestionId,
                    IsTheAnswer=view.IsTheAnswer,
                    Picture = pic
                };
                db.Answers.Add(answer);
                await db.SaveChangesAsync();
                return RedirectToAction(String.Format("Details/{0}",view.QuestionId));
            }

            ViewBag.QuestionId = new SelectList(db.Questions, "QuestionId", "QuestionName", view.QuestionId);
            return View(view);
        }

        // GET: Answers/Edit/5
        public async Task<ActionResult> AnswerEdit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Answer answer = await db.Answers.FindAsync(id);
            if (answer == null)
            {
                return HttpNotFound();
            }
            ViewBag.QuestionId = new SelectList(db.Questions, "QuestionId", "QuestionName", answer.QuestionId);
            return View(answer);
        }

        // POST: Answers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AnswerEdit([Bind(Include = "AnswerId,Name,QuestionId")] Answer answer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(answer).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.QuestionId = new SelectList(db.Questions, "QuestionId", "QuestionName", answer.QuestionId);
            return View(answer);
        }

        // GET: Answers/Delete/5
        public async Task<ActionResult> AnswerDelete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Answer answer = await db.Answers.FindAsync(id);
            if (answer == null)
            {
                return HttpNotFound();
            }
            return View(answer);
        }

        // POST: Answers/Delete/5
        [HttpPost, ActionName("AnswerDelete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AnswerDeleteConfirmed(int id)
        {
            Answer answer = await db.Answers.FindAsync(id);
            db.Answers.Remove(answer);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        #endregion


        #region QuestionsRegion
        // GET: Questions
        public async Task<ActionResult> Index()
        {
            var questions = db.Questions.Include(q => q.Category);
            return View(await questions.ToListAsync());
        }

        // GET: Questions/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Question question = await db.Questions.FindAsync(id);
            if (question == null)
            {
                return HttpNotFound();
            }
            return View(question);
        }

        // GET: Questions/Create
        public ActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "Name");
            var question = new QuestionView();
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(QuestionView view)
        {
            if (ModelState.IsValid)
            {
                var pic = string.Empty;
                const string folder = "~/Content/GamePics";

                if (view.ImageFile != null)
                {
                    pic = Files.UploadPhoto(view.ImageFile, folder, "");
                    pic = string.Format("{0}/{1}", folder, pic);
                }

                var question = new Question
                {
                    CategoryId = view.CategoryId,
                    QuestionName = view.QuestionName
                };

                question.Picture = pic;

                db.Questions.Add(question);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "Name", view.CategoryId);
            return View(view);
        }

        // GET: Questions/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Question question = await db.Questions.FindAsync(id);
            if (question == null)
            {
                return HttpNotFound();
            }
            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "Name", question.CategoryId);
            return View(question);
        }

        // POST: Questions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "QuestionId,QuestionName,CategoryId,Picture")] Question question)
        {
            if (ModelState.IsValid)
            {
                db.Entry(question).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewBag.CategoryId = new SelectList(db.Categories, "CategoryId", "Name", question.CategoryId);
            return View(question);
        }

        // GET: Questions/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Question question = await db.Questions.FindAsync(id);
            if (question == null)
            {
                return HttpNotFound();
            }
            return View(question);
        }

        // POST: Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Question question = await db.Questions.FindAsync(id);
            db.Questions.Remove(question);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        #endregion



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
