using System;
using System.Linq;
using System.Text.Json;
using LiteDB;
using sscs2023.Classes;
using sscs2023.Classes.DBs.DBClasses;
using static sscs2023.Classes.DBs.DBClasses.RoomDBClasses;

namespace sscs2023.Classes.DBs
{
    public class RoomDB
    {
        public static LiteDatabase RoomDBFile = new LiteDatabase(Path.Join(Program.dataDir, "DBs", "Rooms.db"));
        public static readonly ILiteCollection<Room> Rooms = RoomDBFile.GetCollection<Room>("Rooms");

        public static async Task ImportRooms(string path) // everything in the next line will fix it and make it grab the importrooms.json from ../../Data/Imports/ImportRooms.json
            {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[DB] No import file found at {path}, skipping room import.");
                return;
            }
            try
            {
                string json = await File.ReadAllTextAsync(path);
                var importedRooms = System.Text.Json.JsonSerializer.Deserialize<List<Room>>(json);
                if (importedRooms != null)
                {
                    foreach (var room in importedRooms)
                    {
                        await AddRoom(room, log: false, shouldAssignNewIds: true);
                    }
                    Console.WriteLine($"[DB] Imported {importedRooms.Count} rooms from {path}.");
                }
                else
                {
                    Console.WriteLine($"[DB] No rooms found in import file at {path}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB Error] Failed to import rooms from {path}: {ex.Message}");
            }
        }

        public static async Task ClearRooms(bool log = false)
        {
            await Task.Run(() =>
            {
                try
                {
                    var dormRooms = Rooms.Find(r => r.IsDorm).ToList();
                    Rooms.DeleteAll();
                    foreach (var dorm in dormRooms)
                    {
                        Rooms.Insert(dorm);
                    }
                    if (log)
                    {
                        Console.WriteLine($"[DB] Cleared rooms, preserved {dormRooms.Count} dorm rooms.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DB Error] Failed to clear rooms: {ex.Message}");
                }
            });
        }

        public static async Task AddRoom(Room newRoom, bool log = false, bool shouldAssignNewIds = true)
        {
            if (newRoom == null) return;

            await Task.Run(() =>
            {
                try
                {
                    if (shouldAssignNewIds || newRoom.RoomId <= 0)
                    {
                        newRoom.RoomId = GetNextRoomId();
                    }

                    newRoom.RankedEntityId = newRoom.RoomId.ToString();

                    if (newRoom.SubRooms != null && newRoom.SubRooms.Any())
                    {
                        long currentMaxSubId = GetNextSubRoomId();
                        foreach (var sub in newRoom.SubRooms)
                        {
                            if (shouldAssignNewIds || sub.SubRoomId <= 0)
                            {
                                sub.SubRoomId = currentMaxSubId;
                                currentMaxSubId++;
                            }
                            sub.RoomId = newRoom.RoomId;
                        }
                    }

                    Rooms.Insert(newRoom);

                    if (log)
                    {
                        Console.WriteLine($"[DB] Added room: {newRoom.Name} (ID: {newRoom.RoomId})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DB Error] Failed to add room {newRoom.Name}: {ex.Message}");
                }
            });
        }

        public static long GetNextRoomId()
        {
            if (Rooms.Count() == 0) 
                return 1;
            
            var maxId = Rooms.Max(x => x.RoomId);

            return Convert.ToInt64(maxId) + 1;
        }

        public static long GetNextSubRoomId()
        {
            var allRooms = Rooms.FindAll().ToList();

            if (allRooms.Count == 0)
                return 1;

            long maxSubId = 0;
            foreach (var room in allRooms)
            {
                if (room.SubRooms != null && room.SubRooms.Any())
                {
                    long currentMax = room.SubRooms.Max(s => Convert.ToInt64(s.SubRoomId));
                    if (currentMax > maxSubId) maxSubId = currentMax;
                }
            }
            return maxSubId + 1;
        }
        
        public static Room GetRoom(long roomId)
        {
            return Rooms.FindById(roomId);
        }

        public static Room GetRoomByName(string name)
        {
            return Rooms.FindOne(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        
        public static List<Room> GetRoomsByNames(List<string> names)
        {
            if (names == null || names.Count == 0) 
                return new List<Room>();
            
            return Rooms.Find(room => 
                names.Contains(room.Name, StringComparer.OrdinalIgnoreCase)
            ).ToList();
        }
        
        public static (List<Room> Results, int Total) GetHotRooms(string tag, int skip, int take)
        {
            var query = Rooms.Query().Where(r => !r.IsDorm);
            string t = tag?.ToLower();
            bool isRRO = (t == "rro" || t == "recroomoriginal");

            if (isRRO) 
            {
                query = query.Where("Tags[*].Tag ANY IN ['rro', 'recroomoriginal']");
            }
            else if (t != "new") 
            {
                query = query.Where("Tags[*].Tag ALL NOT IN ['base', 'rro', 'recroomoriginal']");
            }

            if (!isRRO) 
            {
                query = query.Where(r => r.Accessibility == RoomAccessibility.Public);
            }

            var finalQuery = (t == "new") 
                ? query.OrderByDescending(r => r.CreatedAt) 
                : query.OrderByDescending(r => r.Stats.VisitCount);

            var results = finalQuery.Skip(skip).Limit(take).ToList();
            int total = finalQuery.Count();

            return (results, total);
        }
        
        public static void IncrementRoomVisitCount(long roomId)
        {
            var room = Rooms.FindById(roomId);
            if (room != null)
            {
                room.Stats.VisitCount++;
                Rooms.Update(room);
            }
        }

        public static readonly ILiteCollection<SubRoomSave> SubRoomSaves = RoomDBFile.GetCollection<SubRoomSave>("SubRoomSaves");

        public static (List<SubRoomSave> Results, int TotalResults) GetSubRoomSaves(
        long roomId,
        long subRoomId,
        int skip,
        int take,
        string? search = null)
        {
            take = Math.Min(take, 9999);

            var query = SubRoomSaves.FindAll()
                .Where(s => s.RoomId == roomId && s.SubRoomId == subRoomId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLowerInvariant();

                query = query.Where(s =>
                    (s.DataBlob ?? "").ToLower().Contains(q) ||
                    (s.Description ?? "").ToLower().Contains(q) ||
                    (s.Tags != null && s.Tags.Any(t => t.ToLower().Contains(q)))
                );
            }

            var ordered = query.OrderByDescending(s => s.CreatedAt);

            var results = ordered
                .Skip(skip)
                .Take(take)
                .ToList();

            return (results, ordered.Count());
        }

        public static long GetNextSubRoomSaveId()
        {
            if (SubRoomSaves.Count() == 0)
                return 1;

            return SubRoomSaves.Max(x => x.SubRoomDataSaveId) + 1;
        }

        public static List<Room> GetBaseRooms()
        {
            return Rooms.FindAll()
                .Where(room =>
                    System.Text.Json.JsonSerializer.Serialize(room)
                        .Contains("base", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
        }

        public static void IncrementRoomCheerCount(long roomId)
        {
            var room = Rooms.FindById(roomId);
            if (room != null)
            {
                room.Stats.CheerCount++;
                Rooms.Update(room);
            }
        }
        
        public static void DecrementRoomCheerCount(long roomId)
        {
            var room = Rooms.FindById(roomId);
            if (room != null && room.Stats.CheerCount > 0)
            {
                room.Stats.CheerCount--;
                Rooms.Update(room);
            }
        }
        
        public static void IncrementRoomFavoriteCount(long roomId)
        {
            var room = Rooms.FindById(roomId);
            if (room != null)
            {
                room.Stats.FavoriteCount++;
                Rooms.Update(room);
            }
        }
        
        public static void DecrementRoomFavoriteCount(long roomId)
        {
            var room = Rooms.FindById(roomId);
            if (room != null && room.Stats.FavoriteCount > 0)
            {
                room.Stats.FavoriteCount--;
                Rooms.Update(room);
            }
        }
    }
}