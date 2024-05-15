using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyStash.Areas.Customer.Controllers
{
    public class CardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public ShoppingCardVM ShoppingCardVM { get; set; }
        public CardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }


        [Area("Customer")]
        [Authorize]
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity) User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;


            ShoppingCardVM = new()
            {
                ShoppingCardList = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == userId, IncludeProp: "Product")
            };

            foreach(var card in ShoppingCardVM.ShoppingCardList)
            {
                card.Price = GetPriceBasedOnQuantity(card);
                ShoppingCardVM.OrderTotal += (card.Price * card.Count);
            }

            return View(ShoppingCardVM);
        }

        private double GetPriceBasedOnQuantity(ShoppingCard shoppingCard)
        {
            if(shoppingCard.Count <= 50)
            {
                return shoppingCard.Product.Price;
            } else
            {
                if(shoppingCard.Count <= 100)
                {
                    return shoppingCard.Product.Price50;
                }else
                {
                    return shoppingCard.Product.Price100;
                }
            }
        }

    }
}
