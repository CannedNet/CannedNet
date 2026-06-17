using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CannedNet.Admin.Models;

namespace CannedNet.Admin.Services;

public class AdminAuthService
{
    private readonly string _filePath;
    private List<AdminUser> _users = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public AdminAuthService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "admin_users.json");
        Load();
    }

    public bool HasAnyUser()
    {
        lock (_lock) { return _users.Count > 0; }
    }

    public AdminUser? GetById(int id)
    {
        lock (_lock) { return _users.FirstOrDefault(u => u.Id == id); }
    }

    public AdminUser? GetByUsername(string username)
    {
        lock (_lock) { return _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)); }
    }

    public List<AdminUser> GetAll()
    {
        lock (_lock) { return _users.OrderBy(u => u.Id).ToList(); }
    }

    public bool IsOwner(int id)
    {
        lock (_lock) { return _users.Any(u => u.Id == id && u.IsOwner); }
    }

    public AdminUser CreateUser(string username, string password, bool isOwner)
    {
        lock (_lock)
        {
            var user = new AdminUser
            {
                Id = _nextId++,
                Username = username,
                PasswordHash = HashPassword(password),
                IsOwner = isOwner,
                CreatedAt = DateTime.UtcNow
            };
            _users.Add(user);
            Save();
            return user;
        }
    }

    public bool ChangePassword(int id, string oldPassword, string newPassword)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return false;
            if (user.PasswordHash != HashPassword(oldPassword)) return false;
            user.PasswordHash = HashPassword(newPassword);
            Save();
            return true;
        }
    }

    public bool DeleteUser(int id)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null) return false;
            _users.Remove(user);
            Save();
            return true;
        }
    }

    public bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    private string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + "CannedNetAdminSalt!@#$"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var users = JsonSerializer.Deserialize<List<AdminUser>>(json);
                if (users != null && users.Count > 0)
                {
                    _users = users;
                    _nextId = _users.Max(u => u.Id) + 1;
                }
            }
        }
        catch { }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }
}
