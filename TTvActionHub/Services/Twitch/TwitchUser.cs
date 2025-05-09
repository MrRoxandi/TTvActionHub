using System.ComponentModel.DataAnnotations;

namespace TTvActionHub.Services.Twitch;

public class TwitchUser
{
    [Key]
    public int Id { get; set; }
    public long Points { get; set; }
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
}