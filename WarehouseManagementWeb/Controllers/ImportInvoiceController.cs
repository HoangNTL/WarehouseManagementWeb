using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WarehouseManagementWeb.Models;
using WarehouseManagementWeb.Models.Dtos;

namespace WarehouseManagementWeb.Controllers
{
    public class ImportInvoiceController : Controller
    {
        // GET: ImportInvoice
        public ActionResult Index()
        {
            using (var db = new DBDataContext())
            {
                var importInvoices = db.ImportInvoices.ToList();
                return View(importInvoices);
            }
        }

        #region Add Import Invoice
        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Create(ImportInvoiceDto dto)
        {
            using (var db = new DBDataContext())
            {
                db.Connection.Open();

                // Bắt đầu transaction
                using (var transaction = db.Connection.BeginTransaction())
                {
                    db.Transaction = transaction;

                    try
                    {
                        // Kiểm tra mã hóa đơn đã tồn tại chưa
                        string invoiceCode;
                        do
                        {
                            invoiceCode = GenerateInvoiceCode();
                        }
                        while (db.ImportInvoices.Any(i => i.InvoiceCode == invoiceCode));

                        // Tạo mới hóa đơn
                        var invoice = new ImportInvoice
                        {
                            InvoiceCode = invoiceCode,
                            ImportDate = dto.ImportDate,
                            SupplierName = dto.SupplierName,
                            Note = dto.Note,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        db.ImportInvoices.InsertOnSubmit(invoice);
                        db.SubmitChanges(); // Lưu để có invoice.Id

                        if (dto.Products != null && dto.Products.Any())
                        {
                            foreach (var item in dto.Products)
                            {
                                var product = db.Products.FirstOrDefault(p => p.Code == item.ProductCode);
                                if (product == null)
                                {
                                    throw new Exception($"Không tìm thấy sản phẩm với mã {item.ProductCode}");
                                }

                                product.Quantity += item.Quantity;
                                product.UpdatedAt = DateTime.Now;

                                var detail = new ImportInvoiceDetail
                                {
                                    ImportInvoiceId = invoice.Id,
                                    ProductId = product.Id,
                                    Quantity = item.Quantity,
                                    UnitPrice = (decimal)product.Price,
                                };
                                db.ImportInvoiceDetails.InsertOnSubmit(detail);
                            }
                        }
                        else
                        {
                            throw new Exception("Danh sách sản phẩm rỗng!");
                        }

                        db.SubmitChanges(); // Lưu toàn bộ chi tiết + cập nhật
                        transaction.Commit(); // OK thì commit

                        return Json(new { success = true, message = "Tạo hóa đơn nhập thành công!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Có lỗi thì rollback toàn bộ
                        return Json(new { success = false, message = "Tạo hóa đơn thất bại: " + ex.Message });
                    }
                    finally
                    {
                        db.Connection.Close(); // Đóng kết nối
                    }
                }
            }
        }
        #endregion

        #region Edit Import Invoice
        public ActionResult Edit()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Edit(ImportInvoiceDto dto)
        {
            using (var db = new DBDataContext())
            {
                var existingInvoice = db.ImportInvoices.FirstOrDefault(i => i.InvoiceCode == dto.InvoiceCode);
                if (existingInvoice == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn để cập nhật!" });
                }

                existingInvoice.ImportDate = dto.ImportDate;
                existingInvoice.SupplierName = dto.SupplierName;
                existingInvoice.Note = dto.Note;
                existingInvoice.UpdatedAt = DateTime.Now;
                db.SubmitChanges();

                var oldDetails = db.ImportInvoiceDetails.Where(d => d.ImportInvoiceId == existingInvoice.Id).ToList();
                db.ImportInvoiceDetails.DeleteAllOnSubmit(oldDetails);
                db.SubmitChanges();

                foreach (var detail in oldDetails)
                {
                    var product = db.Products.FirstOrDefault(p => p.Id == detail.ProductId);
                    if (product != null)
                    {
                        product.Quantity -= detail.Quantity;
                        product.UpdatedAt = DateTime.Now;
                    }
                }

                db.SubmitChanges();

                if (dto.Products != null && dto.Products.Any())
                {
                    foreach (var item in dto.Products)
                    {
                        var product = db.Products.FirstOrDefault(p => p.Code == item.ProductCode);
                        if (product != null)
                        {
                            product.Quantity += item.Quantity;
                            product.UpdatedAt = DateTime.Now;
                            db.SubmitChanges();

                            var newDetail = new ImportInvoiceDetail
                            {
                                ImportInvoiceId = existingInvoice.Id,
                                ProductId = product.Id,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice
                            };

                            db.ImportInvoiceDetails.InsertOnSubmit(newDetail);
                        }
                    }

                    db.SubmitChanges();
                }
                else
                {
                    return Json(new { success = false, message = "Danh sách sản phẩm rỗng!" });
                }

                return Json(new { success = true, message = "Cập nhật hóa đơn thành công!" });
            }
        }
        #endregion

        #region Filter Import Invoice
        public ActionResult FilterByDate(string fromDate, string toDate, string keyword)
        {
            using (var db = new DBDataContext())
            {
                var query = db.ImportInvoices.AsQueryable();

                if (DateTime.TryParse(fromDate, out var from))
                    query = query.Where(i => i.ImportDate >= from);

                if (DateTime.TryParse(toDate, out var to))
                    query = query.Where(i => i.ImportDate <= to);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim().ToLower(); // chuẩn hóa keyword
                    query = query.Where(i =>
                        i.InvoiceCode.ToLower().Contains(keyword) ||
                        i.SupplierName.ToLower().Contains(keyword));
                }

                var result = query.OrderByDescending(i => i.ImportDate).ToList();
                return PartialView("_ImportInvoiceTablePartial", result);
            }
        }
        #endregion

        private string GenerateInvoiceCode()
        {
            string shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"HD-II-{shortGuid}";
        }
    }
}