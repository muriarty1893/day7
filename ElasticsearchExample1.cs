using System;
using System.Collections.Generic;
using System.IO;
using Nest;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace ElasticsearchExample2
{
    // BusinessOperation sınıfı, CSV dosyasındaki verilerin yapısını temsil eder
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
            // Elasticsearch bağlantı ayarlarını yapılandır
            var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
                .DefaultIndex("business_operations"); // Varsayılan indeks adı

            var client = new ElasticClient(settings);

            // Veriyi Elasticsearch'e yükle
            if (!client.Indices.Exists("business_operations").Exists) // İndeks var mı kontrol et
            {
                // İndeks oluştur ve BusinessOperation sınıfını haritalandır
                client.Indices.Create("business_operations", c => c
                    .Map<BusinessOperation>(m => m
                        .AutoMap()
                    )
                );

                // CSV dosyasını oku
                using (var reader = new StreamReader("business-operations-survey-2023-climate-change 1.csv"))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    // CSV verilerini BusinessOperation nesnelerine dönüştür
                    var records = csv.GetRecords<BusinessOperation>();
                    // Verileri Elasticsearch'e yükle
                    client.IndexMany(records);
                }
            }
            //CSV Dosyasının Doğru Yolu ve Verilerin Yüklendiğini Kontrol Etme:
            if (!client.Indices.Exists("business_operations").Exists)
            {
                client.Indices.Create("business_operations", c => c
                    .Map<BusinessOperation>(m => m.AutoMap()));

                using (var reader = new StreamReader("business-operations-survey-2023-climate-change 1.csv"))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var records = csv.GetRecords<BusinessOperation>();
                    var indexResponse = client.IndexMany(records);
                    if (!indexResponse.IsValid)
                    {
                        Console.WriteLine("Veriler Elasticsearch'e yüklenirken hata oluştu.");
                        return;
                    }
                }
            }
            /**
            // Basit bir arama yap
            var searchResponse = client.Search<BusinessOperation>(s => s
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f
                            .Field(p => p.Description)
                            .Field(p => p.Industry)
                            
                        )
                        .Query("Agriculture") // Aranacak kelimeyi buraya yazın
                    )
                )
            );**/
             //tüm alanalarda arama;
               var searchResponse = client.Search<BusinessOperation>(s => s
                        .Query(q => q
                            .QueryString(qs => qs
                                .Query("Agriculture")
                            )
                        )
                    );
            
             
             
             
            //Arama Sorgusunun Doğru Olduğunu Kontrol Etme:
            if (!searchResponse.IsValid)
            {
                Console.WriteLine("Arama sorgusu başarısız oldu.");
                return;
            }

            if (searchResponse.Hits.Count == 0)
            {
                Console.WriteLine("Arama sorgusuyla eşleşen sonuç bulunamadı.");
            }

            foreach (var hit in searchResponse.Hits)
            {
                Console.WriteLine($"{hit.Source.Description} - {hit.Source.Industry} - {hit.Source.Value}");
            }
            // Arama sonuçlarını yazdır
            foreach (var hit in searchResponse.Hits)
            {
                Console.WriteLine($"{hit.Source.Description} - {hit.Source.Industry} - {hit.Source.Value}");
            }
            
            //anında cmd den çıkmasın diye
            Console.WriteLine("Bir tuşa basın çıkmak için...");
            Console.ReadLine();
        }
    }
}
