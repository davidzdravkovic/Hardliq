namespace TaskManager.Models;
public class Topic
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? ParentId { get; set; }  
    public required string Name { get; set; }
    public required string Type { get; set; }   
    public int SortOrder { get; set; }
}