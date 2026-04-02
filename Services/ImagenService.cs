using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace AMPAGestion.Services;

public class ImagenService
{
    private readonly AmazonS3Client _s3;
    private readonly string _bucket;
    private readonly string _endpointPublico;

    public ImagenService(IConfiguration config)
    {
        var keyId    = config["B2:KeyId"]     ?? throw new Exception("B2:KeyId no configurado");
        var appKey   = config["B2:AppKey"]    ?? throw new Exception("B2:AppKey no configurado");
        var endpoint = config["B2:Endpoint"]  ?? throw new Exception("B2:Endpoint no configurado");
        _bucket          = config["B2:Bucket"]    ?? throw new Exception("B2:Bucket no configurado");
        _endpointPublico = config["B2:PublicUrl"] ?? $"https://f003.backblazeb2.com/file/{_bucket}";

        Console.WriteLine($"[B2] KeyId: {keyId[..Math.Min(8, keyId.Length)]}...");
        Console.WriteLine($"[B2] AppKey length: {appKey.Length}");
        Console.WriteLine($"[B2] Bucket: {_bucket}");
        Console.WriteLine($"[B2] Endpoint: {endpoint}");

        var credentials = new BasicAWSCredentials(keyId, appKey);
        var s3Config = new AmazonS3Config
        {
            ServiceURL       = endpoint,
            ForcePathStyle   = true,
            SignatureVersion = "4"
        };
        _s3 = new AmazonS3Client(credentials, s3Config);
    }

    public async Task<string?> SubirImagenAsync(Stream stream, string nombreArchivo, string carpeta = "ampa")
    {
        try
        {
            var extension = Path.GetExtension(nombreArchivo);
            var key       = $"{carpeta}/{Guid.NewGuid():N}{extension}";
            var mime      = ObtenerMimeType(extension);

            Console.WriteLine($"[B2] Subiendo: {key} ({mime})");

            var request = new PutObjectRequest
            {
                BucketName  = _bucket,
                Key         = key,
                InputStream = stream,
                ContentType = mime,
                CannedACL   = S3CannedACL.PublicRead
            };

            var response = await _s3.PutObjectAsync(request);
            Console.WriteLine($"[B2] Respuesta HTTP: {(int)response.HttpStatusCode}");

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                var url = $"{_endpointPublico.TrimEnd('/')}/{key}";
                Console.WriteLine($"[B2] URL pública: {url}");
                return url;
            }

            Console.WriteLine($"[B2] Error: status {response.HttpStatusCode}");
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            Console.WriteLine($"[B2] AmazonS3Exception: {ex.StatusCode} - {ex.ErrorCode} - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[B2] Exception: {ex.GetType().Name} - {ex.Message}");
            return null;
        }
    }

    public async Task<bool> EliminarImagenAsync(string url)
    {
        try
        {
            var base_ = _endpointPublico.TrimEnd('/') + "/";
            if (!url.StartsWith(base_)) return false;
            var key = url[base_.Length..];
            await _s3.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucket, Key = key });
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[B2] Error eliminando: {ex.Message}");
            return false;
        }
    }

    private static string ObtenerMimeType(string ext) => ext.ToLower() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png"            => "image/png",
        ".gif"            => "image/gif",
        ".webp"           => "image/webp",
        ".pdf"            => "application/pdf",
        ".doc"            => "application/msword",
        ".docx"           => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls"            => "application/vnd.ms-excel",
        ".xlsx"           => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _                 => "application/octet-stream"
    };
}
