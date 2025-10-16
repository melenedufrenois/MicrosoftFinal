using LoLProject.Persistence.Models;

namespace LoLProject.WebApp.Clients;

public interface ITodoClient
{
    Task<List<TodoItem>?> GetTodoItemsAsync();
    Task<TodoItem> CreateTodoItemAsync(TodoItem item);
    Task<TodoItem?> UpdateTodoItemAsync(int id, TodoItem item);
    Task DeleteTodoItemAsync(int id);
}