using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class MenuItemController : Controller
    {

        private readonly ApplicationDbContext _db; //this is the local object

        private readonly IWebHostEnvironment _hostingEnvironment;

        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }
        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostingEnvironment)//this is the object retrieved from the container using DI
        {
            _db = db;
            _hostingEnvironment = hostingEnvironment;
            MenuItemVM = new MenuItemViewModel()
            {
                Category = _db.Category,
                MenuItem = new Models.MenuItem()
            };
        }
        public async Task<IActionResult> Index()
        {
            //We'll retrieve all of the menuitems from the db and pass them to the view
            var menuItems = await _db.MenuItem.Include(m=>m.Category).Include(m=>m.SubCategory).ToListAsync();
            return View(menuItems);
        }

        //GET - CREATE
        public IActionResult Create()
        {
            return View(MenuItemVM);
        }


        //POST - CREATE
        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePOST()
        {
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());
            //We are assigning the subcategory id here so that whenever we need it, it will be there because it does not have a value in the view model. It was populated with JS
            if (!ModelState.IsValid)
            {
                return View(MenuItemVM);
            }
            _db.MenuItem.Add(MenuItemVM.MenuItem);
            await _db.SaveChangesAsync();

            //Work on the image saving section (the name of the image should b unique, best is to name the image based on the id)
            //We'll extract the root path of the application, we'll need the hostingEnvironment

            string webRootPath = _hostingEnvironment.WebRootPath;

            // extract all the files of the image that the user has uploaded
            var files = HttpContext.Request.Form.Files;

            // extract the menuitem from DB, whatever has been saved
            var menuItemFromDb = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);

            if (files.Count>0)
            {
                // file has been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                //the id and extension will be the name of the image we are renaming to
                var extension = Path.GetExtension(files[0].FileName);

            //it will copy the file to a location on the server and rename it
            using (var fileStream= new FileStream(Path.Combine(uploads, MenuItemVM.MenuItem.Id + extension), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);//uploading only one
                }
                //after the image has been uploaded, this is to make sure that inside d DB we change the image column with the location where image is saved.
                menuItemFromDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + extension; 
            }
            else
            {
                // no file was uploaded, so use default image
                var uploads = Path.Combine(webRootPath, @"images\" + SD.DefaultFoodImage);

                //make a copy of the file from our folder, copy from uploads in the wwwoute path, even though it is the real path it needs to be mentioned, 
                //because in real time it might not be seen and you might decide to choose a different location 
                System.IO.File.Copy(uploads, webRootPath + @"\images\" + MenuItemVM.MenuItem.Id + ".jpg");

                //update the entry in database
                menuItemFromDb.Image = @"\images" + MenuItemVM.MenuItem.Id + ".jpg";
            }
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        //GET - EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id==null)
            {
                return NotFound();
            }
            //loading the menuitem
            MenuItemVM.MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);

            //loading the subcategory, based on the menu item category id, to load it for the very first time
            MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem==null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPOST(int? id)
        {
            if (id==null)
            {
                return View(MenuItemVM);
            }
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());
            //We are retrieving the subcategory id here because we are updating it using JS
            if (!ModelState.IsValid)
            {
                // to populate the subcategory id before returning the view
                MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();
                return View(MenuItemVM);
            }

            //Work on the image saving section (the name of the image should b unique, best is to name the image based on the id)
            //We'll extract the root path of the application, we'll need the hostingEnvironment

            string webRootPath = _hostingEnvironment.WebRootPath;

            // extract all the files of the image that the user has uploaded
            var files = HttpContext.Request.Form.Files;

            // extract the menuitem from DB, whatever has been saved
            var menuItemFromDb = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);

            if (files.Count > 0)
            {
                // New image has been uploaded
                var uploads = Path.Combine(webRootPath, "images");
                //the id and extension will be the name of the image we are renaming to
                var extension_new = Path.GetExtension(files[0].FileName);


                //Before it replaces it has to delete the original(trimming because there is already a \\  before the image)
                var imagePath = Path.Combine(webRootPath, menuItemFromDb.Image.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                //we will upload the new file
                using (var fileStream = new FileStream(Path.Combine(uploads, MenuItemVM.MenuItem.Id + extension_new), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);//uploading only one
                }
                //after the image has been uploaded, this is to make sure that inside d DB we change the image column with the location where image is saved.
                menuItemFromDb.Image = @"\images\" + MenuItemVM.MenuItem.Id + extension_new;
            }

            //to change the text, it will be fetched from the VM, whatever the user has changed and will push it to the db
            menuItemFromDb.Name = MenuItemVM.MenuItem.Name;
            menuItemFromDb.Description = MenuItemVM.MenuItem.Description;
            menuItemFromDb.Price = MenuItemVM.MenuItem.Price;
            menuItemFromDb.Spicyness = MenuItemVM.MenuItem.Spicyness;
            menuItemFromDb.CategoryId = MenuItemVM.MenuItem.CategoryId;
            menuItemFromDb.SubCategoryId = MenuItemVM.MenuItem.SubCategoryId;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET - DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            //loading the menuitem
            MenuItemVM.MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);

            //loading the subcategory, based on the menu item category id, to load it for the very first time
            MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }


        //GET - DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            //loading the menuitem
            MenuItemVM.MenuItem = await _db.MenuItem.Include(m => m.Category).Include(m => m.SubCategory).SingleOrDefaultAsync(m => m.Id == id);

            //loading the subcategory, based on the menu item category id, to load it for the very first time
            MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem == null)
            {
                return NotFound();
            }
            return View(MenuItemVM);
        }

        //POST - DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)//two parameters and the definition cannot be the same, we have to change the method name
        {

            if (id == null)
            {
                return View(MenuItemVM);
            }
            string webRootPath = _hostingEnvironment.WebRootPath;
            MenuItem menuItem = await _db.MenuItem.FindAsync(id);

            if (menuItem != null)
            {
                var imagePath = Path.Combine(webRootPath, menuItem.Image.TrimStart('\\'));
                if (System.IO.File.Exists(imagePath))//delete the image first
                {
                    System.IO.File.Delete(imagePath);
                }
                _db.MenuItem.Remove(menuItem);//then delete the data
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));

        }
    }
}