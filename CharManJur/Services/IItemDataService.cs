using CharManJur.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface IItemDataService
{
    // === TEMPLATE METHODS ===
    Task<List<Item>> GetAllItemsAsync();

    Task<Item?> GetItemByIdAsync(int id);

    // Updated to use ItemCategory enum
    Task<List<Item>> GetItemsByCategoryAsync(ItemCategory? category);

    Task<List<Item>> GetFoundationItemsAsync();

    Task<List<Item>> GetPlayerCreatedItemsAsync();

    Task<List<Item>> QueryItemsAsync(ItemQueryCriteria criteria);

    Task<Item> CreateCustomItemAsync(CreateCustomItemRequest request);

    // === UPDATE METHODS ===
    Task<bool> UpdateItemAsync(Item item);

    // === DELETION METHODS ===
    Task<bool> DeleteItemAsync(int id);

    // == CUSTOM ITEM TRACKER ===
    Task<List<Item>> GetCustomItemsAsync();
}