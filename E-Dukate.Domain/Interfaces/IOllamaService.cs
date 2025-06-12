using System;

namespace E_Dukate.Domain.Interfaces;

public interface IOllamaService
{
    Task<string> GetResponseAsync(string prompt);
}
