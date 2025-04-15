using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WarehouseManagementWeb.Models.Dtos
{
    public class ExportInvoiceProductDto
    {
        public string ProductCode { get; set; } // Dùng để kiểm tra tồn tại
        public string ProductName { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}