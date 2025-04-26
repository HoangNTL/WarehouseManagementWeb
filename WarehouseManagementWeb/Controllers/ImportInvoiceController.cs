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
        public ActionResult Edit(int id)
        {
            using (var db = new DBDataContext())
            {
                var invoice = db.ImportInvoices.FirstOrDefault(i => i.Id == id);

                if (invoice == null)
                {
                    return HttpNotFound();
                }

                var invoiceDetails = (from detail in db.ImportInvoiceDetails
                                      join product in db.Products on detail.ProductId equals product.Id
                                      where detail.ImportInvoiceId == id
                                      select new ImportInvoiceProductDto
                                      {
                                          ProductCode = product.Code,
                                          ProductName = product.Name,
                                          Unit = product.Unit,
                                          UnitPrice = detail.UnitPrice,
                                          Quantity = detail.Quantity
                                      }).ToList();

                var dto = new ImportInvoiceDto
                {
                    InvoiceCode = invoice.InvoiceCode,
                    ImportDate = invoice.ImportDate,
                    SupplierName = invoice.SupplierName,
                    Note = invoice.Note,
                    Products = invoiceDetails
                };

                return View(dto);
            }
        }


        [HttpPost]
        public JsonResult Update(ImportInvoiceDto dto)
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
                        var invoice = db.ImportInvoices.FirstOrDefault(i => i.InvoiceCode == dto.InvoiceCode);
                        if (invoice == null)
                        {
                            throw new Exception($"Không tìm thấy hóa đơn với mã {dto.InvoiceCode}");
                        }

                        // Cập nhật thông tin hóa đơn
                        invoice.ImportDate = dto.ImportDate;
                        invoice.SupplierName = dto.SupplierName;
                        invoice.Note = dto.Note;
                        invoice.UpdatedAt = DateTime.Now;

                        // Xóa tất cả các chi tiết hóa đơn cũ (có thể thay đổi sản phẩm hoặc số lượng)
                        var existingDetails = db.ImportInvoiceDetails.Where(d => d.ImportInvoiceId == invoice.Id).ToList();
                        db.ImportInvoiceDetails.DeleteAllOnSubmit(existingDetails);

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

                        db.SubmitChanges(); // Lưu toàn bộ thông tin hóa đơn và chi tiết
                        transaction.Commit(); // Commit transaction

                        return Json(new { success = true, message = "Cập nhật hóa đơn nhập thành công!" });
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

        [HttpGet]
        public ActionResult Details(int id)
        {
            using (var db = new DBDataContext())
            {
                var invoice = db.ImportInvoices.FirstOrDefault(i => i.Id == id);

                if (invoice == null)
                {
                    return HttpNotFound();
                }

                var invoiceDetails = (from detail in db.ImportInvoiceDetails
                                      join product in db.Products on detail.ProductId equals product.Id
                                      where detail.ImportInvoiceId == id
                                      select new ImportInvoiceProductDto
                                      {
                                          ProductCode = product.Code,
                                          ProductName = product.Name,
                                          Unit = product.Unit,
                                          UnitPrice = detail.UnitPrice,
                                          Quantity = detail.Quantity
                                      }).ToList();

                var dto = new ImportInvoiceDto
                {
                    InvoiceCode = invoice.InvoiceCode,
                    ImportDate = invoice.ImportDate,
                    SupplierName = invoice.SupplierName,
                    Note = invoice.Note,
                    Products = invoiceDetails
                };

                return PartialView("_ImportInvoiceDetailsPartial", dto);
            }
        }

    }
}