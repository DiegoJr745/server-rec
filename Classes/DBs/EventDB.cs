using System;
using LiteDB;
using sscs2023.Classes;
using sscs2023.Classes.DBs.DBClasses;
using static sscs2023.Classes.DBs.DBClasses.EventDBClasses;

namespace sscs2023.Classes.DBs
{
    public class EventDB
    {
        public static LiteDatabase EventDBFile = new LiteDatabase(Path.Join(Program.dataDir, "DBs", "Events.db"));
        public static readonly ILiteCollection<PlayerEvent> Events = EventDBFile.GetCollection<PlayerEvent>("PlayerEvents");

        public static PlayerEventResponse CreatePlayerEvent(long creatorPlayerId, PlayerEventRequest request)
        {
            try
            {
                int accessibility = request.Accessibility?.ToLower() switch
                {
                    "public" => 1,
                    "private" => 0,
                    _ => 1
                };

                var playerEvent = new PlayerEvent
                {
                    CreatorPlayerId = creatorPlayerId,
                    Name = request.Name ?? string.Empty,
                    Description = request.Description ?? string.Empty,
                    Accessibility = accessibility,
                    ClubId = request.ClubId,
                    RoomId = request.RoomId,
                    SubRoomId = request.SubRoomId,
                    StartTime = DateTime.Parse(request.StartTime),
                    EndTime = DateTime.Parse(request.EndTime),
                    ImageName = request.ImageName ?? string.Empty,
                    State = 0,
                    AttendeeCount = 0,
                    Tags = request.Tags ?? new List<string>(),
                    CreatedAt = DateTime.UtcNow
                };

                var inserted = Events.Insert(playerEvent);
                playerEvent.PlayerEventId = inserted.AsInt64;

                return new PlayerEventResponse
                {
                    PlayerEvent = playerEvent,
                    Result = 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EventDB] Error creating player event: {ex.Message}");
                return new PlayerEventResponse
                {
                    PlayerEvent = null,
                    Result = -1
                };
            }
        }

        public static List<PlayerEvent> GetPlayerEvents(long creatorPlayerId)
        {
            return Events.Find(x => x.CreatorPlayerId == creatorPlayerId).ToList();
        }

        public static PlayerEvent? GetPlayerEventById(long eventId)
        {
            return Events.FindById(eventId);
        }

        public static bool DeletePlayerEvent(long eventId, long creatorPlayerId)
        {
            var playerEvent = Events.FindById(eventId);
            if (playerEvent == null || playerEvent.CreatorPlayerId != creatorPlayerId)
                return false;

            return Events.Delete(eventId);
        }
    }
}
