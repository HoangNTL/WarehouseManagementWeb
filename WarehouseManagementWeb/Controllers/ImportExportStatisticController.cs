using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WarehouseManagementWeb.Models;

namespace WarehouseManagementWeb.Controllers
{
    public class ImportExportStatisticController : Controller
    {
        // GET: ImportExportStatistic
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetImportExportStatistic(string type, int? month, int? year)
        {
            var now = DateTime.Now;
            int selectedMonth = month ?? now.Month;
            int selectedYear = year ?? now.Year;

            using (var db = new DBDataContext())
            {
                var importData = from invoice in db.ImportInvoices
                                 join detail in db.ImportInvoiceDetails on invoice.Id equals detail.ImportInvoiceId
                                 select new { invoice.ImportDate, detail.Quantity };

                var exportData = from invoice in db.ExportInvoices
                                 join detail in db.ExportInvoiceDetails on invoice.Id equals detail.ExportInvoiceId
                                 select new { invoice.ExportDate, detail.Quantity };

                var result = new List<object>();

                if (type == "day")
                {
                    var importGrouped = importData
                        .Where(x => x.ImportDate.Month == selectedMonth && x.ImportDate.Year == selectedYear)
                        .GroupBy(x => x.ImportDate.Day)
                        .Select(g => new { Day = g.Key, Quantity = g.Sum(x => x.Quantity) });

                    var exportGrouped = exportData
                        .Where(x => x.ExportDate.Month == selectedMonth && x.ExportDate.Year == selectedYear)
                        .GroupBy(x => x.ExportDate.Day)
                        .Select(g => new { Day = g.Key, Quantity = g.Sum(x => x.Quantity) });

                    for (int day = 1; day <= DateTime.DaysInMonth(selectedYear, selectedMonth); day++)
                    {
                        result.Add(new
                        {
                            Label = $"{day}/{selectedMonth}/{selectedYear}",
                            ImportQuantity = importGrouped.FirstOrDefault(x => x.Day == day)?.Quantity ?? 0,
                            ExportQuantity = exportGrouped.FirstOrDefault(x => x.Day == day)?.Quantity ?? 0
                        });
                    }
                }
                else if (type == "month")
                {
                    var importGrouped = importData
                        .Where(x => x.ImportDate.Year == selectedYear)
                        .GroupBy(x => x.ImportDate.Month)
                        .Select(g => new { Month = g.Key, Quantity = g.Sum(x => x.Quantity) });

                    var exportGrouped = exportData
                        .Where(x => x.ExportDate.Year == selectedYear)
                        .GroupBy(x => x.ExportDate.Month)
                        .Select(g => new { Month = g.Key, Quantity = g.Sum(x => x.Quantity) });

                    for (int m = 1; m <= 12; m++)
                    {
                        result.Add(new
                        {
                            Label = $"Tháng {m}/{selectedYear}",
                            ImportQuantity = importGrouped.FirstOrDefault(x => x.Month == m)?.Quantity ?? 0,
                            ExportQuantity = exportGrouped.FirstOrDefault(x => x.Month == m)?.Quantity ?? 0
                        });
                    }
                }
                else if (type == "quarter")
                {
                    var importGrouped = importData
                        .Where(x => x.ImportDate.Year == selectedYear)
                        .GroupBy(x => (x.ImportDate.Month - 1) / 3 + 1)
                        .Select(g => new { Quarter = g.Key, Quantity = g.Sum(x => x.Quantity) });

                    var exportGrouped = exportData
                        .Where(x => x.ExportDate.Year == selectedYear)
                        .GroupBy(x => (x.ExportDate.Month - 1) / 3 + 1)
                        .Select(g => new { Quarter = g.Key, Quantity = g.Sum(x => x.Quantity) });

                    for (int q = 1; q <= 4; q++)
                    {
                        result.Add(new
                        {
                            Label = $"Quý {q} - {selectedYear}",
                            ImportQuantity = importGrouped.FirstOrDefault(x => x.Quarter == q)?.Quantity ?? 0,
                            ExportQuantity = exportGrouped.FirstOrDefault(x => x.Quarter == q)?.Quantity ?? 0
                        });
                    }
                }
                else if (type == "year")
                {
                    var importGrouped = importData
                        .GroupBy(x => x.ImportDate.Year)
                        .Select(g => new { Year = g.Key, Quantity = g.Sum(x => x.Quantity) });

                    var exportGrouped = exportData
                        .GroupBy(x => x.ExportDate.Year)
                        .Select(g => new { Year = g.Key, Quantity = g.Sum(x => x.Quantity) });

                    var allYears = importGrouped.Select(x => x.Year).Union(exportGrouped.Select(x => x.Year)).Distinct().OrderBy(y => y);

                    foreach (var y in allYears)
                    {
                        result.Add(new
                        {
                            Label = y.ToString(),
                            ImportQuantity = importGrouped.FirstOrDefault(x => x.Year == y)?.Quantity ?? 0,
                            ExportQuantity = exportGrouped.FirstOrDefault(x => x.Year == y)?.Quantity ?? 0
                        });
                    }
                }

                return Json(result, JsonRequestBehavior.AllowGet);
            }    


        }

    }
}