namespace IpAddressesAPI.IPDetails.Interfaces
{
    // Contract for the factory class that retrieves the details of a given IP address
    public interface IGetDetails
    {
        Task<(bool Success, string Result)> ExecuteAsync(System.Net.IPAddress address);
    }
}
