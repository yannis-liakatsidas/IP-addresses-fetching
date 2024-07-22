using System.Text.Json;
using IpAddressesAPI.Helpers;
using IpAddressesAPI.IPDetails.Interfaces;
using IpAddressesAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IpAddressesAPI.IPDetails.Implementations
{
    public class GetDetailsFromDatabase : IGetDetails
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;

        public GetDetailsFromDatabase(IMemoryCache memoryCache, ApplicationDbContext context)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        public async Task<(bool Success, string Result)> ExecuteAsync(System.Net.IPAddress address)
        {
            string result = string.Empty;

            // Check if this IP exists in the database, also fetching the details of the associated country
            var ipRow = await _context.IPAddresses.Include(r => r.Country).FirstOrDefaultAsync(r => r.IP == address).ConfigureAwait(false);
            if (ipRow is not null)
            {
                var countryObject = new CountryObject();
                var country = ipRow.Country;
                countryObject.CountryTwoLetter = country.TwoLetterCode;
                countryObject.CountryThreeLetter = country.ThreeLetterCode;
                countryObject.CountryName = country.Name;

                // Add the IP in cache for future requests
                _memoryCache.Set(ipRow.IP, countryObject);

                result = JsonSerializer.Serialize(countryObject);
                return (true, result);
            }
            return (false, result);
        }
    }
}
