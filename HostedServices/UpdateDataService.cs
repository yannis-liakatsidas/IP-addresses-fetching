using IpAddressesAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace IpAddressesAPI.HostedServices
{
    public class UpdateDataService : BackgroundService, IDisposable
    {
        private const string _ip2cURL = "https://ip2c.org/";
        private readonly ILogger<UpdateDataService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _interval;
        private readonly IMemoryCache _memoryCache;

        public UpdateDataService(IServiceProvider serviceProvider, ILogger<UpdateDataService> logger, IMemoryCache memoryCache, TimeSpan interval)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _memoryCache = memoryCache;
            _interval = interval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Background task running.");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Retrieve all IP addresses and their associated country details from the database
                    var allAddresses = await dbContext.IPAddresses.Include(ip => ip.Country).ToListAsync(stoppingToken).ConfigureAwait(false);

                    // Create batches with each one having (at most) 100 IP addresses
                    var addressBatches = allAddresses.Chunk(100);
                    foreach (var batch in addressBatches)
                    {
                        foreach (var address in batch)
                        {
                            string url = Path.Combine([_ip2cURL, address.IP.ToString()]);
                            using (HttpClient client = new HttpClient())
                            {
                                try
                                {
                                    // Send GET request to IP2C web service
                                    HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);

                                    // Ensure the request was successful
                                    response.EnsureSuccessStatusCode();

                                    // Read response content as a string
                                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                                    var responseData = responseBody.Split(';');

                                    // Check if the response is valid according to IP2C format
                                    if (responseData[0] == "1" && responseData.Length == 4) 
                                    {
                                        string twoLetterCode = responseData[1];
                                        string threeLetterCode = responseData[2];
                                        string countryName = responseData[3];

                                        bool isTwoLetterCodeChanged = !string.Equals(address.Country.TwoLetterCode, twoLetterCode, StringComparison.OrdinalIgnoreCase);
                                        bool isThreeLetterCodeChanged = !string.Equals(address.Country.ThreeLetterCode, threeLetterCode, StringComparison.OrdinalIgnoreCase);

                                        if (isTwoLetterCodeChanged || isThreeLetterCodeChanged)
                                        {

                                            // Invalidate the cache for the specific item by deleting the old value
                                            _memoryCache.Remove(address);

                                            var hasCountry = dbContext.Countries.Any();
                                            Country? newCountry = null;
                                            if (hasCountry)
                                            {
                                                newCountry = dbContext.Countries.FirstOrDefault(r =>
                                                r.TwoLetterCode.ToLower() == twoLetterCode.ToLower()
                                                && r.ThreeLetterCode.ToLower() == threeLetterCode.ToLower());
                                            }

                                            // Check if the country exists; if not, create the correspind country and put it in the database
                                            if (newCountry is null)
                                            {
                                                var nameLength = countryName.Length < 50 ? countryName.Length : 50;

                                                newCountry = new Country()
                                                {
                                                    Name = countryName[..nameLength],
                                                    TwoLetterCode = twoLetterCode,
                                                    ThreeLetterCode = threeLetterCode,
                                                    CreatedAt = DateTime.Now
                                                };
                                            }

                                            // Register the new (current) time of update
                                            address.UpdatedAt = DateTime.Now;
                                            address.Country = newCountry;

                                            // Save all changes done in the database
                                            await dbContext.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"The following exception was thrown for IP address {address.IP}: {e.Message}.");
                                }
                            }
                        }
                    }
                }
                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
