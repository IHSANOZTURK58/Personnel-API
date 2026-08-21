using Microsoft.AspNetCore.Http;
using Minio;
using Minio.DataModel.Args;
using Microsoft.Extensions.Configuration;

namespace Simfer.PersonnelSystem.API.Services
{
    public class MinioService
    {
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName = "hatali-urunler";

       
        public MinioService(IConfiguration configuration)
        {
            _minioClient = new MinioClient()
                .WithEndpoint(configuration["Minio:Endpoint"])
                .WithCredentials(configuration["Minio:AccessKey"], configuration["Minio:SecretKey"])
                .Build();
        }

        // 1. FOTOĞRAF YÜKLEME METODU
        public async Task<string> UploadFileAsync(IFormFile file)
        {
            // Aynı isimde iki dosya yüklenip birbirini ezmesin diye rastgele eşsiz bir isim (Guid) üretiyoruz.
            string extension = Path.GetExtension(file.FileName);
            string newFileName = Guid.NewGuid().ToString() + extension;
                
            // Dosyayı MinIO'ya fırlatıyoruz
            using var stream = file.OpenReadStream();
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(newFileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            // Veritabanına (SQL) kaydetmek üzere sadece bu yeni dosya adını (Örn: 5f4d2...a3.jpg) geri dönüyoruz.
            return newFileName;
        }

        // 2. GÜVENLİ LİNK ÜRETME METODU (1 Saatlik Bilet)
        public async Task<string> GetFileUrlAsync(string fileName)
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry(60 * 60); // 60 saniye * 60 dakika = 1 Saat geçerli link!

            return await _minioClient.PresignedGetObjectAsync(args);
        }
    }
}