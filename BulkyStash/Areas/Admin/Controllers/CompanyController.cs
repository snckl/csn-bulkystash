// Ignore Spelling: Admin

using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyStash.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index() // By default it is get request
        {
            List<Company> CompanyList = _unitOfWork.Company.GetAll().ToList();
            return View(CompanyList);
        }

        public IActionResult Upsert(int? id)
        {

     

            if(id == null || id == 0)
            {
                // Create
                return View(new Company());
            } else
            {
                // Update
                Company company = _unitOfWork.Company.Get(u => u.Id == id);
                return View(company);
            }
           
        }

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
           

                if(company.Id == 0)
                {
                    _unitOfWork.Company.Add(company); // Keeps the changes
                }
                else
                {
                    _unitOfWork.Company.Update(company);
                }
                
                _unitOfWork.Save(); // Saves to db
                TempData["success"] = "Company created successfully.";
                return RedirectToAction("Index"); // Redirects to action.If different controller type it after coma
            } else
            {

                return View(company);
            }
           
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> Companys = _unitOfWork.Company.GetAll().ToList();
            return Json(new {data = Companys});
        }


        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var CompanyToBeDeleted = _unitOfWork.Company.Get(u => u.Id == id);
            if (CompanyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unitOfWork.Company.Remove(CompanyToBeDeleted);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Delete Successful" });
        }

        #endregion

    }
}
