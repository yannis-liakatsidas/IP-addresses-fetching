using System.Text.Json;
using IpAddressesAPI.Helpers;
using IpAddressesAPI.IPDetails.Interfaces;
using IpAddressesAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace IpAddressesAPI.IPDetails.Implementations
{
    public class GetDetailsFromExternalSource : IGetDetails
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ApplicationDbContext _context;
        private const string _ip2cURL = "https://ip2c.org/";

        public GetDetailsFromExternalSource(IMemoryCache memoryCache, ApplicationDbContext context)
        {
            _memoryCache = memoryCache;
            _context = context;
        }

        public async Task<(bool Success, string Result)> ExecuteAsync(System.Net.IPAddress address)
        {
            string url = Path.Combine([_ip2cURL, address.ToString()]);
            bool success;
            string result = string.Empty;
            CountryObject countryResult = new CountryObject();
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Send GET request to IP2C web service
                    HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);

                    // Check if the request was successful
                    if (!response.IsSuccessStatusCode)
                    {
                        result = $"Information for address {address} were not found in IP2C web service. Please check for any errors and try again.";
                        return (false, result);
                    }

                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var responseData = responseBody.Split(';');
                    if (responseData[0] == "1" && responseData.Length == 4)
                    {
                        string twoLetterCode = responseData[1];
                        string threeLetterCode = responseData[2];
                        string countryName = responseData[3];

                        if (!(twoLetterCode.Length == 2 && threeLetterCode.Length == 3))
                        {
                            result = "The country codes that IP2C returned have wrong size";
                            return (false, result);
                        }

                        // Check if the database currently has any countries
                        var hasCountry = _context.Countries.Any();
                        Country? country = null;

                        if (hasCountry)
                        {
                            country = _context.Countries.FirstOrDefault(r =>
                            r.TwoLetterCode.ToLower() == twoLetterCode.ToLower()
                            && r.ThreeLetterCode.ToLower() == threeLetterCode.ToLower());
                        }

                        if (country is null)
                        {
                            // Constraint to map the nvarchar(50) SQL data type
                            var nameLength = countryName.Length < 50 ? countryName.Length : 50;
                            country = new Country()
                            {
                                TwoLetterCode = twoLetterCode,
                                ThreeLetterCode = threeLetterCode,
                                Name = countryName[..nameLength],
                                CreatedAt = DateTime.Now
                            };
                            await _context.Countries.AddAsync(country).ConfigureAwait(false);
                        }

                        // Persist information in the database
                        System.Net.IPAddress ip = address;
                        var creationDate = DateTime.Now;
                        Models.IPAddress newIPAddress = new Models.IPAddress()
                        {
                            IP = ip,
                            Country = country,
                            CreatedAt = creationDate, // Make sure the two dates have the exact same value
                            UpdatedAt = creationDate
                        };

                        await _context.AddAsync(newIPAddress).ConfigureAwait(false);

                        // Save all changes done to the database
                        await _context.SaveChangesAsync();

                        countryResult.CountryTwoLetter = twoLetterCode;
                        countryResult.CountryThreeLetter = threeLetterCode;
                        countryResult.CountryName = countryName;

                        //add in cache
                        _memoryCache.Set(ip, countryResult);

                        success = true;
                        result = JsonSerializer.Serialize(countryResult);
                    }
                    else
                    {
                        success = false;
                        result = $"Error communicating with IP2C web service. Response from the server: {response}";
                    }
                }
                catch (Exception e)
                {
                    success = false;
                    result = e.Message;
                }
                return (success, result);
            }
        }
    }
}