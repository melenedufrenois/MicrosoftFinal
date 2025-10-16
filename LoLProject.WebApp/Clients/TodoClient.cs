using LoLProject.Persistence.Models;

namespace LoLProject.WebApp.Clients;

public class TodoClient : ITodoClient
{
    private readonly HttpClient _httpClient;
    public TodoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TodoItem>> GetTodoItemsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<TodoItem>>("/api/todo");
    }

    public async Task<TodoItem> CreateTodoItemAsync(TodoItem item)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/todo", item);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TodoItem>();
    }
}