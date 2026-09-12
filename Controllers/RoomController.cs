using LiteDB;
using Microsoft.AspNetCore.Mvc;
using sscs2023.Auth;
using sscs2023.Classes;
using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using sscs2023.Classes.Rooms;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using static sscs2023.Classes.DBs.DBClasses.RoomDBClasses;

namespace sscs2023.Controllers
{
    [ApiController]
    [Route("/roomserver")]
    public class RoomController : ControllerBase
    {
        [HttpGet("rooms")]
        public IActionResult GetRoomBy([FromQuery] string? name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var room = RoomDB.GetRoomByName(name);
                return room != null ? Ok(room) : NotFound();
            }

            return NotFound();
        }

        [HttpGet("rooms/ownedby/{accountId}")]
        public IActionResult GetRoomsOwnedBy(long accountId)
        {
            var rooms = RoomDB.Rooms
                .Find(r => r.CreatorAccountId == accountId)
                .ToList();

            return Ok(rooms);
        }

        [HttpGet("rooms/search")]
        public IActionResult SearchRooms(string query, int skip = 0, int take = 100)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query required.");

            var parts = query.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var allRooms = RoomDB.Rooms.FindAll().ToList();

            int? platformId = null;

            var heartbeat = HttpContext.Items["Heartbeat"] as dynamic;
            if (heartbeat != null)
            {
                try { platformId = (int?)heartbeat.PlatformId; } catch { }
            }

            bool requiresScreens =
                platformId == 0 || platformId == 1 || platformId == 4 ||
                platformId == 8 || platformId == 16 || platformId == 32 ||
                platformId == 64 || platformId == -1;

            var exactRooms = new List<string>();
            var tagFilters = new List<string>();
            var scoredTerms = new List<string>();

            foreach (var raw in parts)
            {
                var part = raw.Trim();
                if (string.IsNullOrEmpty(part)) continue;

                if (part.StartsWith("^"))
                {
                    exactRooms.Add(part[1..].Trim().ToLowerInvariant());
                    continue;
                }

                if (part.StartsWith("#"))
                {
                    var split = part[1..].Trim().ToLowerInvariant()
                        .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                    tagFilters.Add(split[0]);

                    if (split.Length > 1)
                        scoredTerms.Add(split[1]);

                    continue;
                }

                scoredTerms.Add(part.ToLowerInvariant());
            }

            var working = allRooms.Where(r =>
                r.Accessibility == RoomDBClasses.RoomAccessibility.Public
            );

            if (requiresScreens)
            {
                working = working.Where(r => r.SupportsScreens);
            }

            if (exactRooms.Count > 0)
            {
                working = working.Where(r =>
                    exactRooms.Contains((r.Name ?? "").ToLowerInvariant())
                );
            }

            if (tagFilters.Count > 0)
            {
                working = working.Where(r =>
                    r.Tags != null &&
                    tagFilters.All(tf =>
                        r.Tags.Any(t => (t.Tag ?? "").ToLowerInvariant().Contains(tf))
                    )
                );
            }

            var results = working
                .Select(room =>
                {
                    string name = (room.Name ?? "").ToLowerInvariant();
                    string desc = (room.Description ?? "").ToLowerInvariant();
                    var tags = room.Tags?.Select(t => (t.Tag ?? "").ToLowerInvariant()).ToList()
                               ?? new List<string>();

                    int score = 0;

                    foreach (var q in scoredTerms)
                    {
                        if (string.IsNullOrWhiteSpace(q)) continue;

                        if (name == q) score += 1000;

                        if (name.StartsWith(q)) score += 500;

                        if (name.Contains(q)) score += 200;

                        if (desc.Contains(q)) score += 50;

                        if (tags.Any(t => t.Contains(q)))
                            score += 10;

                        score -= Math.Abs(name.Length - q.Length);
                    }

                    return new { room, score };
                })
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .ThenBy(x => x.room.Name.Length)
                .Skip(skip)
                .Take(take)
                .Select(x => x.room)
                .ToList();

            return Ok(new
            {
                TotalResults = results.Count,
                Results = results
            });
        }

        [HttpPost("rooms/{roomId}/clone")]
        public IActionResult CloneRoom(long roomId, [FromForm] CloneRoomRequest body)
        {
            var original = RoomDB.GetRoom(roomId);
            if (original == null)
                return NotFound("Room not found");

            if (body == null || string.IsNullOrWhiteSpace(body.Name))
                return BadRequest("Missing name");

            long? currentAccountId = AuthStuff.GetPlayerId(HttpContext.Request);
            if (currentAccountId == null)
                return Unauthorized();

            long newId = original.RoomId + 1;
            while (RoomDB.Rooms.Exists(x => x.RoomId == newId))
            {
                newId++;
            }

            var json = System.Text.Json.JsonSerializer.Serialize(original);
            var clone = System.Text.Json.JsonSerializer.Deserialize<Room>(json);

            if (clone == null)
                return StatusCode(500, "Clone failed");

            clone.RoomId = newId;
            clone.Name = body.Name;
            clone.CreatedAt = DateTime.UtcNow;
            clone.RankedEntityId = newId.ToString();
            clone.ImageName = "DefaultRoomImage.png";
            clone.Accessibility = RoomAccessibility.Private;
            clone.CloningAllowed = false;
            clone.IsRRO = false;
            clone.IsDeveloperOwned = false;
            clone.PromoImages = null;
            clone.Stats.CheerCount = 0;
            clone.Stats.FavoriteCount = 0;
            clone.Stats.VisitCount = 0;

            clone.CreatorAccountId = currentAccountId.Value;

            clone.Tags = (clone.Tags ?? new List<Tags>())
                .Where(t =>
                    !string.Equals(t.Tag, "base", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(t.Tag, "rro", StringComparison.OrdinalIgnoreCase)
                )
                .ToList();

            if (clone.SubRooms != null)
            {
                long subId = RoomDB.GetNextSubRoomId();

                foreach (var sub in clone.SubRooms)
                {
                    sub.SubRoomId = subId++;
                    sub.RoomId = newId;
                    sub.SavedByAccountId = currentAccountId.Value;
                }
            }

            clone.Roles = new List<Roles>
    {
        new Roles
        {
            AccountId = currentAccountId.Value,
            Role = Role.Creator,
            InvitedRole = Role.Creator
        }
    };

            RoomDB.Rooms.Insert(clone);

            var cloneJson = System.Text.Json.JsonSerializer.SerializeToElement(clone);
            var merged = new Dictionary<string, object>();

            foreach (var prop in cloneJson.EnumerateObject())
            {
                merged[prop.Name] = prop.Value;
            }

            merged["success"] = true;
            merged["value"] = clone;
            merged["error_id"] = null;
            merged["error"] = null;

            return Ok(merged);
        }

        [HttpGet("rooms/ownedby/me")]
        public IActionResult GetCreatedByMe(int skip = 0, int take = 9999)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();

            long id = Convert.ToInt64(playerId);

            var query = RoomDB.Rooms.FindAll().Where(r => r.CreatorAccountId == id);

            var results = query
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToList();

            return Ok(results);
        }

        [HttpGet("rooms/base")]
        public IActionResult GetBaseRooms()
        {
            var rooms = RoomDB.Rooms
                .FindAll()
                .Where(r => r.Tags != null &&
                            r.Tags.Any(t => t.Tag.Equals("base", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Ok(rooms);
        }

        [HttpGet("rooms/visitedby/me")]
        public IActionResult GetVisitedByMe(int skip = 0, int take = 9)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();

            var results = RoomDB.Rooms
                .FindAll()
                .Select(room =>
                {
                    var lastVisited = PlayerDB.GetRoomLastVisitedAt((long)playerId, room.RoomId);
                    return lastVisited == null ? null : new { room, lastVisited };
                })
                .Where(x => x != null)
                .OrderByDescending(x => x.lastVisited)
                .Skip(skip)
                .Take(take)
                .Select(x => x.room)
                .ToList();

            return Ok(results);
        }

        [HttpGet("rooms/{roomId}")]
        public IActionResult GetRoomById(long roomId)
        {
            var room = RoomDB.GetRoom(roomId);
            return room != null ? Ok(room) : NotFound();
        }

        [HttpGet("rooms/bulk")]
        public IActionResult GetRoomsByNames([FromQuery] List<string> name)
        {
            var rooms = RoomDB.GetRoomsByNames(name);
            return Ok(rooms);
        }

        [HttpGet("rooms/favoritedby/me")]
        public IActionResult GetFavoritedByMe(int skip = 0, int take = 100)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();

            var resultthing = RoomDB.Rooms
                .FindAll()
                .Where(room => PlayerDB.HasFavoritedRoom((long)playerId, room.RoomId))
                .OrderByDescending(r => r.Stats.FavoriteCount)
                .Skip(skip)
                .Take(take)
                .ToList();

            return Ok(resultthing);
        }

        [HttpPost("/api/rooms/v1/verifyRole")]
        public IActionResult VerifyRole(
        [FromQuery] long roomId,
        [FromQuery] int role,
        [FromQuery] string? context)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null)
                return Unauthorized();

            var room = RoomDB.GetRoom(roomId);
            if (room == null)
                return Ok(false);

            bool hasRole255 = room.Roles != null &&
                              room.Roles.Any(r =>
                                  r.AccountId == (long)playerId &&
                                  r.Role == (Role)255);

            return Ok(hasRole255);
        }


        [HttpGet("photon_access_token")]
        public IActionResult GetPhotonAccessToken()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) return Unauthorized();

            var heartbeat = PlayerDB.GetPlayerHeartbeat((long)id);

            return Ok(new
            {
                Permissions = new object[] { },
                PhotonAccessToken = "",
                RoomInstanceId = heartbeat?.roomInstance?.roomInstanceId
            });
        }

        [HttpGet("rooms/hot")]
        public IActionResult HotRooms(string tag, int skip = 0, int take = 30)
        {
            var (results, total) = RoomDB.GetHotRooms(tag, skip, take); // ts tag needa be fixed plz fix pingvin

            return Ok(new
            {
                Results = results ?? new List<Room>(),
                TotalResults = total
            });
        }

        [HttpPost("rooms/{roomId}/accessibility")]
        public IActionResult ChangeRoomAccessibility(long roomId, [FromForm] int accessibility)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();
            var room = RoomDB.GetRoom(roomId);
            if (room == null) return NotFound();
            var role = room.Roles.FirstOrDefault(r => r.AccountId == (long)playerId);
            if (role == null || role.Role != Role.Creator) return Forbid();
            room.Accessibility = (RoomAccessibility)accessibility;
            RoomDB.Rooms.Update(room);
            return Ok(new { Success = true });
        }

        [HttpGet("rooms/{roomId}/interactionby/me")]
        public IActionResult GetInteractionByMe(long roomId)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();

            return Ok(new
            {
                Cheered = PlayerDB.HasCheeredRoom((long)playerId, roomId),
                Favorited = PlayerDB.HasFavoritedRoom((long)playerId, roomId),
                LastVisitedAt = PlayerDB.GetRoomLastVisitedAt((long)playerId, roomId) ?? DateTime.UtcNow
            });
        }

        [HttpPut("rooms/{roomId}/interactionby/me/favorite")]
        public IActionResult FavoriteRoom(long roomId)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();

            if (!PlayerDB.HasFavoritedRoom((long)playerId, roomId))
            {
                PlayerDB.AddFavoriteRoom((long)playerId, roomId);
                RoomDB.IncrementRoomFavoriteCount(roomId);
            }

            return Ok(new { Favorited = true });
        }

        [HttpDelete("rooms/{roomId}/interactionby/me/favorite")]
        public IActionResult UnfavoriteRoom(long roomId)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();

            if (PlayerDB.HasFavoritedRoom((long)playerId, roomId))
            {
                PlayerDB.RemoveFavoriteRoom((long)playerId, roomId);
                RoomDB.DecrementRoomFavoriteCount(roomId);
            }

            return Ok(new { Favorited = false });
        }

        [HttpDelete("rooms/{roomId}")]
        public IActionResult DeleteRoom(long roomId)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();
            var room = RoomDB.GetRoom(roomId);
            if (room == null) return NotFound();
            var role = room.Roles.FirstOrDefault(r => r.AccountId == (long)playerId);
            if (role == null || role.Role != Role.Creator) return Forbid();
            RoomDB.Rooms.Delete(roomId);
            return Ok(new { Success = true });
        }


        [HttpPut("rooms/{roomId}/interactionby/me/cheer")]
        public IActionResult CheerRoom(long roomId)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();

            if (!PlayerDB.HasCheeredRoom((long)playerId, roomId))
            {
                PlayerDB.AddCheerRoom((long)playerId, roomId);
                RoomDB.IncrementRoomCheerCount(roomId);
            }

            return Ok(new { Cheered = true });
        }

        [HttpDelete("rooms/{roomId}/interactionby/me/cheer")]
        public IActionResult UncheerRoom(long roomId)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) return Unauthorized();

            if (PlayerDB.HasCheeredRoom((long)playerId, roomId))
            {
                PlayerDB.RemoveCheerRoom((long)playerId, roomId);
                RoomDB.DecrementRoomCheerCount(roomId);
            }

            return Ok(new { Cheered = false });
        }

        [HttpGet("rooms/{roomId}/subrooms/{subRoomId}/saves")]
        public IActionResult GetSubRoomSaves(
        long roomId,
        long subRoomId,
        int skip = 0,
        int take = 20,
        string? search = null)
        {
            var room = RoomDB.GetRoom(roomId);
            if (room == null) return NotFound();

            var subRoom = room.SubRooms?.FirstOrDefault(s => s.SubRoomId == subRoomId);
            if (subRoom == null) return NotFound();

            var (results, total) = RoomDB.GetSubRoomSaves(
                roomId,
                subRoomId,
                skip,
                take,
                search
            );

            return Ok(new
            {
                Results = results,
                TotalResults = total
            });
        }

        [HttpPost("rooms/{roomId}/subrooms/{subRoomId}/data")]
        public IActionResult PostSubRoomData(long roomId, long subRoomId, [FromBody] RoomDataRequest request)
        {
            try
            {
                var room = RoomDB.GetRoom(roomId);
                if (room == null)
                    return NotFound();

                var subRoom = room.SubRooms?.FirstOrDefault(s => s.SubRoomId == subRoomId);
                if (subRoom == null)
                    return NotFound();

                var playerId = AuthStuff.GetPlayerId(Request);

                if (!string.IsNullOrEmpty(request.RoomData?.Filename))
                    room.DataBlob = request.RoomData.Filename;

                if (!string.IsNullOrEmpty(request.SubRoomData?.Filename))
                    subRoom.DataBlob = request.SubRoomData.Filename;

                room.PersistenceVersion = request.PersistenceVersion;

                if (playerId != null)
                    subRoom.SavedByAccountId = (long)playerId;

                var save = new RoomDBClasses.SubRoomSave
                {
                    SubRoomDataSaveId = RoomDB.GetNextSubRoomSaveId(),
                    RoomId = room.RoomId,
                    SubRoomId = subRoom.SubRoomId,

                    DataBlob = request.SubRoomData?.Filename,
                    DataBlobHash = request.SubRoomData?.Hash,

                    PersistenceVersion = request.PersistenceVersion,
                    UgcSubVersion = request.PersistenceVersion,
                    OMVersion = 0,

                    SavedByAccountId = playerId,
                    SavedOnPlatform = 1,
                    SavedOnDeviceClass = 5,

                    Description = request.Description ?? "",
                    Tags = new List<string>(),

                    ModerationState = 0,
                    CreatedAt = DateTime.UtcNow
                };

                RoomDB.SubRoomSaves.Insert(save);

                if (request.AutoPublish && save.DataBlob != null)
                {
                    subRoom.DataBlob = save.DataBlob;
                }

                RoomDB.Rooms.Update(room);

                var roomData = new RoomDataResponse
                {
                    RoomId = room.RoomId,
                    IsDorm = room.IsDorm,
                    MaxPlayerCalculationMode = room.MaxPlayerCalculationMode,
                    MaxPlayers = room.MaxPlayers,
                    CloningAllowed = room.CloningAllowed,
                    DisableMicAutoMute = room.DisableMicAutoMute,
                    DisableRoomComments = room.DisableRoomComments,
                    EncryptVoiceChat = room.EncryptVoiceChat,
                    ToxmodEnabled = room.ToxmodEnabled,
                    LoadScreenLocked = room.LoadScreenLocked,
                    PersistenceVersion = room.PersistenceVersion,
                    AutoLocalizeRoom = room.AutoLocalizeRoom,
                    IsDeveloperOwned = room.IsDeveloperOwned,
                    RankedEntityId = room.RankedEntityId ?? "",
                    Name = room.Name,
                    Description = room.Description ?? "",
                    ImageName = room.ImageName,
                    WarningMask = (int)room.WarningMask,
                    CustomWarning = room.CustomWarning ?? "",
                    CreatorAccountId = room.CreatorAccountId,
                    State = (int)(room.State ?? RoomState.Active),
                    Accessibility = (int)room.Accessibility,
                    SupportsLevelVoting = room.SupportsLevelVoting,
                    IsRRO = room.IsRRO,
                    SupportsScreens = room.SupportsScreens,
                    SupportsWalkVR = room.SupportsWalkVR,
                    SupportsTeleportVR = room.SupportsTeleportVR,
                    SupportsVRLow = room.SupportsVRLow,
                    SupportsQuest2 = room.SupportsQuest2,
                    SupportsMobile = room.SupportsMobile,
                    SupportsJuniors = room.SupportsJuniors,
                    MinLevel = room.MinLevel,
                    CreatedAt = room.CreatedAt.ToString("O"),
                    Stats = new RoomStats
                    {
                        CheerCount = room.Stats.CheerCount,
                        FavoriteCount = room.Stats.FavoriteCount,
                        VisitorCount = room.Stats.VisitorCount,
                        VisitCount = room.Stats.VisitCount
                    },
                    RankingContext = 0,
                    SubRooms = room.SubRooms.Select(s => new SubRoomData
                    {
                        SubRoomId = s.SubRoomId,
                        RoomId = s.RoomId,
                        Name = s.Name,
                        DataBlob = s.DataBlob,
                        IsSandbox = s.IsSandbox,
                        MaxPlayers = s.MaxPlayers,
                        Accessibility = (int)s.Accessibility,
                        UnitySceneId = s.UnitySceneId,
                        SavedByAccountId = s.SavedByAccountId
                    }).ToList(),
                    Roles = room.Roles.Select(r => new RoleData
                    {
                        AccountId = r.AccountId,
                        Role = (int)r.Role,
                        InvitedRole = (int)r.InvitedRole
                    }).ToList(),
                    DataBlob = room.DataBlob,
                    UgcVersion = room.UgcVersion,
                    Tags = room.Tags?.Select(t => t.Tag).ToList() ?? new List<string>(),
                    PromoImages = room.PromoImages ?? new List<string>(),
                    PromoExternalContent = new List<object>()
                };

                return Ok(new
                {
                    success = true,
                    error = "",
                    value = roomData
                });
            }
            catch
            {
                return StatusCode(500);
            }
        }

        public class RoomDataRequest
        {
            public bool AutoPublish { get; set; }
            public string? Description { get; set; }
            public string? InventionUsage { get; set; }
            public int PersistenceVersion { get; set; }
            public RoomFileData? RoomData { get; set; }
            public RoomFileData? SubRoomData { get; set; }
            public string? UnityAssetId { get; set; }
        }

        public class RoomFileData
        {
            public string? Filename { get; set; }
            public string? Hash { get; set; }
            public string? OwnershipProof { get; set; }
        }

        public class RoomDataResponse
        {
            public long RoomId { get; set; }
            public bool IsDorm { get; set; }
            public int MaxPlayerCalculationMode { get; set; }
            public int MaxPlayers { get; set; }
            public bool CloningAllowed { get; set; }
            public bool DisableMicAutoMute { get; set; }
            public bool DisableRoomComments { get; set; }
            public bool EncryptVoiceChat { get; set; }
            public bool ToxmodEnabled { get; set; }
            public bool LoadScreenLocked { get; set; }
            public int PersistenceVersion { get; set; }
            public bool AutoLocalizeRoom { get; set; }
            public bool IsDeveloperOwned { get; set; }
            public string RankedEntityId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string ImageName { get; set; }
            public int WarningMask { get; set; }
            public string CustomWarning { get; set; }
            public long CreatorAccountId { get; set; }
            public int State { get; set; }
            public int Accessibility { get; set; }
            public bool SupportsLevelVoting { get; set; }
            public bool IsRRO { get; set; }
            public bool SupportsScreens { get; set; }
            public bool SupportsWalkVR { get; set; }
            public bool SupportsTeleportVR { get; set; }
            public bool SupportsVRLow { get; set; }
            public bool SupportsQuest2 { get; set; }
            public bool SupportsMobile { get; set; }
            public bool SupportsJuniors { get; set; }
            public int MinLevel { get; set; }
            public string CreatedAt { get; set; }
            public RoomStats Stats { get; set; }
            public int RankingContext { get; set; }
            public List<SubRoomData> SubRooms { get; set; }
            public List<RoleData> Roles { get; set; }
            public string DataBlob { get; set; }
            public int UgcVersion { get; set; }
            public List<string> Tags { get; set; }
            public List<string> PromoImages { get; set; }
            public List<object> PromoExternalContent { get; set; }
            public List<LoadScreenData> LoadScreens { get; set; }
        }

        public class RoomStats
        {
            public int CheerCount { get; set; }
            public int FavoriteCount { get; set; }
            public int VisitorCount { get; set; }
            public int VisitCount { get; set; }
        }

        public class SubRoomData
        {
            public long SubRoomId { get; set; }
            public long RoomId { get; set; }
            public string Name { get; set; }
            public string DataBlob { get; set; }
            public bool IsSandbox { get; set; }
            public int MaxPlayers { get; set; }
            public int Accessibility { get; set; }
            public string UnitySceneId { get; set; }
            public long SavedByAccountId { get; set; }
        }

        public class RoleData
        {
            public long AccountId { get; set; }
            public int Role { get; set; }
            public int InvitedRole { get; set; }
        }

        public class LoadScreenData
        {
            public string ImageName { get; set; }
            public string Title { get; set; }
            public string Subtitle { get; set; }
        }
    }
}