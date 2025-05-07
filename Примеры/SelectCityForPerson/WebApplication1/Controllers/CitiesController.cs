using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    public class CitiesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CitiesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(City city)
        {
            if (ModelState.IsValid)
            {
                _db.Cities.Add(city);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View();
        }
    }
}
