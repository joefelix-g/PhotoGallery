namespace PhotoGallery.Models;

public class Photo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? FileName { get; set; }
    public User? User { get; set; }
    public int UserId { get; set; }
}