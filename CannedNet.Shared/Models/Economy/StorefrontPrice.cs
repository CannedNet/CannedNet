namespace CannedNet.Models;

public class StorefrontPrice
{
    public int Id { get; set; }
    public int StorefrontItemId { get; set; }
    public int CurrencyType { get; set; }
    public int Price { get; set; }
    
    public virtual StorefrontItem StorefrontItem { get; set; } = null!;
}

