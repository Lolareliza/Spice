using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using Stripe;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        [BindProperty]//instead of passing on all the objects, you bind the property
        public OrderDetailsCart detailCart { get; set; }

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            detailCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()//creating a new property of order header
            };

            detailCart.OrderHeader.OrderTotal = 0;//because we'll be calculating that when we need userid of the user that is logged in, to retrieve all the items in the shopcart the user has

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//retrieving the user id

            var cart = _db.ShoppingBag.Where(c => c.ApplicationUserId == claim.Value);//retrieving the shopping cart inside
            
            if (cart!=null)
            {
                detailCart.ListCart = cart.ToList();
            }

            foreach (var list in detailCart.ListCart)//Calculate the order total so far
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                detailCart.OrderHeader.OrderTotal += (list.MenuItem.Price * list.Count);
                list.MenuItem.Description = SD.ConvertToRawHtml(list.MenuItem.Description);//converts to raw html ad stores it in the menu item

                if (list.MenuItem.Description.Length>100)
                {
                    list.MenuItem.Description = list.MenuItem.Description.Substring(0, 99) + "...";
                }
            }
            detailCart.OrderHeader.OrderTotalOriginal = detailCart.OrderHeader.OrderTotal;

            if (HttpContext.Session.GetString(SD.ssCouponCode)!=null)
            {
                detailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                detailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailCart.OrderHeader.OrderTotalOriginal);
            }

            return View(detailCart);
        }



        public async Task<IActionResult> Summary()
        {
            detailCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()////creating a new property of order header OR just dclaring it in the method since it is a binding propert
            };

            detailCart.OrderHeader.OrderTotal = 0;//because we'll be calculating that when we need userid of the user that is logged in, to retrieve all the items in the shopcart the user has

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);//retrieving the user id
            ApplicationUser applicationUser = await _db.ApplicationUser.Where(c => c.Id == claim.Value).FirstOrDefaultAsync();

            var cart = _db.ShoppingBag.Where(c => c.ApplicationUserId == claim.Value);//retrieving the shopping cart inside

            if (cart != null)
            {
                detailCart.ListCart = cart.ToList();
            }

            foreach (var list in detailCart.ListCart)//Calculate the order total so far
            {
                list.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                detailCart.OrderHeader.OrderTotal += (list.MenuItem.Price * list.Count);

            }
            detailCart.OrderHeader.OrderTotalOriginal = detailCart.OrderHeader.OrderTotal;
            
            detailCart.OrderHeader.PickupName = applicationUser.Name;
            detailCart.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            detailCart.OrderHeader.PickUpTime = DateTime.Now;




            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                detailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailCart.OrderHeader.OrderTotalOriginal);
            }

            return View(detailCart);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPOST(string stripeToken)
        {
            /*PROCESS FLOW OF THE STRIPE TOKEN
             When the user enters the credit card details and clicks the submit button,
             All the details are posted on the stripe server directly and are not saved anyhere on our server,
             Using the card numbers and details, stripe generates a token and sends the token to the post action method in the controller,
             We will use the token in the controller and pass it to make the actual charge
             */
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);


            detailCart.ListCart = await _db.ShoppingBag.Where(c => c.ApplicationUserId == claim.Value).ToListAsync();//retrieving all of the shopping cart
           
            detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;//because we'll be calculating that when we need userid of the user that is logged in, to retrieve all the items in the shopcart the user has
            detailCart.OrderHeader.OrderDate = DateTime.Now;//because we'll be calculating that when we need userid of the user that is logged in, to retrieve all the items in the shopcart the user has
            detailCart.OrderHeader.UserId = claim.Value;//because we'll be calculating that when we need userid of the user that is logged in, to retrieve all the items in the shopcart the user has
            detailCart.OrderHeader.Status = SD.PaymentStatusPending;//because we'll be calculating that when we need userid of the user that is logged in, to retrieve all the items in the shopcart the user has
            detailCart.OrderHeader.PickUpTime = Convert.ToDateTime(detailCart.OrderHeader.PickUpDate.ToShortDateString()+" "+detailCart.OrderHeader.PickUpTime.ToShortTimeString());//because we'll be calculating that when we need userid of the user that is logged in, to retrieve all the items in the shopcart the user has

            List<OrderDetails> orderDetailsList = new List<OrderDetails>();
            _db.OrderHeader.Add(detailCart.OrderHeader);
            await _db.SaveChangesAsync();

            var details = detailCart.OrderHeader.OrderTotalOriginal ;

            foreach (var item in detailCart.ListCart)//Calculate the order total so far
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                //Creating order details
                OrderDetails orderDetails = new OrderDetails
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = detailCart.OrderHeader.Id,
                    Description = item.MenuItem.Description,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                details = orderDetails.Count * orderDetails.Price;
                _db.OrderDetails.Add(orderDetails);
                await _db.SaveChangesAsync();
            }
            


            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                detailCart.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);
                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == detailCart.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();
                detailCart.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, detailCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                detailCart.OrderHeader.OrderTotal = details;
            }
            detailCart.OrderHeader.CouponCodeDiscount = details - detailCart.OrderHeader.OrderTotal;

            _db.ShoppingBag.RemoveRange(detailCart.ListCart);
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0);
            await _db.SaveChangesAsync();

            //making the call (transaction) to stripe for getting our payment
            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(detailCart.OrderHeader.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID : " + detailCart.OrderHeader.Id,
                Source = stripeToken
            };
            var service = new ChargeService();
            Charge charge = service.Create(options);//This will do the actual transaction
            //check if there is a transaction ID
            if (charge.BalanceTransactionId==null)
            {
                detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            else
            {
                detailCart.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status.ToLower()=="succeeded")
            {
                detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                detailCart.OrderHeader.Status = SD.StatusSubmitted;
            }
            else
            {
                detailCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            await _db.SaveChangesAsync();
            //return RedirectToAction("Index", "Home");
            return RedirectToAction("Confirm", "Order", new { id = detailCart.OrderHeader.Id });//with a confirm action and a order header id
        }

        public IActionResult AddCoupon()
        {
            if (detailCart.OrderHeader.CouponCode == null)
            {
                detailCart.OrderHeader.CouponCode = "";
            }
            HttpContext.Session.SetString(SD.ssCouponCode, detailCart.OrderHeader.CouponCode);

            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
           
            HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {

            var cart = await _db.ShoppingBag.FirstOrDefaultAsync(c => c.Id == cartId);
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {

            var cart = await _db.ShoppingBag.FirstOrDefaultAsync(c => c.Id == cartId);
            if (cart.Count==1)
            {
                //remove it from the cart
                _db.ShoppingBag.Remove(cart);
                await _db.SaveChangesAsync();

                //remove it from the session
                var cnt = _db.ShoppingBag.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Trash(int cartId)
        {

            var cart = await _db.ShoppingBag.FirstOrDefaultAsync(c => c.Id == cartId);
            //remove it from the cart
            _db.ShoppingBag.Remove(cart);
            await _db.SaveChangesAsync();

            //remove it from the session
            var cnt = _db.ShoppingBag.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);
            
            return RedirectToAction(nameof(Index));
        }

        
    }
}
