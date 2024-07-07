using System;
using System.Collections.Generic;
using System.IO;
using Nest;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace ElasticsearchExample2
{
    public class BusinessOperation
    {
        public string Description { get; set; }
        public string Industry { get; set; }
        public string Level { get; set; }
        public string Size { get; set; }
        public string Line_Code { get; set; }
        public int? Value { get; set; }
        public string Unit { get; set; }
        public string Footnotes { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
                .DefaultIndex("business_operations");

            var client = new ElasticClient(settings);

            // Veriyi Elasticsearch'e yükle
            if (!client.Indices.Exists("business_operations").Exists)
            {
                client.Indices.Create("business_operations", c => c
                    .Map<BusinessOperation>(m => m
                        .AutoMap()
                    )
                );

                using (var reader = new StreamReader("business-operations-survey-2023-climate-change 1.csv"))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var records = csv.GetRecords<BusinessOperation>();
                    client.IndexMany(records);
                }
            }

            // Basit bir arama yap
            var searchResponse = client.Search<BusinessOperation>(s => s
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Description)
                        .Query("bussiness")
                    )
                )
            );

            // Arama sonuçlarını yazdır
            foreach (var hit in searchResponse.Hits)
            {
                Console.WriteLine($"{hit.Source.Description} - {hit.Source.Industry} - {hit.Source.Value}");
            }
        }
    }
}