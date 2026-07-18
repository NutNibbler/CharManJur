using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomItemStorageService
{
    Task<List<Item>> LoadCustomItemsAsync();
    Task SaveCustomItemAsync(Item item);
    Task SaveCustomItemsAsync(List<Item> items);
    Task<bool> DeleteCustomItemAsync(int id);
    Task<string> GetStoragePathAsync();
}