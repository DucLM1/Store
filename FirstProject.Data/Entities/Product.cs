using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using FirstProject.Data.Enums;
using FirstProject.Data.Interfaces;
using FirstProject.InfrastructureLayer.ShareKernels.Entities;
using ServiceStack.DataAnnotations;

namespace FirstProject.Data.Entities
{
    public class Product : DbEntity<int>,ISwitchable,IDateTracking,IHasSeoMetaData
    {
        [StringLength(255)]
        [Required]
        public string Name { get; set; }
        [Required]
        public int CategoryId { get; set; }
        public string Image { get; set; }
        [Required]
        [DefaultValue(0)]
        public decimal Price { get; set; }
        public decimal PromotionPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public bool? HomeFlag { get; set; }
        public bool? HotFlag { get; set; }
        public int? ViewCount { get; set; }
        public string Tags { get; set; }
        //Lazy Loading 
        public string Unit { get; set; }
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("CategoryId")] // Trùng với tên trường thứ 2 - khóa ngoại
        public virtual ProductCategory ProductCategory { get; set; }

        public Status Status { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public string SeoPageTitle { get; set; }
        [Column(TypeName = "varchar")]
        public string SeoAlias { get; set; }
        public string SeoKeywords { get; set; }
        public string SeoDescription { get; set; }
    }
}