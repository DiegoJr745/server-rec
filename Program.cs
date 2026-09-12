using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;
using sscs2023;
using sscs2023.Classes;
using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;


namespace sscs2023
{
    public class Program
    {
        public static string dataDir = Path.Join(Environment.CurrentDirectory, "Data");

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            if (!Directory.Exists(dataDir))
            {
                Console.WriteLine($"Setting up data directory...");

                string[] foldersToCreate = new[]
                {
                    dataDir,
                    Path.Join(dataDir, "Images"),
                    Path.Join(dataDir, "Images", "PlayerImages"),
                    Path.Join(dataDir, "Images", "PolaroidImages"),
                    Path.Join(dataDir, "CDN", "DataBlobs"),
                    Path.Join(dataDir, "CDN", "InventionBlobs"),
                    Path.Join(dataDir, "CDN", "RoomBlobs"),
                    Path.Join(dataDir, "Imports"),
                    Path.Join(dataDir, "DBs")
                };

                foreach (var folder in foldersToCreate)
                {
                    Directory.CreateDirectory(folder);
                }

                Console.WriteLine($"Set up Data directory at {dataDir}");
            }

            builder.Services.AddControllers().AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            builder.Services.AddHttpLogging(options =>
            {
                options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
            });

            var app = builder.Build();

            app.UseHttpLogging();
            
            app.UseWebSockets();
            
            app.UseRouting();
            
            app.UseAuthorization();

            app.MapControllers();

            //await RoomDB.ImportRooms(Path.Join(dataDir, "Imports", "ImportRooms.json"));
            //await RoomDB.ClearRooms();


            app.Run("http://localhost:2059");
        }
    }
}