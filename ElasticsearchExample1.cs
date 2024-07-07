using System;
using System.Collections.Generic;
using System.IO;
using Nest;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace ElasticsearchExample2
{
    // Build sınıfı, CSV dosyasındaki verilerin yapısını temsil eder
    public class Build
    {
        [Name("Build")]
        public string BuildName { get; set; }

        [Name("Ascendancy")]
        public string Ascendancy { get; set; }

        [Name("Açıklama")]
        public string Description { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Elasticsearch bağlantı ayarlarını yapılandır
                var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
                    .DefaultIndex("builds") // Varsayılan indeks adı
                    .DisableDirectStreaming(); // Hata ayıklama bilgilerini görmek için

                var client = new ElasticClient(settings);

                // İndeks var mı kontrol et
                if (!client.Indices.Exists("builds").Exists)
                {
                    // İndeks oluştur ve Build sınıfını haritalandır
                    client.Indices.Create("builds", c => c
                        .Map<Build>(m => m
                            .AutoMap()
                            .Properties(ps => ps
                                .Text(t => t
                                    .Name(n => n.BuildName)
                                    .Analyzer("standard")
                                )
                                .Text(t => t
                                    .Name(n => n.Ascendancy)
                                    .Analyzer("standard")
                                )
                                .Text(t => t
                                    .Name(n => n.Description)
                                    .Analyzer("standard")
                                )
                            )
                        )
                    );
                }

                // CSV dosyasını oku
                using (var reader = new StreamReader("buildler2.csv"))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null, // Başlık doğrulamasını devre dışı bırak
                    MissingFieldFound = null, // Eksik alan bulunursa hata vermemek için
                }))
                {
                    // CSV verilerini Build nesnelerine dönüştür
                    var records = csv.GetRecords<Build>();
                    // Verileri Elasticsearch'e yükle
                    var indexResponse = client.IndexMany(records);

                    if (!indexResponse.IsValid)
                    {
                        Console.WriteLine("Veriler Elasticsearch'e yüklenirken hata oluştu.");
                        Console.WriteLine(indexResponse.DebugInformation);  // Detaylı hata bilgilerini yazdırır
                        foreach (var item in indexResponse.ItemsWithErrors)
                        {
                            Console.WriteLine($"Hata: {item.Error}");
                        }
                        return;
                    }
                }

                // Basit bir arama yap
                var searchResponse = client.Search<Build>(s => s
                    .Query(q => q
                        .QueryString(qs => qs
                            .Query("reap") // Aranacak kelimeyi buraya yazın
                        )
                    )
                );

                // Arama Sorgusunun Doğru Olduğunu Kontrol Etme
                if (!searchResponse.IsValid)
                {
                    Console.WriteLine("Arama sorgusu başarısız oldu.");
                    Console.WriteLine(searchResponse.DebugInformation);  // Hata bilgilerini yazdırır
                    return;
                }

                if (searchResponse.Hits.Count == 0)
                {
                    Console.WriteLine("Arama sorgusuyla eşleşen sonuç bulunamadı.");
                }
                else
                {
                    foreach (var hit in searchResponse.Hits)
                    {
                        Console.WriteLine($"{hit.Source.BuildName} - {hit.Source.Ascendancy} - {hit.Source.Description}");
                    }
                }

                // Anında cmd den çıkmasın diye
                Console.WriteLine("Bir tuşa basın çıkmak için...");
                Console.ReadLine(); // Programın kapanmasını engellemek için eklenen satır
            }
            catch (Exception ex)
            {
                // Hata ayıklama
                Console.WriteLine($"Beklenmeyen bir hata oluştu: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
