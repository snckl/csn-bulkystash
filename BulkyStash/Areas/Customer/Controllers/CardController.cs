using Azure;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyStash.Areas.Customer.Controllers
{
	[Area("Customer")]
	[Authorize]
	public class CardController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

		[BindProperty]
		public ShoppingCardVM ShoppingCardVM { get; set; }

		public CardController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCardVM = new()
			{
				ShoppingCardList = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == userId, IncludeProp: "Product"),
				OrderHeader = new()
			};

			foreach (var card in ShoppingCardVM.ShoppingCardList)
			{
				card.Price = GetPriceBasedOnQuantity(card);
				ShoppingCardVM.OrderHeader.OrderTotal += (card.Price * card.Count);
			}

			return View(ShoppingCardVM);
		}

		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCardVM = new()
			{
				ShoppingCardList = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == userId, IncludeProp: "Product"),
				OrderHeader = new()
			};

			ShoppingCardVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
			ShoppingCardVM.OrderHeader.Name = ShoppingCardVM.OrderHeader.ApplicationUser.Name;
			ShoppingCardVM.OrderHeader.PhoneNumber = ShoppingCardVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCardVM.OrderHeader.StreetAddress = ShoppingCardVM.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCardVM.OrderHeader.City = ShoppingCardVM.OrderHeader.ApplicationUser.City;
			ShoppingCardVM.OrderHeader.State = ShoppingCardVM.OrderHeader.ApplicationUser.State;
			ShoppingCardVM.OrderHeader.PostalCode = ShoppingCardVM.OrderHeader.ApplicationUser.PostalCode;

			foreach (var card in ShoppingCardVM.ShoppingCardList)
			{
				card.Price = GetPriceBasedOnQuantity(card);
				ShoppingCardVM.OrderHeader.OrderTotal += (card.Price * card.Count);
			}
			return View(ShoppingCardVM);
		}

		[HttpPost]
		[ActionName("Summary")]
		public IActionResult SummaryPOST()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCardVM.ShoppingCardList = _unitOfWork.ShoppingCard.GetAll(u => u.ApplicationUserId == userId, IncludeProp: "Product");
			ShoppingCardVM.OrderHeader.OrderDate = System.DateTime.Now;
			ShoppingCardVM.OrderHeader.ApplicationUserId = userId;

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

			foreach (var card in ShoppingCardVM.ShoppingCardList)
			{
				card.Price = GetPriceBasedOnQuantity(card);
				ShoppingCardVM.OrderHeader.OrderTotal += (card.Price * card.Count);
			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				// it is a regular account.
				ShoppingCardVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCardVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
			{
				// company user
				ShoppingCardVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCardVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
			_unitOfWork.OrderHeader.Add(ShoppingCardVM.OrderHeader);
			_unitOfWork.Save();

			foreach (var card in ShoppingCardVM.ShoppingCardList)
			{
				OrderDetail orderDetail = new()
				{
					ProductId = card.ProductId,
					OrderHeaderId = ShoppingCardVM.OrderHeader.Id,
					Price = card.Price,
					Count = card.Count,
				};
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				var domain = "https://localhost:7253/";

				var options = new Stripe.Checkout.SessionCreateOptions
				{
					SuccessUrl = domain + $"customer/card/OrderConfirmation?id={ShoppingCardVM.OrderHeader.Id}",
					CancelUrl = domain + "customer/card/index",
					LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
					Mode = "payment",
				};

				foreach (var item in ShoppingCardVM.ShoppingCardList)
				{
					var sessionLineItem = new Stripe.Checkout.SessionLineItemOptions
					{
						PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100),
							Currency = "usd",
							ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							}
						},
						Quantity = item.Count
					};
					options.LineItems.Add(sessionLineItem);
				}

				var service = new Stripe.Checkout.SessionService();
				Session session = service.Create(options);
				_unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCardVM.OrderHeader.Id,session.Id,session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);


			}

			return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCardVM.OrderHeader.Id });
		}

		public IActionResult OrderConfirmation(int id)
		{
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id,IncludeProp:"ApplicationUser");
			if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				// this is an order by a customer
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if(session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(id,SD.StatusApproved,SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}

			List<ShoppingCard> shoppingCards = _unitOfWork.ShoppingCard
				.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

			_unitOfWork.ShoppingCard.RemoveRange(shoppingCards);
			_unitOfWork.Save();


			return View(id);
		}

		public IActionResult Plus(int cardId)
		{
			var cardFromDb = _unitOfWork.ShoppingCard.Get(u => u.Id == cardId);
			cardFromDb.Count += 1;
			_unitOfWork.ShoppingCard.Update(cardFromDb);
			_unitOfWork.Save();

			return RedirectToAction(nameof(Index));
		}

		public IActionResult Minus(int cardId)
		{
			var cardFromDb = _unitOfWork.ShoppingCard.Get(u => u.Id == cardId);
			if (cardFromDb.Count <= 1)
			{
				_unitOfWork.ShoppingCard.Remove(cardFromDb);
			}
			else
			{
				cardFromDb.Count -= 1;
				_unitOfWork.ShoppingCard.Update(cardFromDb);
			}
			_unitOfWork.Save();

			return RedirectToAction(nameof(Index));
		}

		public IActionResult Remove(int cardId)
		{
			var cardFromDb = _unitOfWork.ShoppingCard.Get(u => u.Id == cardId);
			_unitOfWork.ShoppingCard.Remove(cardFromDb);
			_unitOfWork.Save();

			return RedirectToAction(nameof(Index));
		}

		private double GetPriceBasedOnQuantity(ShoppingCard shoppingCard)
		{
			if (shoppingCard.Count <= 50)
			{
				return shoppingCard.Product.Price;
			}
			else
			{
				if (shoppingCard.Count <= 100)
				{
					return shoppingCard.Product.Price50;
				}
				else
				{
					return shoppingCard.Product.Price100;
				}
			}
		}
	}
}