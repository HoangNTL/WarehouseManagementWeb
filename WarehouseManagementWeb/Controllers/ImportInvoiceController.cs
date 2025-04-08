using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WarehouseManagementWeb.Models;

namespace WarehouseManagementWeb.Controllers
{
    public class ImportInvoiceController : Controller
    {
        DBDataContext db = new DBDataContext();
        // GET: ImportInvoice
        public ActionResult Index()
        {
            var importInvoices = db.ImportInvoices.ToList();
            return View(importInvoices);
        }
    }
}