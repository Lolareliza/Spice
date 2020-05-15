using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models.ViewModels
{
    public class OrderDetailsCart
    {
       
        public OrderHeader OrderHeader { get; set; }
        public List<ShoppingBag> ListCart { get; set; }

        //it is good to always use models inside viewmodels, if you make any change to a model those properties will still be in your view model
    }
}
