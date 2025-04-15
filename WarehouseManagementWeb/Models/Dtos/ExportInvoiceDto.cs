using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WarehouseManagementWeb.Models.Dtos
{
    public class ExportInvoiceDto
    {
        public string InvoiceCode { get; set; }
        public DateTime ExportDate { get; set; }
        public string CustomerName {  get; set; }
        public string Note { get; set; }
        public List<ExportInvoiceProductDto> Products { get; set; }

    }
}