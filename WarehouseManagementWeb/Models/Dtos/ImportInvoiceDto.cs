using System;
using System.Collections.Generic;

namespace WarehouseManagementWeb.Models.Dtos
{
    public class ImportInvoiceDto
    {
        public string InvoiceCode { get; set; }
        public DateTime ImportDate { get; set; }
        public string SupplierName { get; set; }
        public string Note { get; set; }

        public List<ImportInvoiceProductDto> Products { get; set; }
    }
}