namespace CannedNet.Models;

public class Storefront
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StorefrontType { get; set; }
    public DateTime NextUpdate { get; set; }
    
    public virtual ICollection<StorefrontItem> Items { get; set; } = new List<StorefrontItem>();
}

