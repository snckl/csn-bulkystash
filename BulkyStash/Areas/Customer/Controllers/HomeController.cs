using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyStash.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger,IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unitOfWork.Product.GetAll(IncludeProp:"Category");

            return View(productList);
        }

        public IActionResult Details(int id)
        {
            ShoppingCard card = new()
            {
                Id = 0,
                Product = _unitOfWork.Product.Get(u => u.Id == id, IncludeProp: "Category"),
                Count = 1,
                ProductId =  id
            };
            return View(card);
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCard card)
        {
            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            card.ApplicationUserId = userId;

            ShoppingCard cardFromDb = _unitOfWork.ShoppingCard.Get(u =>u.ApplicationUserId == userId && u.ProductId == card.ProductId);

            if(cardFromDb != null)
            {
                cardFromDb.Count += card.Count;
                _unitOfWork.ShoppingCard.Update(cardFromDb);
            } else
            {
                _unitOfWork.ShoppingCard.Add(card);
                _unitOfWork.Save();
            }


            
            

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
