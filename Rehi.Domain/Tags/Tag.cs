namespace Rehi.Domain.Tags;
 
 public class Tag
 {
     public Guid Id { get; set; }
     public string Name { get; set; } = null!;
     public Guid UserId { get; set; }
     
     public bool IsDeleted { get; set; }
     public DateTimeOffset CreateAt { get; set; }
     public DateTimeOffset? UpdateAt { get; set; }
 
 }