using System.ComponentModel.DataAnnotations;

namespace TTvActionHub.Managers.PointsManagerItems;

public class PointsData
{
    [Key]
    public int Id { get; set; }
    [MaxLength(150)]
    public string Username { get; set; } = string.Empty;
    [MaxLength(150)]
    public string UserId { get; set; } = string.Empty;
    public long Points { get; set; }
}