using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WarehouseManagementWeb.Models;

namespace WarehouseManagementWeb.Controllers
{
    public class RevenueStatisticController : Controller
    {
        // GET: RevenueStatistic
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetRevenueStatistic(string type, int month, int year)
        {
            using (var db = new DBDataContext())
            {
                var exports = from e in db.ExportInvoices
                              join ed in db.ExportInvoiceDetails on e.Id equals ed.ExportInvoiceId
                              select new
                              {
                                  Date = e.ExportDate,
                                  Amount = ed.Quantity * ed.UnitPrice
                              };

                var imports = from i in db.ImportInvoices
                              join id in db.ImportInvoiceDetails on i.Id equals id.ImportInvoiceId
                              select new
                              {
                                  Date = i.ImportDate,
                                  Amount = id.Quantity * id.UnitPrice
                              };

                IQueryable<dynamic> groupedExports = null;
                IQueryable<dynamic> groupedImports = null;

                if (type == "day")
                {
                    // Lấy tổng doanh thu theo ngày
                    groupedExports = exports
                        .Where(x => x.Date.Year == year && x.Date.Month == month)
                        .GroupBy(x => x.Date.Day)
                        .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Amount) });

                    groupedImports = imports
                        .Where(x => x.Date.Year == year && x.Date.Month == month)
                        .GroupBy(x => x.Date.Day)
                        .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Amount) });
                }
                else if (type == "month")
                {
                    // Lấy tổng doanh thu theo tháng
                    groupedExports = exports
                        .Where(x => x.Date.Year == year)
                        .GroupBy(x => x.Date.Month)
                        .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Amount) });

                    groupedImports = imports
                        .Where(x => x.Date.Year == year)
                        .GroupBy(x => x.Date.Month)
                        .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Amount) });
                }
                else if (type == "quarter")
                {
                    // Lấy tổng doanh thu theo quý
                    groupedExports = exports
                        .Where(x => x.Date.Year == year)
                        .GroupBy(x => (x.Date.Month - 1) / 3 + 1) // Tính quý
                        .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Amount) });

                    groupedImports = imports
                        .Where(x => x.Date.Year == year)
                        .GroupBy(x => (x.Date.Month - 1) / 3 + 1)
                        .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Amount) });
                }
                else if (type == "year")
                {
                    // Lấy tổng doanh thu theo năm
                    groupedExports = exports
                        .GroupBy(x => x.Date.Year)
                        .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Amount) });

                    groupedImports = imports
                        .GroupBy(x => x.Date.Year)
                        .Select(g => new { Label = g.Key, Value = g.Sum(x => x.Amount) });
                }

                var exportList = groupedExports.ToList();
                var importList = groupedImports.ToList();

                // Tạo ra danh sách revenue, kết hợp dữ liệu xuất và nhập kho
                var revenueData = new List<object>();

                if (type == "day")
                {
                    var daysInMonth = DateTime.DaysInMonth(year, month);
                    for (int i = 1; i <= daysInMonth; i++)
                    {
                        var export = exportList.FirstOrDefault(x => x.Label == i);
                        var import = importList.FirstOrDefault(x => x.Label == i);
                        revenueData.Add(new
                        {
                            Label = $"Ngày {i}",
                            Revenue = (export?.Value ?? 0) - (import?.Value ?? 0)
                        });
                    }
                }
                else if (type == "month")
                {
                    for (int i = 1; i <= 12; i++)
                    {
                        var export = exportList.FirstOrDefault(x => x.Label == i);
                        var import = importList.FirstOrDefault(x => x.Label == i);
                        revenueData.Add(new
                        {
                            Label = $"Tháng {i}",
                            Revenue = (export?.Value ?? 0) - (import?.Value ?? 0)
                        });
                    }
                }
                else if (type == "quarter")
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        var export = exportList.FirstOrDefault(x => x.Label == i);
                        var import = importList.FirstOrDefault(x => x.Label == i);
                        revenueData.Add(new
                        {
                            Label = $"Quý {i}",
                            Revenue = (export?.Value ?? 0) - (import?.Value ?? 0)
                        });
                    }
                }
                else if (type == "year")
                {
                    var currentYear = DateTime.Now.Year; // Lấy năm hiện tại
                    var years = Enumerable.Range(2022, currentYear - 2022 + 1).ToList(); // Tạo dãy năm từ 2022 đến năm hiện tại

                    foreach (var yearLabel in years)
                    {
                        var export = exportList.FirstOrDefault(x => x.Label == yearLabel);
                        var import = importList.FirstOrDefault(x => x.Label == yearLabel);
                        revenueData.Add(new
                        {
                            Label = $"Năm {yearLabel}",
                            Revenue = (export?.Value ?? 0) - (import?.Value ?? 0)
                        });
                    }
                }

                return Json(revenueData, JsonRequestBehavior.AllowGet);
            }
        }

    }
}