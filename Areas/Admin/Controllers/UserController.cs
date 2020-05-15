using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
   
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            //ClaimsIdentity is a concrete implementation of a claims-based identity; 
            //that is, an identity described by a collection of claims.
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //here we are fetching the id of the person that has logged in, and we are passing all of the application users except the logged in user
            return View(await _db.ApplicationUser.Where(u=>u.Id != claim.Value).ToListAsync());
        }

        public async Task<IActionResult> Lock(string id)
        {
            if (id==null)
            {
                return NotFound();
            }
            //this will return the first object of the id found, and if it can't find it, it will return null
            var applicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(m => m.Id == id);

            if (applicationUser == null)
            {
                return NotFound();
            }
            //it will bring out the function to lock out the person
            applicationUser.LockoutEnd = DateTime.Now.AddYears(1000);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UnLock(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            //this will return the first object of the id found, and if it can't find it, it will return null
            var applicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(m => m.Id == id);

            if (applicationUser == null)
            {
                return NotFound();
            }
            //it will bring out the function to lock out the person
            applicationUser.LockoutEnd = DateTime.Now;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}