using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using static sscs2023.Classes.DBs.DBClasses.PlayerDBClasses;
using static sscs2023.Classes.DBs.DBClasses.RoomDBClasses;
using static sscs2023.Classes.ServerConfig;

namespace sscs2023.Classes
{
    public class Sessions
    {
        private static readonly Random _rng = new();

        public static Heartbeat? CreateRoom(long playerId, long roomId, long? subRoomId = null)
        {
            var roomData = RoomDB.GetRoom(roomId);
            if (roomData == null) 
                return null;

            SubRooms sub = null;

            // If subRoomId is specified, try to find it
            if (subRoomId.HasValue)
            {
                sub = roomData.SubRooms?.FirstOrDefault(s => s.SubRoomId == subRoomId.Value);
                if (sub == null)
                    return null; // SubRoom not found
            }
            else
            {
                // Default to first subroom if none specified
                sub = roomData.SubRooms?.FirstOrDefault();
            }

            if (sub == null) 
                return null;

            long instanceNumber = _rng.Next(1000, 999999999);
            var photonSuffix = "room";
            var roomName = roomData.Name ?? "UnknownRoom";
            
            var session = new RoomInstance {
                Name = $"^{roomName}",
                dataBlob = sub.DataBlob ?? "",
                isPrivate = false,
                location = sub.UnitySceneId,
                maxCapacity = sub.MaxPlayers,
                photonRegion = "us", photonRegionId = "us",
                photonRoomId = $"sscsRoom-{roomName}-{photonSuffix}",
                roomId = (long)roomData.RoomId,
                roomInstanceId = instanceNumber,
                subRoomId = (long)sub.SubRoomId
            };

            var newHeartbeat = PlayerDB.UpdatePlayerHeartbeat(playerId, session);
            return newHeartbeat;
        }

        public static Heartbeat CreateDorm(long playerId, string name)
        {
            long dormId = 1;
            var dorm = RoomDB.GetRoom(dormId);
            long instanceId = _rng.Next(1000, 999999999);
            
            var session = new RoomInstance 
            {
                isPrivate = isDormPrivate,
                location = "76d98498-60a1-430c-ab76-b54a29b7a163",
                maxCapacity = 4,
                Name = $"@{name}'s Dorm",
                photonRegion = "us", photonRegionId = "us", // eu region is laggy lol just cause im in us
                photonRoomId = $"sscsDorm-{instanceId}-room",
                roomId = dormId,
                roomInstanceId = instanceId,
                subRoomId = (long)(dorm?.SubRooms?.FirstOrDefault()?.SubRoomId ?? 0)
            };
            
            var newHeartbeat = PlayerDB.UpdatePlayerHeartbeat(playerId, session);
            return newHeartbeat;
        }
    }
}
