namespace IpAddressesAPI.Helpers
{
    // The result object for the GET requests of a specific IP address
    public class CountryObject
    {
        public string CountryName { get; set; } = null!;
        public string CountryTwoLetter { get; set; } = null!;
        public string CountryThreeLetter { get; set; } = null!;
    }

    // The type of data retrieval for the Factory method
    public enum DataSourceType
    {
        Cache = 0,
        Database = 1,
        External = 2
    }
}
