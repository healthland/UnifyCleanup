using RemoveDuplicateIdentifier.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;

namespace RemoveDuplicateIdentifier.Services
{
    public interface IRemoveDuplicateIdService
    {      
        Task Execute(CommandOptions commandLineOptions, CancellationToken source);
    }

    public class RemoveDuplicateIdService : IRemoveDuplicateIdService
    {
        private readonly ILogger<RemoveDuplicateIdService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public RemoveDuplicateIdService(ILogger<RemoveDuplicateIdService> logger,
                        IConfiguration configuration,
                        IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task Execute(CommandOptions commandLineOptions, CancellationToken cancellationToken)
        {
            try
            {
                //Get the server connection
                using var orgConn = new NpgsqlConnection(_configuration.GetSection($"ConnectionStrings:{commandLineOptions.Environment}").Value);
                string sql = $"select \"DBConnections\".\"DBConnectionID\", \"DBConnections\".\"DBConnectionString\" from public.\"DBConnections\"  Order by \"DBConnectionID\"";


                orgConn.Open();
                using var orgCommand = orgConn.CreateCommand();
                orgCommand.CommandText = sql;
                using var orgReader = await orgCommand.ExecuteReaderAsync(cancellationToken);
                List<DbConnections> dbConnections = new List<DbConnections>();
                if (orgReader.HasRows)
                {
                    while (orgReader.Read())
                    {
                        dbConnections.Add(new DbConnections()
                        {
                            DBConnectionId = Convert.ToInt32(orgReader["DBConnectionId"].ToString()),
                            ConnectionString = orgReader["DBConnectionString"].ToString()
                        });
                    }
                }

                List<IMDuplicates> imDuplicates = new List<IMDuplicates>();
                foreach (var dbconn in dbConnections)
                {
                    using var conn = new NpgsqlConnection(dbconn.ConnectionString + ";Trust Server Certificate=true;");

                    string dupFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "DuplicateInput.json");
                    string data = File.ReadAllText(dupFilePath);
                    List<DuplicateInput> duplicateInput = JsonConvert.DeserializeObject<List<DuplicateInput>>(data);
                    var itemList = duplicateInput.Where(d => d.DBConnectionId == dbconn.DBConnectionId.ToString()).ToList();
                    foreach (var item in itemList)
                    {
                        var OrganizationId = item.Organization.ToString();
                        var partitionTable = "TPatientInfo_" + item.OrganizationId.ToString();
                        conn.Open();
                        using var command = conn.CreateCommand();
                        sql = $"SELECT   \"IdentifierSystem\", \"IdentifierValue\", \"PartitionKey\", COUNT(DISTINCT \"RecordId\") \r\nFROM public.\"{partitionTable}\"\r\nWhere \"IdentifierValue\" like 'id-%'\r\ngroup by  \"PartitionKey\", \"IdentifierSystem\", \"IdentifierValue\"\r\nHAVING COUNT(DISTINCT \"RecordId\") > 1\r\nORDER BY  \"PartitionKey\" ASC";
                       
                        command.CommandText = sql;
                        using var partreader = await command.ExecuteReaderAsync(cancellationToken);

                        if (partreader.HasRows)
                        {
                            while (partreader.Read())
                            {
                                var imDup = new IMDuplicates();
                                imDup.OrganizationId = OrganizationId;
                                imDup.IdentifierValue = partreader["IdentifierValue"].ToString();
                                imDup.Count = partreader["count"].ToString();
                                imDuplicates.Add(imDup);
                            }
                        }
                                                                        
                            conn.Close();
                        }                    
                }
                //  Write to IMDuplicates.json
                string imdupPath = Path.Combine(Directory.GetCurrentDirectory(), "Files", "IMDuplicates.json");
                File.WriteAllText(imdupPath, JsonConvert.SerializeObject(imDuplicates, Formatting.Indented));
            }


            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

        }
    }
}
