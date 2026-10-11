namespace Web2Hand.Models.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
}
