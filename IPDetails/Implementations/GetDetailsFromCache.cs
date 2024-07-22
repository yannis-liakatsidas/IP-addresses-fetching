using System.Net;
using System.Text.Json;
using IpAddressesAPI.Helpers;
using IpAddressesAPI.IPDetails.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace IpAddressesAPI.IPDetails.Implementations
{
    public class GetDetailsFromCache : IGetDetails
    {
        private readonly IMemoryCache _memoryCache;
        public GetDetailsFromCache(IMemoryCache memoryCache) 
        { 
            _memoryCache = memoryCache;
        }
        public Task<(bool Success, string Result)> ExecuteAsync(IPAddress address)
        {
            string result = string.Empty;

            // Try to find the value in the cache, and return a task of the result to align with the asynchronous method of the interface
            if (_memoryCache.TryGetValue(address, out CountryObject countryObject))
            {
                result = JsonSerializer.Serialize(countryObject);
                return Task.FromResult((true, result));
            }
            return Task.FromResult((false, result));
        }
    }
}
