using CharManJur.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CharManJur.Services;

public interface ICustomQuipStorageService
{
    Task<List<Quip>> LoadCustomQuipsAsync();
    Task SaveCustomQuipAsync(Quip quip);
    Task SaveCustomQuipsAsync(List<Quip> quips);
    Task<bool> DeleteCustomQuipAsync(Guid guid);
    Task<string> GetStoragePathAsync();
}