using System.Data;
using System.Data.SqlClient;
using IpAddressesAPI.Helpers;
using IpAddressesAPI.IPDetails.Factory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IpAddressesAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IPAddressesController : ControllerBase
    {
        private readonly IPDetailsFactory _factory;
        private readonly IConfiguration _configuration;
        public IPAddressesController(IPDetailsFactory factory, IConfiguration configuration)
        {
            _factory = factory;
            _configuration = configuration;
        }

        [HttpGet("{IPInput}")]
        public async Task<IActionResult> GetCountryDetailsAsync(string IPInput)
        {
            if (!System.Net.IPAddress.TryParse(IPInput, out var address))
            {
                return base.BadRequest(new {Message = "The IP address is not valid."} );
            }

            // Using the factory design pattern to get the details of the IP requested
            
            // Attempt to get the IP details from the cache
            var result = await _factory.GetDetailsType(DataSourceType.Cache).ExecuteAsync(address);

            // If the cache lookup fails, try to get the details from the database
            if (!result.Success)
            {
                result = await _factory.GetDetailsType(DataSourceType.Database).ExecuteAsync(address);

                // If the database lookup also fails, try to get the details from the IP2C web service
                if (!result.Success)
                    result = await _factory.GetDetailsType(DataSourceType.External).ExecuteAsync(address);
            }

            if (!result.Success)
            {
                return NotFound(new { Message = result.Result });
            }

            return Ok(result.Result);
        }

        [HttpGet("countries-report")]
        public async Task<IActionResult> GetAddressesPerCountryAsync([FromQuery] string[]? twoLetterCodes)
        {
            // The main query body without the where clause and grouping
            string sqlQuery = @"SELECT Countries.[Name] AS CountryName, 
            COUNT(IPAddresses.Id) AS AddressesCount,
            MAX(IPAddresses.UpdatedAt) AS LastAddressUpdated
            FROM IPAddresses
            INNER JOIN Countries on Countries.Id = IPAddresses.CountryId";

            List<SqlParameter> parameters = new List<SqlParameter>();

            // Check for existence of two letter codes as parameters
            if (twoLetterCodes is not null && twoLetterCodes.Length > 0)
            {

                // Strict policy: Even if one code is wrong, return an error message so as to assure that the results will be the wanted ones
                if (twoLetterCodes.Any(r => r.Length != 2))
                    return StatusCode(500, new { Message = "There is one or more input with wrong formats. Please correct them and try again." });

                var countriesLength = twoLetterCodes.Length;

                // Add each parameter in the SqlParameter list
                for (int i = 0; i < countriesLength; i++)
                {
                    parameters.Add(new SqlParameter($"@Country{i}", SqlDbType.NVarChar) { Value = twoLetterCodes[i] });
                }
                string[] paramNames = parameters.Select(x => x.ParameterName).ToArray();

                // Complete the where clause of the query
                sqlQuery += $"\r\nWHERE Countries.TwoLetterCode IN ({string.Join(",", paramNames)})";
            }

            // Complete with the grouping to execute the count appropriately
            sqlQuery += "\r\nGROUP BY CountryId, Countries.[Name]";
            
            // Get the connection string as declared in the appsettings.json configuration file
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (connectionString is null)
                return StatusCode(500, new { Message = "Connection string is not declared." });

            IActionResult result;

            try
            {
                var resultTable = await ExecuteQueryAsync(connectionString, sqlQuery, parameters.ToArray()).ConfigureAwait(false);
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(resultTable);
                result = Ok(json);
            }
            catch (Exception ex)
            {
                result = StatusCode(500, new { Message = "An error occurred while executing the query.", Details = ex.Message });
            }
            return result;
        }

        #region helpers
        private static async Task<DataTable> ExecuteQueryAsync(string connectionString, string sqlQuery, SqlParameter[]? parameters = null)
        {
            DataTable dataTable = new DataTable();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    if (parameters is not null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    // Load a DataTable with the results of the query execution
                    using (SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        dataTable.Load(reader);
                    }
                }
            }
            return dataTable;

        }
        #endregion
    }
}
