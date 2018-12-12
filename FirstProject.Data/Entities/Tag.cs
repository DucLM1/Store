using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using FirstProject.InfrastructureLayer.ShareKernels.Entities;

namespace FirstProject.Data.Entities
{
    public class Tag : DbEntity<string>
    {
        [MaxLength(50)]
        [Required]
        public string Name { get; set; }
        [MaxLength(50)]
        [Required]
        public string Type { get; set; }
    }
}
