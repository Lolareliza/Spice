using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task< IActionResult> Index()
        {
            return View(await _db.Coupon.ToListAsync());
        }

        //GET - CREATE
        public IActionResult Create()
        {
            return View();
        }

        //POST - CREATE

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupons)
        {
            if (ModelState.IsValid)
            {
                //fetch the file that was uploaded for the image
                var files = HttpContext.Request.Form.Files;
                if (files.Count>0)
                {
                    //if true, we convert it into a stream of byte to store it into a DB
                    byte[] p1 = null;
                    using(var fs1 = files[0].OpenReadStream())//This will start creating the file
                    {
                        //This is for memory stream
                        using(var ms1 = new MemoryStream())
                        {
                            //This will convert our image into a stream of bytes and store it into p1 (Here we convert our image to a byte array)
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();
                           
                        }
                        //Then we just need to add it to our pictures inside the DB
                        coupons.Picture = p1;
                    }
                    _db.Coupon.Add(coupons);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(coupons);
        }

        //GET - EDIT

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _db.Coupon.SingleOrDefaultAsync(m => m.Id == id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }


        //POST - EDIT
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(Coupon coupons)
        {
            if (coupons.Id == 0)
            {
                return NotFound();
            }

            var couponFromDb = await _db.Coupon.Where(c => c.Id == coupons.Id).FirstOrDefaultAsync();
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                if (files.Count > 0)
                {
                    //if true, we convert it into a stream of byte to store it into a DB
                    byte[] p1 = null;
                    using (var fs1 = files[0].OpenReadStream())//This will start creating the file
                    {
                        //This is for memory stream
                        using (var ms1 = new MemoryStream())
                        {
                            //This will convert our image into a stream of bytes and store it into p1 (Here we convert our image to a byte array)
                            fs1.CopyTo(ms1);
                            p1 = ms1.ToArray();

                        }
                       
                    }
                    //Then we just need to add it to our pictures inside the DB
                    couponFromDb.Picture = p1;

                }
                couponFromDb.Name = coupons.Name;
                couponFromDb.CouponType = coupons.CouponType;
                couponFromDb.MinimumAmount = coupons.MinimumAmount;
                couponFromDb.Discount = coupons.Discount;
                couponFromDb.IsActive = coupons.IsActive;

                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(coupons);



        }


        //GET - DETAILS

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _db.Coupon.SingleOrDefaultAsync(m => m.Id == id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }


        //GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var coupon = await _db.Coupon.SingleOrDefaultAsync(m => m.Id == id);
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }


        //POST - DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            var coupon = await _db.Coupon.FindAsync(id);
            if (coupon == null)
            {
                return View();
            }
            _db.Coupon.Remove(coupon);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}