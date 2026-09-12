using System;
using LiteDB;
using sscs2023.Classes;
using sscs2023.Classes.DBs.DBClasses;
using static sscs2023.Classes.DBs.DBClasses.InventionDBClasses;

namespace sscs2023.Classes.DBs
{
    public class InventionDB
    {
        public static LiteDatabase InventionDBFile = new LiteDatabase(Path.Join(Program.dataDir, "DBs", "Inventions.db"));
        public static readonly ILiteCollection<Invention> Inventions = InventionDBFile.GetCollection<Invention>("Inventions");
        public static readonly ILiteCollection<InventionVersion> Versions = InventionDBFile.GetCollection<InventionVersion>("Versions");

        public static SaveInventionResponse SaveInvention(long creatorPlayerId, SaveInventionRequest request)
        {
            try
            {
                long inventionId = GetNextInventionId();
                string replicationId = new Random().Next(int.MinValue, int.MaxValue).ToString();

                var invention = new Invention
                {
                    InventionId = inventionId,
                    CreatorPlayerId = creatorPlayerId,
                    Name = request.name ?? string.Empty,
                    Description = request.description ?? string.Empty,
                    ImageName = request.imageName ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                    CurrentVersionNumber = 1,
                    ReplicationId = replicationId
                };

                var version = new InventionVersion
                {
                    InventionId = inventionId,
                    VersionNumber = 1,
                    BlobName = request.inventionDataFilename ?? string.Empty,
                    ChipsCost = request.chipsCost,
                    CloudVariablesCost = request.cloudVariablesCost,
                    InstantiationCost = request.instantiationCost,
                    LightsCost = request.lightsCost,
                    ReplicationId = new Random().Next(int.MinValue, int.MaxValue).ToString()
                };

                Inventions.Insert(invention);
                Versions.Insert(version);

                return new SaveInventionResponse
                {
                    Invention = new InventionData
                    {
                        AllowTrial = invention.AllowTrial,
                        CheerCount = invention.CheerCount,
                        CreatedAt = invention.CreatedAt.ToString("O"),
                        CreatorPermission = invention.CreatorPermission,
                        CreatorPlayerId = invention.CreatorPlayerId,
                        CurrentVersionNumber = invention.CurrentVersionNumber,
                        Description = invention.Description,
                        GeneralPermission = invention.GeneralPermission,
                        HideFromPlayer = invention.HideFromPlayer,
                        ImageName = invention.ImageName,
                        InventionId = invention.InventionId,
                        IsAGInvention = invention.IsAGInvention,
                        IsCertifiedInvention = invention.IsCertifiedInvention,
                        IsPublished = invention.IsPublished,
                        ModifiedAt = invention.ModifiedAt.ToString("O"),
                        Name = invention.Name,
                        NumDownloads = invention.NumDownloads,
                        NumPlayersHaveUsedInRoom = invention.NumPlayersHaveUsedInRoom,
                        Price = invention.Price,
                        ReplicationId = invention.ReplicationId
                    },
                    InventionVersion = new InventionVersionData
                    {
                        BlobName = version.BlobName,
                        ChipsCost = version.ChipsCost,
                        CloudVariablesCost = version.CloudVariablesCost,
                        InstantiationCost = version.InstantiationCost,
                        InventionId = version.InventionId,
                        LightsCost = version.LightsCost,
                        ReplicationId = version.ReplicationId,
                        VersionNumber = version.VersionNumber
                    },
                    Status = 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InventionDB] Error saving invention: {ex.Message}");
                return new SaveInventionResponse { Status = -1 };
            }
        }

        public static Invention GetInvention(long inventionId)
        {
            return Inventions.FindById(inventionId);
        }

        public static List<Invention> GetInventionsBatch(List<long> inventionIds)
        {
            return Inventions.Find(x => inventionIds.Contains(x.InventionId)).ToList();
        }

        public static InventionVersion GetVersion(long inventionId, int versionNumber)
        {
            return Versions.FindOne(x => x.InventionId == inventionId && x.VersionNumber == versionNumber);
        }

        public static SetTagsResponse SetTags(long inventionId, SetTagsRequest request)
        {
            try
            {
                var invention = GetInvention(inventionId);
                if (invention == null)
                    return new SetTagsResponse { Result = -1, Tags = new List<string>() };

                invention.Tags.Clear();

                var allTags = new List<string>();

                foreach (var autoTag in request.AutoTags ?? new List<string>())
                {
                    invention.Tags.Add(new InventionTag { Tag = autoTag, Type = 0 });
                    allTags.Add(autoTag);
                }

                foreach (var customTag in request.CustomTags ?? new List<string>())
                {
                    invention.Tags.Add(new InventionTag { Tag = customTag, Type = 1 });
                    allTags.Add(customTag);
                }

                Inventions.Update(invention);

                return new SetTagsResponse
                {
                    Result = 0,
                    Tags = allTags
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InventionDB] Error setting tags: {ex.Message}");
                return new SetTagsResponse { Result = -1, Tags = new List<string>() };
            }
        }

        public static long GetNextInventionId()
        {
            if (Inventions.Count() == 0)
                return 5012352472715840656;

            var maxId = Inventions.Max(x => x.InventionId);
            return Convert.ToInt64(maxId) + 1;
        }
    }
}
