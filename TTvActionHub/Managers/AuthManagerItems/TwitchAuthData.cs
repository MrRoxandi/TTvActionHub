using System.ComponentModel.DataAnnotations;

namespace TTvActionHub.Managers.AuthManagerItems;

public class TwitchAuthData
{
    [Key]
    public int Id { get; set; } 

    public string Login { get; set; } = string.Empty;
    public string TwitchUserId { get; set; } = string.Empty; 

    public string EncryptedAccessToken { get; set; } = string.Empty;
    public string EncryptedRefreshToken { get; set; } = string.Empty;
}