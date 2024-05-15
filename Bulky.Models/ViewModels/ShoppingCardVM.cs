using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Models.ViewModels
{
    public class ShoppingCardVM
    {
        public OrderHeader OrderHeader { get; set; }
        public IEnumerable<ShoppingCard> ShoppingCardList { get; set; }
        public double OrderTotal { get; set; }
    }
}
