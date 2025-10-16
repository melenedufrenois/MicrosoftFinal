using LoLProject.Persistence.Models;

namespace LoLProject.WebApp.Clients;

public interface ITodoClient
{
    Task<List<TodoItem>?> GetTodoItemsAsync();
    Task<TodoItem> CreateTodoItemAsync(TodoItem item);
}