using FirstProject.Data.Enums;

namespace FirstProject.Data.Interfaces
{
    /// <summary>
    /// Dành cho triển khai Status
    /// </summary>
    public interface ISwitchable
    {
        Status Status { get; set; }
    }
}