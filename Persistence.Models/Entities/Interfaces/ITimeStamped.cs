namespace Persistence.Models.Entities.Interfaces;

public interface ITimeStamped
{
    public DateTime CreatedUtc { get; set; }
    public DateTime? ModifiedUtc { get; set; }
}