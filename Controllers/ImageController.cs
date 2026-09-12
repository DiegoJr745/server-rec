using Microsoft.AspNetCore.Mvc;
using static System.Net.Mime.MediaTypeNames;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace sscs2023.Controllers
{
	[ApiController]
    public class ImageController : ControllerBase
    {
        [Route("/imageserver/{*img_path}")]
        public async Task<IActionResult> ImgServer(string img_path, int width = 0, int height = 0, string sig = "p1")
        {
            img_path = Uri.UnescapeDataString(img_path ?? "").TrimStart('/');

            HttpContext.Response.Headers.Append("content-signature", "key-id=KEY:RSA:p1.rec.net; data=IWwe/pZ5vWWqNSkSM/54isgDxlZkdrP0sUrppKCbNktO2yCOTjq746xWiiLsueGuVcAGQqkjeRTimxolHckS/YXSYkEJxtiCXbLlsRia2DyAqtWVkGWsfczzFhp/56U66FVzolTspPCvjScOVlGO7dDIK7sJ+ndcRauWjsQsC6g3e7rUc6uwY099a6gy7sw6xr5BFZQSz8wg+fqyHYD/Sc4nQQVOTFZNNASqbJYhpNhEMXRnafCMuLl8a3mkGwvy3t4q2D/7SM48xrGZjEV47qNx1A91KCe28XVToFh4BzwEUU8nZ0d+KwV79MGarLo1cY8igc8FcoThKcovI4ClOg==");
            HttpContext.Response.Headers.Append("Content-Disposition", $"inline; filename=\"{Path.GetFileName(img_path)}\"");
            HttpContext.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            HttpContext.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, Cache-Control");
            HttpContext.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            Response.Headers["Cache-Control"] = "public, max-age=14400";

            var etag = $"\"{img_path.GetHashCode()}\"";
            HttpContext.Response.Headers.Append("ETag", etag);

            bool cropSquare = HttpContext.Request.Query.ContainsKey("cropsquare") &&
                (HttpContext.Request.Query["cropsquare"].ToString().ToLower() == "true" ||
                 HttpContext.Request.Query["cropsquare"].ToString() == "1");

            string baseImagesPath = Path.Join(Program.dataDir, "Images");
            string foundLocalPath = null;

            if (Directory.Exists(baseImagesPath))
            {
                string directPath = Path.Join(baseImagesPath, img_path);
                if (System.IO.File.Exists(directPath))
                {
                    foundLocalPath = directPath;
                }
                else
                {
                    var subDirectories = Directory.EnumerateDirectories(baseImagesPath, "*", SearchOption.AllDirectories);
                    foreach (var dir in subDirectories)
                    {
                        string testPath = Path.Join(dir, img_path);
                        if (System.IO.File.Exists(testPath))
                        {
                            foundLocalPath = testPath;
                            break;
                        }
                    }
                }
            }

            if (foundLocalPath != null)
            {
                try
                {
                    var imageBytes = await System.IO.File.ReadAllBytesAsync(foundLocalPath);
                    imageBytes = await ProcessImageAsync(imageBytes, cropSquare, width, height);
                    return File(imageBytes, "image/png");
                }
                catch
                {
                    var fallbackBytes = await System.IO.File.ReadAllBytesAsync(foundLocalPath);
                    return File(fallbackBytes, "image/png");
                }
            }

            string recNetLocalPath = Path.Join(baseImagesPath, "RecNet", img_path);

            try
            {
                using HttpClient client = new();
                byte[] data = await client.GetByteArrayAsync($"https://img.rec.net/{img_path}");

                Directory.CreateDirectory(Path.GetDirectoryName(recNetLocalPath)!);
                await System.IO.File.WriteAllBytesAsync(recNetLocalPath, data);

                try
                {
                    var processed = await ProcessImageAsync(data, cropSquare, width, height);
                    return File(processed, GetMimeType(img_path));
                }
                catch
                {
                    return File(data, GetMimeType(img_path));
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[rec_net fetch failed] {ex.Message}");
            }

            return NotFound();
        }

        private static string GetMimeType(string filePath)
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "image/png";
            }
            return contentType;
        }

        private static async Task<byte[]> ProcessImageAsync(byte[] imageBytes, bool cropSquare, int width, int height)
        {
            using var ms = new MemoryStream(imageBytes);
            using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(ms);

            if (cropSquare)
            {
                int size = Math.Min(image.Width, image.Height);
                int x = (image.Width - size) / 2;
                int y = (image.Height - size) / 2;
                image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, size, size)));

                int targetSize = width > 0 ? width : height;
                if (targetSize > 0)
                {
                    image.Mutate(ctx => ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(targetSize, targetSize),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.Lanczos3
                    }));
                }
            }
            else if (width > 0 || height > 0)
            {
                int resizeWidth = width;
                int resizeHeight = height;

                if (width > 0 && height == 0)
                {
                    resizeHeight = (int)((double)image.Height / image.Width * width);
                }
                else if (height > 0 && width == 0)
                {
                    resizeWidth = (int)((double)image.Width / image.Height * height);
                }

                image.Mutate(ctx => ctx.Resize(resizeWidth, resizeHeight));
            }

            using var output = new MemoryStream();
            await image.SaveAsync(output, new PngEncoder());
            return output.ToArray();
        }
    }
}