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

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            string extension = Path.GetExtension(file.FileName);
            string newFileName = Guid.NewGuid().ToString() + extension;
                
            using var stream = file.OpenReadStream();
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(newFileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType);

            await _minioClient.PutObjectAsync(putObjectArgs);

            return newFileName;
        }
        public async Task<string> GetFileUrlAsync(string fileName)
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry(60 * 60); 

            return await _minioClient.PresignedGetObjectAsync(args);
        }
    }
}