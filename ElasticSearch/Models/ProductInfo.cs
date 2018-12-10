using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Nest;
namespace ElasticSearch.Models
{
    [Description("product_publish")]
    [ElasticsearchType(Name = "product_publish")]
    public class ProductInfo
    {
        public ProductInfo() { }

        public ProductInfo(ProductInfoOnList product)
        {
            this.Id = product.productid;
            this.Productid = product.productid;
            this.Title = product.title;
            this.Maker = product.maker;
            this.Model = product.model;
            this.Version = product.version;
            this.Transmissionid = product.transmissionid;
            this.Price = product.price;
            this.Year = product.year;
            this.City = product.city;
            this.Numofkm = product.numofkm;
            this.Numofkmunit = product.numofkmunit;
            this.Image = product.image;
            this.Usertype = product.usertype;
            this.Secondhand = product.secondhand;
            this.Createduser = product.createduser;
            this.Createdate = product.createdate;
            this.Ispublish = product.ispublish;
            this.Branchname = product.branchname;
            this.Modelname = product.modelname;
            this.Versionname = product.versionname;
            this.Cityname = product.cityname;
            this.Publishdate = product.publishdate;
            this.Color = product.color;
            this.Type = product.type;
            this.Viptype = product.viptype;
        }

        [Description("_id")]
        public int Id { get; set; }

        [Description("productid")]
        public int Productid { get; set; }

        [Description("title")]
        public string Title { get; set; }

        [Description("maker")]
        public int Maker { get; set; }

        [Description("model")]
        public int Model { get; set; }

        [Description("version")]
        public short Version { get; set; }

        [Description("transmissionid")]
        public int Transmissionid { get; set; }

        [Description("price")]
        public decimal Price { get; set; }

        [Description("year")]
        public short Year { get; set; }

        [Description("city")]
        public int City { get; set; }

        [Description("numofkm")]
        public int Numofkm { get; set; }

        [Description("numofkmunit")]
        public string Numofkmunit { get; set; }

        [Description("image")]
        public string Image { get; set; }

        [Description("usertype")]
        public int Usertype { get; set; }

        [Description("secondhand")]
        public int Secondhand { get; set; }

        [Description("createduser")]
        public int Createduser { get; set; }

        [Description("createdate")]
        public long Createdate { get; set; }

        [Description("branchname")]
        public string Branchname { get; set; }

        [Description("modelname")]
        public string Modelname { get; set; }

        [Description("versionname")]
        public string Versionname { get; set; }

        [Description("cityname")]
        public string Cityname { get; set; }

        [Description("ispublish")]
        public bool Ispublish { get; set; }

        [Description("publishdate")]
        public long Publishdate { get; set; }

        [Description("color")]
        public bool Color { get; set; }

        [Description("region")]
        public bool Region { get; set; }

        [Description("type")]
        public bool Type { get; set; }

        [Description("textsearch")]
        public string Textsearch { get; set; }

        [Description("viptype")]
        public int Viptype { get; set; }
    }
}
