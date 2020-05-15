using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        public OrderController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //consuming the order details view model
            OrderDetailsViewModel orderDetailsViewModel = new OrderDetailsViewModel()
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(c => c.Id == id && c.UserId == claim.Value),
                //Should be only able to see the order that is for them

                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
                //populate the order details, id is the details , while the orderid is the order header id
            };
            return View(orderDetailsViewModel);
        }
        public IActionResult Index()
        {
            return View();
        }

[Authorize]
        public async Task<IActionResult> OrderHistory()
        {

            //find the user id
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            List<OrderDetailsViewModel> orderList = new List<OrderDetailsViewModel>();
            List<OrderHeader> OrderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser).Where(u => u.UserId == claim.Value).ToListAsync();


            //iterate through all the order headers
            foreach (OrderHeader item in OrderHeaderList)
            {
                OrderDetailsViewModel individual = new OrderDetailsViewModel
                {
                    OrderHeader = item,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == item.Id).ToListAsync(),
                };
                orderList.Add(individual);

            }
            //for each of the order, we'll create a list of the order detailsViewMode and add it to the Orderlist

            return View(orderList);
        }
    }
}