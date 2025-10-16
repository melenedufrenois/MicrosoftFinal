using System.Net.Http.Json;
using LoLProject.Persistence.Models;

namespace LoLProject.WebApp.Clients;

public sealed class TodoClient : ITodoClient
{
    private readonly HttpClient _http;
    public TodoClient(HttpClient http) => _http = http;

    public Task<List<TodoItem>?> GetTodoItemsAsync()
        => _http.GetFromJsonAsync<List<TodoItem>>("/api/todo");

    public async Task<TodoItem> CreateTodoItemAsync(TodoItem item)
    {
        var resp = await _http.PostAsJsonAsync("/api/todo", item);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<TodoItem>())!;
    }
}