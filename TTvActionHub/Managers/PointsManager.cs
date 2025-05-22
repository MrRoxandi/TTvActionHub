using Microsoft.EntityFrameworkCore;
using TTvActionHub.Managers.PointsManagerItems;

namespace TTvActionHub.Managers;

public class PointsManager
{
    private readonly PointsDbContext _context;

    public PointsManager(string tag)
    {
        _context = new PointsDbContext(tag);
        _context.EnsureCreated();
    }
    
    public async Task<bool> ContainsUsernameAsync(string username) => 
        await GetUserAsync(username) is not null;
    
    public async Task<bool> ContainsIdAsync(string id) => 
        await GetUserByIdAsync(id) is not null;

    public async Task<bool> CreateUserAsync(string username, string id)
    {
        if (await ContainsUsernameAsync(username) || await ContainsIdAsync(id)) 
            return false;
            
        var tmp = new PointsData { Username = username, UserId = id };
        await _context.Users.AddAsync(tmp);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetUserPointsAsync(string username, long points)
    {
        var user = await GetUserAsync(username);
        if (user is null) return false;
        
        user.Points = points;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddUserPointsAsync(string username, long points)
    {
        var user = await GetUserAsync(username);
        if (user is null) return false;
        
        user.Points += points;
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> SetUserPointsByIdAsync(string id, long points)
    {
        var user = await GetUserByIdAsync(id);
        if (user is null) return false;
        
        user.Points = points;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddUserPointsByIdAsync(string id, long points)
    {
        var user = await GetUserByIdAsync(id);
        if (user is null) return false;
        
        user.Points += points;
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<long> GetUserPointsAsync(string username)
    {
        var user = await GetUserAsync(username);
        return user?.Points ?? 0;
    }
    
    public async Task<long> GetUserPointsByIdAsync(string id)
    {
        var user = await GetUserByIdAsync(id);
        return user?.Points ?? 0;
    }
    
    private async Task<PointsData?> GetUserAsync(string username) => 
        await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        
    private async Task<PointsData?> GetUserByIdAsync(string id) => 
        await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
    
    
    public bool ContainsUsername(string username) => ContainsUsernameAsync(username).GetAwaiter().GetResult();
    public bool ContainsId(string id) => ContainsIdAsync(id).GetAwaiter().GetResult();
    public bool CreateUser(string username, string id) => CreateUserAsync(username, id).GetAwaiter().GetResult();
    public bool SetUserPoints(string username, long points) => SetUserPointsAsync(username, points).GetAwaiter().GetResult();
    public bool AddUserPoints(string username, long points) => AddUserPointsAsync(username, points).GetAwaiter().GetResult();
    public bool SetUserPointsById(string id, long points) => SetUserPointsByIdAsync(id, points).GetAwaiter().GetResult();
    public bool AddUserPointsById(string id, long points) => AddUserPointsByIdAsync(id, points).GetAwaiter().GetResult();
    public long GetUserPoints(string username) => GetUserPointsAsync(username).GetAwaiter().GetResult();
    public long GetUserPointsById(string id) => GetUserPointsByIdAsync(id).GetAwaiter().GetResult();
}