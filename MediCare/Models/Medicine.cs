using System;
using System.ComponentModel.DataAnnotations;

namespace MediCare.Models
{
    public class Medicine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MedicineName { get; set; }
        public string? GenericName { get; set; }
        public string? Category { get; set; }
        public string? DosageForm { get; set; }
        public string? Strength { get; set; }
        public string? PackSize { get; set; }
        
        public string? Manufacturer { get; set; }
        public string? Supplier { get; set; }

        [Required]
        public int Stock { get; set; }
        public int ReorderLevel { get; set; }
        public string? Unit { get; set; }

        [Required]
        public decimal PurchasePrice { get; set; }
        
        [Required]
        public decimal SellingPrice { get; set; }
        public decimal GST { get; set; }

        public DateTime? MfgDate { get; set; }
        public DateTime? ExpDate { get; set; }
        public string? Storage { get; set; }

        public string? Usage { get; set; }
        public string? SideEffects { get; set; }
        public string? Instructions { get; set; }
        public string? PrescriptionRequired { get; set; }
    }
}
