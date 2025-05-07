using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    public class PersonsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PersonsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Cities = new SelectList(_db.Cities, "Id", "Name");
            return View();
        }

        [HttpPost]
        public ActionResult Create(Person person)
        {
            if (ModelState.IsValid)
            {
                _db.Persons.Add(person);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Cities = new SelectList(_db.Cities, "Id", "Name");
            return View(person);
        }
    }
}
