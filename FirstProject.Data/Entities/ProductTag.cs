using FirstProject.InfrastructureLayer.ShareKernels.Entities;
using ServiceStack.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FirstProject.Data.Entities
{
    public class ProductTag : DbEntity<int>
    {
        public int ProductId { get; set; }

        [StringLength(50)]
        [Column(TypeName = "varchar")]
        public string TagId { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("TagId")]
        public virtual Tag Tag { get; set; }
    }
}