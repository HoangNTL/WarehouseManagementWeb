using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using WarehouseManagementWeb.Models;

namespace WarehouseManagementWeb.Controllers
{
    public class ProductController : Controller
    {
        // GET: Product
        public ActionResult Index()
        {
            using (var db = new DBDataContext())
            {
                var product = db.Products.ToList();
                return View(product);
            }
        }

        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Create(Product model)
        {
            if (ModelState.IsValid)
            {
                using (var db = new DBDataContext())
                {
                    var newProduct = new Product
                    {
                        Name = model.Name,
                        Code = model.Code,
                        Description = model.Description,
                        Unit = model.Unit,
                        Price = model.Price,
                        Quantity = model.Quantity,
                        CreatedAt = DateTime.Now
                    };
                    db.Products.InsertOnSubmit(model);
                    db.SubmitChanges();

                    return Json(new { success = true, message = "Product created successfully!" });
                }
            }

            return Json(new { success = false, message = "Invalid data!" });
        }

        [HttpGet]
        public JsonResult GetProductsAjax()
        {
            using (var db = new DBDataContext())
            {
                var products = db.Products
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Code = p.Code,
                        Unit = p.Unit,
                        Price = p.Price,
                        Quantity = p.Quantity
                    })
                    .ToList();

                return Json(products, JsonRequestBehavior.AllowGet);
            }

        }
    }
}