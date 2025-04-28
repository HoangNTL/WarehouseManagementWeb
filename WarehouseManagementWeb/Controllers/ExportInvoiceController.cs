using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WarehouseManagementWeb.Models;
using WarehouseManagementWeb.Models.Dtos;

namespace WarehouseManagementWeb.Controllers
{
    public class ExportInvoiceController : Controller
    {

        public ActionResult Index()
        {
            using (var db = new DBDataContext())
            {
                var exportInvoices = db.ExportInvoices.ToList();
                return View(exportInvoices);
            }
        }

        #region Add Export Invoice
        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Create(ExportInvoiceDto dto)
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
                        while (db.ExportInvoices.Any(i => i.InvoiceCode == invoiceCode));

                        // Tạo mới hóa đơn
                        var invoice = new ExportInvoice
                        {
                            InvoiceCode = invoiceCode,
                            ExportDate = dto.ExportDate,
                            CustomerName = dto.CustomerName,
                            Note = dto.Note,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };
                        db.ExportInvoices.InsertOnSubmit(invoice);
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

                                if (product.Quantity < item.Quantity)
                                {
                                    throw new Exception($"Sản phẩm {product.Name} không đủ số lượng trong kho. Còn lại: {product.Quantity}, yêu cầu: {item.Quantity}");
                                }

                                product.Quantity -= item.Quantity; // giảm tồn kho
                                product.UpdatedAt = DateTime.Now;

                                var detail = new ExportInvoiceDetail
                                {
                                    ExportInvoiceId = invoice.Id,
                                    ProductId = product.Id,
                                    Quantity = item.Quantity,
                                    UnitPrice = (decimal)product.Price,
                                };
                                db.ExportInvoiceDetails.InsertOnSubmit(detail);
                            }
                        }
                        else
                        {
                            throw new Exception("Danh sách sản phẩm rỗng!");
                        }

                        db.SubmitChanges(); // Lưu toàn bộ chi tiết + cập nhật
                        transaction.Commit(); // OK thì commit

                        return Json(new { success = true, message = "Tạo hóa đơn xuất thành công!" });
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

        #region Edit Export Invoice
        public ActionResult Edit(int id)
        {
            using (var db = new DBDataContext())
            {
                var invoice = db.ExportInvoices.FirstOrDefault(i => i.Id == id);

                if (invoice == null)
                {
                    return HttpNotFound();
                }

                var invoiceDetails = (from detail in db.ExportInvoiceDetails
                                      join product in db.Products on detail.ProductId equals product.Id
                                      where detail.ExportInvoiceId == id
                                      select new ExportInvoiceProductDto
                                      {
                                          ProductCode = product.Code,
                                          ProductName = product.Name,
                                          Unit = product.Unit,
                                          UnitPrice = detail.UnitPrice,
                                          Quantity = detail.Quantity
                                      }).ToList();

                var dto = new ExportInvoiceDto
                {
                    InvoiceCode = invoice.InvoiceCode,
                    ExportDate = invoice.ExportDate,
                    CustomerName = invoice.CustomerName,
                    Note = invoice.Note,
                    Products = invoiceDetails
                };

                return View(dto);
            }
        }

        //[HttpPost]
        //public JsonResult Edit(ImportInvoiceDto dto)
        //{
        //    using (var db = new DBDataContext())
        //    {
        //        var existingInvoice = db.ImportInvoices.FirstOrDefault(i => i.InvoiceCode == dto.InvoiceCode);
        //        if (existingInvoice == null)
        //        {
        //            return Json(new { success = false, message = "Không tìm thấy hóa đơn để cập nhật!" });
        //        }

        //        existingInvoice.ImportDate = dto.ImportDate;
        //        existingInvoice.SupplierName = dto.SupplierName;
        //        existingInvoice.Note = dto.Note;
        //        existingInvoice.UpdatedAt = DateTime.Now;
        //        db.SubmitChanges();

        //        var oldDetails = db.ImportInvoiceDetails.Where(d => d.ImportInvoiceId == existingInvoice.Id).ToList();
        //        db.ImportInvoiceDetails.DeleteAllOnSubmit(oldDetails);
        //        db.SubmitChanges();

        //        foreach (var detail in oldDetails)
        //        {
        //            var product = db.Products.FirstOrDefault(p => p.Id == detail.ProductId);
        //            if (product != null)
        //            {
        //                product.Quantity -= detail.Quantity;
        //                product.UpdatedAt = DateTime.Now;
        //            }
        //        }

        //        db.SubmitChanges();

        //        if (dto.Products != null && dto.Products.Any())
        //        {
        //            foreach (var item in dto.Products)
        //            {
        //                var product = db.Products.FirstOrDefault(p => p.Code == item.ProductCode);
        //                if (product != null)
        //                {
        //                    product.Quantity += item.Quantity;
        //                    product.UpdatedAt = DateTime.Now;
        //                    db.SubmitChanges();

        //                    var newDetail = new ImportInvoiceDetail
        //                    {
        //                        ImportInvoiceId = existingInvoice.Id,
        //                        ProductId = product.Id,
        //                        Quantity = item.Quantity,
        //                        UnitPrice = item.UnitPrice
        //                    };

        //                    db.ImportInvoiceDetails.InsertOnSubmit(newDetail);
        //                }
        //            }

        //            db.SubmitChanges();
        //        }
        //        else
        //        {
        //            return Json(new { success = false, message = "Danh sách sản phẩm rỗng!" });
        //        }

        //        return Json(new { success = true, message = "Cập nhật hóa đơn thành công!" });
        //    }
        //}


        [HttpPost]
        public JsonResult Update(ExportInvoiceDto dto)
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
                        // Lấy hóa đơn theo mã hóa đơn
                        var invoice = db.ExportInvoices.FirstOrDefault(i => i.InvoiceCode == dto.InvoiceCode);
                        if (invoice == null)
                        {
                            throw new Exception($"Không tìm thấy hóa đơn với mã {dto.InvoiceCode}");
                        }

                        // Cập nhật thông tin hóa đơn
                        invoice.ExportDate = dto.ExportDate;
                        invoice.CustomerName = dto.CustomerName;
                        invoice.Note = dto.Note;
                        invoice.UpdatedAt = DateTime.Now;

                        // Xóa tất cả các chi tiết hóa đơn cũ (có thể thay đổi sản phẩm hoặc số lượng)
                        var existingDetails = db.ExportInvoiceDetails.Where(d => d.ExportInvoiceId == invoice.Id).ToList();
                        db.ExportInvoiceDetails.DeleteAllOnSubmit(existingDetails);

                        if (dto.Products != null && dto.Products.Any())
                        {
                            foreach (var item in dto.Products)
                            {
                                var product = db.Products.FirstOrDefault(p => p.Code == item.ProductCode);
                                if (product == null)
                                {
                                    throw new Exception($"Không tìm thấy sản phẩm với mã {item.ProductCode}");
                                }

                                // Cập nhật lại số lượng sản phẩm
                                product.Quantity += item.Quantity;
                                product.UpdatedAt = DateTime.Now;

                                // Thêm chi tiết hóa đơn nhập mới
                                var detail = new ExportInvoiceDetail
                                {
                                    ExportInvoiceId = invoice.Id,
                                    ProductId = product.Id,
                                    Quantity = item.Quantity,
                                    UnitPrice = (decimal)product.Price,
                                };
                                db.ExportInvoiceDetails.InsertOnSubmit(detail);
                            }
                        }
                        else
                        {
                            throw new Exception("Danh sách sản phẩm rỗng!");
                        }

                        db.SubmitChanges(); // Lưu toàn bộ thông tin hóa đơn và chi tiết
                        transaction.Commit(); // Commit transaction

                        return Json(new { success = true, message = "Cập nhật hóa đơn xuất thành công!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback transaction nếu có lỗi
                        return Json(new { success = false, message = "Cập nhật hóa đơn thất bại: " + ex.Message });
                    }
                    finally
                    {
                        db.Connection.Close(); // Đóng kết nối
                    }
                }
            }
        }
        #endregion

        #region Filter Export Invoice
        public ActionResult FilterByDate(string fromDate, string toDate, string keyword)
        {
            using (var db = new DBDataContext())
            {
                var query = db.ExportInvoices.AsQueryable();

                if (DateTime.TryParse(fromDate, out var from))
                    query = query.Where(i => i.ExportDate >= from);

                if (DateTime.TryParse(toDate, out var to))
                    query = query.Where(i => i.ExportDate <= to);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim().ToLower(); // chuẩn hóa keyword
                    query = query.Where(i =>
                        i.InvoiceCode.ToLower().Contains(keyword) ||
                        i.CustomerName.ToLower().Contains(keyword));
                }

                var result = query.OrderByDescending(i => i.ExportDate).ToList();
                return PartialView("_ExportInvoiceTablePartial", result);
            }
        }
        #endregion


        private string GenerateInvoiceCode()
        {
            string shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return $"HD-EI-{shortGuid}";
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            using (var db = new DBDataContext())    
            {
                var invoice = db.ExportInvoices.FirstOrDefault(i => i.Id == id);

                if (invoice == null)
                {
                    return HttpNotFound();
                }

                var invoiceDetails = (from detail in db.ExportInvoiceDetails
                                      join product in db.Products on detail.ProductId equals product.Id
                                      where detail.ExportInvoiceId == id
                                      select new ExportInvoiceProductDto
                                      {
                                          ProductCode = product.Code,
                                          ProductName = product.Name,
                                          Unit = product.Unit,
                                          UnitPrice = detail.UnitPrice,
                                          Quantity = detail.Quantity
                                      }).ToList();

                var dto = new ExportInvoiceDto
                {
                    InvoiceCode = invoice.InvoiceCode,
                    ExportDate = invoice.ExportDate,
                    CustomerName = invoice.CustomerName,
                    Note = invoice.Note,
                    Products = invoiceDetails
                };

                return PartialView("_ExportInvoiceDetailsPartial", dto);
            }
        }

    }
}