using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace AMPAGestion.Services;

public class ImagenService
{
    private readonly Cloudinary _cloudinary;

    public ImagenService(IConfiguration config)
    {
        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]);
        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string?> SubirImagenAsync(Stream stream, string nombreArchivo, string carpeta = "ampa")
    {
        var parametros = new ImageUploadParams
        {
            File           = new FileDescription(nombreArchivo, stream),
            Folder         = carpeta,
            Transformation = new Transformation()
                .Width(1200).Height(1200)
                .Crop("limit")
                .Quality("auto")
                .FetchFormat("auto")
        };

        var resultado = await _cloudinary.UploadAsync(parametros);

        if (resultado.Error != null)
            return null;

        return resultado.SecureUrl?.ToString();
    }

    public async Task<bool> EliminarImagenAsync(string url)
    {
        try
        {
            // Extraer el public_id de la URL (incluye la carpeta)
            var uri    = new Uri(url);
            var partes = uri.AbsolutePath.Split('/');
            // La URL tiene formato: /image/upload/v.../carpeta/nombre.ext
            var uploadIdx = Array.IndexOf(partes, "upload");
            if (uploadIdx < 0) return false;

            // Saltar versión (v12345...) si existe
            var desde = uploadIdx + 1;
            if (desde < partes.Length && partes[desde].StartsWith("v"))
                desde++;

            var publicId = string.Join("/",
                partes[desde..].Select(p => p.Contains('.') ? p[..p.LastIndexOf('.')] : p));

            var resultado = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            return resultado.Result == "ok";
        }
        catch
        {
            return false;
        }
    }
}
