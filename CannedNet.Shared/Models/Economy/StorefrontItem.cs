namespace CannedNet.Models;

public class StorefrontItem
{
    public int Id { get; set; }
    public int StorefrontId { get; set; }
    public int PurchasableItemId { get; set; }
    public int Type { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? NewUntil { get; set; }
    
    public virtual Storefront Storefront { get; set; } = null!;
    public virtual ICollection<GiftDrop> GiftDrops { get; set; } = new List<GiftDrop>();
    public virtual ICollection<StorefrontPrice> Prices { get; set; } = new List<StorefrontPrice>();
}

