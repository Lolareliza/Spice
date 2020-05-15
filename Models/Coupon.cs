using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Spice.Models
{
    public class Coupon
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string CouponType { get; set; }

        public enum ECouponType { Percent=0, Dollar=1 }

        [Required]
        public double Discount { get; set; }

        [Required]
        public double MinimumAmount { get; set; }

        //Uploading on the DB not the server
        public byte[] Picture { get; set; } //whenever you are saving an image in DB, the DB type should be a byte of streams
        public bool IsActive { get; set; }
    }
}
