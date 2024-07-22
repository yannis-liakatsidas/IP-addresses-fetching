using IpAddressesAPI.IPDetails.Interfaces;
using IpAddressesAPI.IPDetails.Implementations;
using IpAddressesAPI.Helpers;

namespace IpAddressesAPI.IPDetails.Factory
{

    public class IPDetailsFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public IPDetailsFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IGetDetails GetDetailsType(DataSourceType sourceType) 
        {
            return sourceType switch
            {
                DataSourceType.Cache => _serviceProvider.GetRequiredService<GetDetailsFromCache>(),
                DataSourceType.Database => _serviceProvider.GetRequiredService<GetDetailsFromDatabase>(),
                DataSourceType.External => _serviceProvider.GetRequiredService<GetDetailsFromExternalSource>(),
                _ => throw new ArgumentException("Invalid type", nameof(sourceType)),
            };
        }
    }
}
