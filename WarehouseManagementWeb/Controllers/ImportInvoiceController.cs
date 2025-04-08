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

        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Create(ImportInvoiceDto dto)
        {
            using (var db = new DBDataContext())
            {
                // Kiểm tra mã hóa đơn đã tồn tại chưa
                var existingInvoice = db.ImportInvoices.FirstOrDefault(i => i.InvoiceCode == dto.InvoiceCode);
                if (existingInvoice != null)
                {
                    return Json(new { success = false, message = "Mã hóa đơn đã tồn tại!" });
                }

                // Tạo mới hóa đơn
                var invoice = new ImportInvoice
                {
                    InvoiceCode = dto.InvoiceCode,
                    ImportDate = dto.ImportDate,
                    SupplierName = dto.SupplierName,
                    Note = dto.Note,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                db.ImportInvoices.InsertOnSubmit(invoice);
                db.SubmitChanges(); // Lưu trước để có invoice.Id

                // Duyệt qua danh sách sản phẩm
                //foreach (var item in dto.Products)
                //{
                //    //// Tìm sản phẩm theo mã
                //    //var product = db.Products.FirstOrDefault(p => p.Code == item.ProductCode);

                //    //if (product == null)
                //    //{
                //    //    // Nếu chưa có thì tạo mới
                //    //    product = new Product
                //    //    {
                //    //        Code = item.ProductCode,
                //    //        Name = item.ProductName,
                //    //        Unit = item.Unit,
                //    //        Price = item.UnitPrice,
                //    //        Quantity = item.Quantity,
                //    //        CreatedAt = DateTime.Now,
                //    //        UpdatedAt = DateTime.Now
                //    //    };
                //    //    db.Products.InsertOnSubmit(product);
                //    //    db.SubmitChanges(); // Lưu lại để có product.Id
                //    //}
                //    //else
                //    //{
                //    //    // Nếu đã có thì cập nhật số lượng tồn kho
                //    //    product.Quantity += item.Quantity;
                //    //    product.UpdatedAt = DateTime.Now;
                //    //    db.SubmitChanges();
                //    //}

                //    var product = db.Products.FirstOrDefault(p => p.Code == item.ProductCode);

                //    product.Quantity += item.Quantity;
                //    product.UpdatedAt = DateTime.Now;

                //    db.SubmitChanges();

                //    // Tạo chi tiết hóa đơn
                //    var detail = new ImportInvoiceDetail
                //    {
                //        ImportInvoiceId = invoice.Id,
                //        ProductId = product.Id,
                //        Quantity = item.Quantity,
                //        UnitPrice = item.UnitPrice
                //    };
                //    db.ImportInvoiceDetails.InsertOnSubmit(detail);
                //}

                if (dto.Products != null && dto.Products.Any())
                {
                    foreach (var item in dto.Products)
                    {
                        var product = db.Products.FirstOrDefault(p => p.Code == item.ProductCode);

                        product.Quantity += item.Quantity;
                        product.UpdatedAt = DateTime.Now;

                        db.SubmitChanges();

                        // Tạo chi tiết hóa đơn
                        var detail = new ImportInvoiceDetail
                        {
                            ImportInvoiceId = invoice.Id,
                            ProductId = product.Id,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        };
                        db.ImportInvoiceDetails.InsertOnSubmit(detail);
                    }
                }
                else
                {
                    return Json(new { success = false, message = "Danh sách sản phẩm rỗng!" });
                }

                db.SubmitChanges(); // Lưu toàn bộ chi tiết
                return Json(new { success = true, message = "Tạo hóa đơn nhập thành công!" });
            }
        }

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

    }
}