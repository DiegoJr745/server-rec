using System;
using LiteDB;

namespace sscs2023.Classes.DBs.DBClasses
{
    public class EventDBClasses
    {
        public class PlayerEvent
        {
            [BsonId]
            public long PlayerEventId { get; set; }
            public long CreatorPlayerId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int Accessibility { get; set; } = 1; // 1 = Public, etc.
            public long? ClubId { get; set; }
            public long RoomId { get; set; }
            public long? SubRoomId { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public string ImageName { get; set; } = string.Empty;
            public int State { get; set; } = 0; // 0 = Active, etc.
            public int AttendeeCount { get; set; } = 0;
            public List<string> Tags { get; set; } = new();
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        public class PlayerEventRequest
        {
            public string Accessibility { get; set; } = string.Empty;
            public long? ClubId { get; set; }
            public string Description { get; set; } = string.Empty;
            public string EndTime { get; set; } = string.Empty;
            public string ImageName { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public long RoomId { get; set; }
            public string StartTime { get; set; } = string.Empty;
            public long? SubRoomId { get; set; }
            public List<string> Tags { get; set; } = new();
        }

        public class PlayerEventResponse
        {
            public PlayerEvent PlayerEvent { get; set; }
            public int Result { get; set; } = 0; // 0 = Success
        }
    }
}
