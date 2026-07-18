using System.Text.Json;
using AICenterProject.Models;

namespace AICenterProject.Services;

public sealed class UserStore
{
    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<User> _users = [];

    public UserStore(IWebHostEnvironment environment) => _path = Path.Combine(environment.ContentRootPath, "App_Data", "users.json");

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        if (!File.Exists(_path)) return;
        await using var stream = File.OpenRead(_path);
        _users = await JsonSerializer.DeserializeAsync<List<User>>(stream) ?? [];
    }

    public async Task<User?> FindByUsernameAsync(string username)
    {
        await _lock.WaitAsync();
        try { return _users.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase)); }
        finally { _lock.Release(); }
    }

    public async Task<User?> FindByIdAsync(Guid id)
    {
        await _lock.WaitAsync();
        try { return _users.FirstOrDefault(x => x.UserId == id); }
        finally { _lock.Release(); }
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try { return _users.ToList(); }
        finally { _lock.Release(); }
    }

    public async Task<bool> AddAsync(User user)
    {
        await _lock.WaitAsync();
        try
        {
            if (_users.Any(x => string.Equals(x.Username, user.Username, StringComparison.OrdinalIgnoreCase))) return false;
            _users.Add(user);
            await SaveAsync();
            return true;
        }
        finally { _lock.Release(); }
    }

    private async Task SaveAsync()
    {
        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
            await JsonSerializer.SerializeAsync(stream, _users, new JsonSerializerOptions { WriteIndented = true });
        File.Move(temp, _path, true);
    }
}
