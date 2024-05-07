using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Mvc;

namespace BulkyStash.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        public CategoryController(ICategoryRepository db)
        {
            _categoryRepository = db;
        }

        public IActionResult Index() // By default it is get request
        {
            List<Category> CategoryList = _categoryRepository.GetAll().ToList();
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
                _categoryRepository.Add(category); // Keeps the changes
                _categoryRepository.Save(); // Saves to db
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

            Category? category = _categoryRepository.Get(u => u.Id == id);

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
                _categoryRepository.Update(category);
                _categoryRepository.Save();
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

            Category? category = _categoryRepository.Get(u => u.Id == id );

            if (category == null)
            {
                return NotFound();
            }


            return View(category);
        }

        [HttpPost,ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            Category? category = _categoryRepository.Get(u => u.Id == id);
            if(category == null)
            {
                return NotFound();
            }
            _categoryRepository.Remove(category);
            _categoryRepository.Save();

            TempData["success"] = "Category deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
