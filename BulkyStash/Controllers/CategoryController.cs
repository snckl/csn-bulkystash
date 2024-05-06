using BulkyStash.Data;
using BulkyStash.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyStash.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index() // By default it is get request
        {
            List<Category> CategoryList = _db.Categories.ToList();
            return View(CategoryList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            //if(category.Name == category.DisplayOrder.ToString())
            //{
            //    ModelState.AddModelError("Name","Display Order and Name cannot be same.");
            //}
            if (ModelState.IsValid)
            { 
                _db.Categories.Add(category); // Keeps the changes
                _db.SaveChanges(); // Saves to db
                TempData["success"] = "Category created successfully.";
                return RedirectToAction("Index"); // Redirects to action.If different controller type it after coma
            }
            return View();
        }

        public IActionResult Edit(int? id)
        {
            if(id == null || id == 0)
            {
                return NotFound();
            } 

            Category? category = _db.Categories.Find(id.Value);

            if(category == null)
            {
                return NotFound();
            }


            return View(category);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _db.Categories.Update(category); 
                _db.SaveChanges();
                TempData["success"] = "Category updated successfully.";
                return RedirectToAction("Index"); // Redirects to action.If different controller type it after coma
            }
            return View();
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? category = _db.Categories.Find(id.Value);

            if (category == null)
            {
                return NotFound();
            }


            return View(category);
        }

        [HttpPost,ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            Category? category = _db.Categories.Find(id);
            if(category == null)
            {
                return NotFound();
            }
            _db.Categories.Remove(category);
            _db.SaveChanges();

            TempData["success"] = "Category deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
