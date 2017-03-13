using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageKeeper.SqlServerBackend.Tests
{
    public class Order
    {
        public int OrderId { get; set; }
        public int ClientId { get; set; }
        public string ProductCode { get; set; }
        public string OfferCode { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
