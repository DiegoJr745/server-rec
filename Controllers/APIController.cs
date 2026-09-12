using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using sscs2023.Auth;
using sscs2023.Classes;
using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using sscs2023.Classes.Rooms;
using System.Text;
using System.Text.Json;
using static sscs2023.Classes.DBs.DBClasses.EventDBClasses;
using static sscs2023.Classes.DBs.DBClasses.PlayerDBClasses;

namespace sscs2023.Controllers
{
    [ApiController]
    public class APIController : ControllerBase
    {
        [HttpGet("/")]
        public IActionResult GetNS()
        {
            string url = ServerConfig.BaseURL;
            return Ok(new
            {
                Accounts = url + "/acc",
                API = url,
                Auth = url + "/auth",
                BugReporting = url,
                Cards = url,
                CDN = url + "/cdn",
                Chat = url,
                Clubs = url,
                CMS = url,
                Commerce = url,
                Data = url,
                DataCollection = url,
                Discovery = url,
                Econ = url,
                GameLogs = url,
                Geo = url,
                Images = url + "/imageserver",
                Leaderboard = url,
                Link = url,
                Lists = url,
                Matchmaking = url + "/match",
                Moderation = url,
                Notifications = url + "/noti",
                PlatformNotifications = url,
                PlayerSettings = url,
                RoomComments = url,
                Rooms = url + "/roomserver",
                Storage = url,
                Strings = url,
                StringsCDN = url,
                Studio = url,
                Thorn = url,
                Videos = url,
                WWW = url
            });
        }

        [HttpGet("api/versioncheck/v4")]
        public IActionResult VersionCheck()
        {
            return Ok(new
            {
                ValidVersion = 0,
                VersionStatus = 0,
                UpdateNotificationStage = 0,
                IsVersionIslanded = false,
                IsCrossPlayDisabled = false
            });
        }

        [HttpGet("api/gameconfigs/v1/all")]
        public IActionResult GetGameConfigs()
        {
            string path = Path.Join(Program.dataDir, "APIS", "GameConfigs.json");
            return System.IO.File.Exists(path) ? Content(System.IO.File.ReadAllText(path), "application/json") : NotFound();
        }

        [HttpGet("api/config/v1/amplitude")]
        public IActionResult GetAmplitude()
        {
            return Ok(new
            {
                AmplitudeKey = "cb2fb2ecb9953512c29af5bca58f2b4a",
                UseRudderStack = true,
                RudderStackKey = "23NiJHIgu3koaGNCZIiuYvIQNCu",
                UseStatSig = true,
                StatSigKey = "client-SBZkOrjD3r1Cat3f3W8K6sBd11WKlXZXIlCWj6l4Aje",
                StatSigEnvironment = 0
            });
        }

        [HttpGet("api/avatar/v1/defaultunlocked")]
        public IActionResult GetDefaultUnlocked()
        {
            return Ok(ServerConfig.Bracket);
        }

        [HttpGet("api/avatar/v1/defaultbaseavataritems")]
        public IActionResult GetDefaultBaseAvatarItems()
        {
            return Ok(ServerConfig.Bracket);
        }
        
        [HttpGet("/api/objectives/v1/myprogress")]
        public IActionResult GetMyObjectiveProgress()
        {
        	var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();
                
            return Ok(new 
            {
            	Objectives = new List<object>(),
                ObjectiveGroups = new List<object>()
            });
        }
        
        [HttpGet("/api/avatar/v2")]
        [HttpGet("/api/avatar/v2/{playerId}")]
        public IActionResult GetMyAvatar(long? playerId = null)
        {
            if (playerId.HasValue)
            {
                var player = PlayerDB.Players.FindById(playerId.Value);
                if (player == null)
                    return NotFound();

                return Ok(player.Player?.PlayerExtra.Avatar);
            }

            var currentPlayer = AuthStuff.GetCurrentPlayer(Request);
            if (currentPlayer == null)
                return Unauthorized();

            return Ok(currentPlayer.Player?.PlayerExtra.Avatar);
        }
        
        [HttpPost("/api/avatar/v2/set")]
        public IActionResult SetMyAvatar([FromBody] PlayerDBClasses.Avatar request)
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();

            return Ok(PlayerDB.SetAvatar((long)id, request));
        }
        
        [HttpGet("/api/avatar/v4/items")]
        public IActionResult GetMyAvatarItems()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();

            string path = Path.Join(Program.dataDir, "APIS", "Items", "AvatarItems.json");
            return System.IO.File.Exists(path) ? Content(System.IO.File.ReadAllText(path), "application/json") : NotFound();
        }
        
        [HttpGet("/api/PlayerReporting/v1/moderationBlockDetails")]
        public IActionResult GetMyModerationBlockDetails()
        {
            var player = AuthStuff.GetCurrentPlayer(Request);
            if (player == null)
            	return Unauthorized();

            return Ok(player.Player.PlayerExtra.ModerationBlockDetails);
        }
        
        [HttpGet("/api/relationships/v2/get")]
        public IActionResult GetMyRelationships()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();

            return Ok(ServerConfig.Bracket);
        }

        [HttpPost("/api/relationships/v1/mute")]
        public IActionResult Mute([FromForm] long playerId)
        {
            var me = AuthStuff.GetPlayerId(Request);
            if (me == null) return Unauthorized();

            var player = PlayerDB.Players.FindById(me.Value);
            if (player == null || player.Player == null) return NotFound();

            player.Player.Relationships ??= new List<PlayerRelationship>();

            var rel = player.Player.Relationships
                .FirstOrDefault(r => r.PlayerId == playerId);

            if (rel == null)
            {
                rel = new PlayerRelationship { PlayerId = playerId };
                player.Player.Relationships.Add(rel);
            }

            rel.Muted = 1;

            PlayerDB.Players.Update(player);

            return Ok(new
            {
                Favorited = rel.Favorited,
                Ignored = rel.Ignored,
                Muted = rel.Muted,
                PlayerID = rel.PlayerId,
                RelationshipType = rel.RelationshipType
            });
        }

        [HttpPost("/api/relationships/v1/unmute")]
        public IActionResult Unmute([FromForm] long playerId)
        {
            var me = AuthStuff.GetPlayerId(Request);
            if (me == null) return Unauthorized();

            var player = PlayerDB.Players.FindById(me.Value);
            if (player == null || player.Player == null) return NotFound();

            player.Player.Relationships ??= new List<PlayerRelationship>();

            var rel = player.Player.Relationships
                .FirstOrDefault(r => r.PlayerId == playerId);

            if (rel == null)
            {
                rel = new PlayerRelationship { PlayerId = playerId };
                player.Player.Relationships.Add(rel);
            }

            rel.Muted = 0;

            PlayerDB.Players.Update(player);

            return Ok(new
            {
                Favorited = rel.Favorited,
                Ignored = rel.Ignored,
                Muted = rel.Muted,
                PlayerID = rel.PlayerId,
                RelationshipType = rel.RelationshipType
            });
        }

        [HttpPost("/api/relationships/v1/ignore")]
        public IActionResult Ignore([FromForm] long playerId)
        {
            var me = AuthStuff.GetPlayerId(Request);
            if (me == null) return Unauthorized();

            var player = PlayerDB.Players.FindById(me.Value);
            if (player == null || player.Player == null) return NotFound();

            player.Player.Relationships ??= new List<PlayerRelationship>();

            var rel = player.Player.Relationships
                .FirstOrDefault(r => r.PlayerId == playerId);

            if (rel == null)
            {
                rel = new PlayerRelationship { PlayerId = playerId };
                player.Player.Relationships.Add(rel);
            }

            rel.Ignored = 1;

            PlayerDB.Players.Update(player);

            return Ok(new
            {
                Favorited = rel.Favorited,
                Ignored = rel.Ignored,
                Muted = rel.Muted,
                PlayerID = rel.PlayerId,
                RelationshipType = rel.RelationshipType
            });
        }

        [HttpPost("/api/relationships/v1/unignore")]
        public IActionResult Unignore([FromForm] long playerId)
        {
            var me = AuthStuff.GetPlayerId(Request);
            if (me == null) return Unauthorized();

            var player = PlayerDB.Players.FindById(me.Value);
            if (player == null || player.Player == null) return NotFound();

            player.Player.Relationships ??= new List<PlayerRelationship>();

            var rel = player.Player.Relationships
                .FirstOrDefault(r => r.PlayerId == playerId);

            if (rel == null)
            {
                rel = new PlayerRelationship { PlayerId = playerId };
                player.Player.Relationships.Add(rel);
            }

            rel.Ignored = 0;

            PlayerDB.Players.Update(player);

            return Ok(new
            {
                Favorited = rel.Favorited,
                Ignored = rel.Ignored,
                Muted = rel.Muted,
                PlayerID = rel.PlayerId,
                RelationshipType = rel.RelationshipType
            });
        }

        [HttpGet("/acc/account/search")]
        public IActionResult SearchAccounts([FromQuery] string name)
        {
            var authId = AuthStuff.GetPlayerId(Request);
            if (authId == null)
                return StatusCode(403);

            if (string.IsNullOrWhiteSpace(name))
                return Ok(new List<PlayerDTOBase>());

            name = name.Trim();

            bool isAtSearch = name.StartsWith("@");

            if (isAtSearch)
            {
                var target = name.Substring(1).ToLowerInvariant();

                var match = PlayerDB.Players.FindAll()
                    .FirstOrDefault(p =>
                        string.Equals(p.Player?.Username ?? "", target, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                    return Ok(new List<PlayerDTOBase>());

                var pl = match.Player;

                int platformFlags = match.PlatformIds?.Aggregate(0, (acc, pid) => acc | (int)pid.Platform) ?? 0;

                return Ok(new List<PlayerDTOBase>
        {
            new PlayerDTO
            {
                accountId = match.PlayerId,
                username = pl?.Username,
                displayName = pl?.DisplayName,
                profileImage = pl?.ProfileImage,
                isJunior = pl?.IsJunior ?? false,
                createdAt = pl?.CreatedAt ?? DateTime.UtcNow,
                platforms = platformFlags,
                personalPronouns = 0,
                identityFlags = 0
            }
        });
            }

            var q = name.ToLowerInvariant();

            var results = PlayerDB.Players.FindAll()
                .Where(p =>
                    (!string.IsNullOrEmpty(p.Player?.Username) &&
                     p.Player.Username.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(p.Player?.DisplayName) &&
                     p.Player.DisplayName.ToLowerInvariant().Contains(q))
                )
                .Select(p =>
                {
                    var username = p.Player?.Username?.ToLowerInvariant() ?? "";
                    var display = p.Player?.DisplayName?.ToLowerInvariant() ?? "";

                    int score = 0;

                    if (username.StartsWith(q) || display.StartsWith(q))
                        score += 100;

                    if (username.Contains(q) || display.Contains(q))
                        score += 50;

                    score -= Math.Min(username.Length, display.Length);

                    return new { Player = p, Score = score };
                })
                .OrderByDescending(x => x.Score)
                .Take(50)
                .Select(x =>
                {
                    var pl = x.Player.Player;

                    int platformFlags = x.Player.PlatformIds?.Aggregate(0, (acc, pid) => acc | (int)pid.Platform) ?? 0;

                    return new PlayerDTO
                    {
                        accountId = x.Player.PlayerId,
                        username = pl?.Username,
                        displayName = pl?.DisplayName,
                        profileImage = pl?.ProfileImage,
                        isJunior = pl?.IsJunior ?? false,
                        createdAt = pl?.CreatedAt ?? DateTime.UtcNow,
                        platforms = platformFlags,
                        personalPronouns = 0,
                        identityFlags = 0
                    } as PlayerDTOBase;
                })
                .ToList();

            return Ok(results);
        }

        [HttpGet("/api/images/v4/room/{roomId}")]
        public IActionResult GetImagesFromRoom()
        {
            return Ok(new {});
        }

        [HttpPost("/data/heartbeat")]
        public async Task<IActionResult> Heartbeat()
        {
            using var reader = new StreamReader(Request.Body);
            var raw = await reader.ReadToEndAsync();

            var decoded = Uri.UnescapeDataString(raw);

            using var outer = JsonDocument.Parse(decoded);
            var root = outer.RootElement;

            var eventType = root.GetProperty("EventType").GetString();
            var eventParamsRaw = root.GetProperty("EventParams").GetString();

            using var inner = JsonDocument.Parse(eventParamsRaw);
            var innerRoot = inner.RootElement;

            var accountId = innerRoot.GetProperty("_AccountId").GetString();
            var timeUtc = innerRoot.GetProperty("_TimeUtc").GetString();

            var embed = new
            {
                title = "Heartbeat",
                color = 3447003,
                fields = new[]
                {
            new { name = "EventType", value = eventType ?? "null", inline = true },
            new { name = "AccountId", value = accountId ?? "null", inline = true },
            new { name = "TimeUtc", value = timeUtc ?? "null", inline = false }
        }
            };

            var payload = new
            {
                embeds = new[] { embed }
            };

            using var httpClient = new HttpClient();

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await httpClient.PostAsync(
                "https://discord.com/api/webhooks/1498942622738284624/wMtXfBJp71VglQwPNC73qWK2Bfo_a3xa_cb0A_nThFB3y5bFwe3tTI_6I7gfTSmzP7jL",
                content
            );

            return Ok();
        }

        [HttpPost("/upload")]
        public async Task<IActionResult> Upload()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null)
                return Unauthorized();

            var file = Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
                return BadRequest();

            if (!int.TryParse(Request.Form["fileType"].FirstOrDefault() ?? "2", out int fileType))
                fileType = 2;

            string basePath = Path.GetFullPath(Path.Join(Program.dataDir, "..", "Data", "cdn"));
            Directory.CreateDirectory(basePath);

            string extension = fileType == 1 ? ".room" : ".bin";

            var safeName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Join(basePath, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new
            {
                filename = safeName
            });
        }

        [HttpGet("/api/messages/v2/get")]
        public IActionResult GetMyMessages()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();

            return Ok(ServerConfig.Bracket);
        }
        
        [HttpGet("/playersettings")]
        public IActionResult GetMySettings()
        {
            var player = AuthStuff.GetCurrentPlayer(Request);
            if (player == null)
            	return Unauthorized();

            return Ok(player.Player.PlayerExtra.Settings);
        }
        
        [HttpPut("/playersettings")]
        public IActionResult SetMySettings([FromForm] string key, [FromForm] string value)
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();
                
			PlayerDB.SetPlayerSetting(key, value ?? "", (long)id);
            
            return NoContent();
        }
        
        [HttpGet("/econ/customAvatarItems/v1/owned")]
        public IActionResult GetMyOwnedCustomAvatarItems([FromQuery] int skip, [FromQuery] int take)
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();
                
            return Ok(new 
            {
            	Results = Array.Empty<object>(),
                TotalResults = 0
            });
        }
        
        [HttpGet("/api/checklist/v1/current")]
        public IActionResult GetMyChecklist()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();

            return Ok(ServerConfig.Bracket);
        }
        
        [HttpGet("/api/players/v2/progression/bulk")]
        public IActionResult GetProgressionForPlayers([FromQuery] List<long> id)
        {
            var authId = AuthStuff.GetPlayerId(Request);
            if (authId == null) 
                return Unauthorized();

            if (id == null || id.Count == 0)
                return Ok(new List<PlayerDBClasses.PlayerProgressionDTO>());

            var progressions = PlayerDB.GetProgressionBulk(id);

            return Ok(progressions);
        }
        
        [HttpGet("/api/playerReputation/v2/bulk")]
        public IActionResult GetReputationBulk([FromQuery] List<long> id)
        {
            var authId = AuthStuff.GetPlayerId(Request);
            if (authId == null) 
                return Unauthorized();

            if (id == null || id.Count == 0)
                return Ok(new List<PlayerDBClasses.Reputation>());

            var results = PlayerDB.GetReputationBulk(id);
            return Ok(results);
        }
        
        [HttpPost("/api/PlayerReporting/v1/hile")] // todo log to channel
        public IActionResult PlayerReportingHile()
        {
            var authId = AuthStuff.GetPlayerId(Request);
            if (authId == null) 
                return Unauthorized();
                
            return Ok(false);
        }

        [HttpGet("api/config/v2")]
        public IActionResult GetConfigV2()
        {
            string path = Path.Join(Program.dataDir, "APIS", "ConfigV2.json");
            return System.IO.File.Exists(path) ? Content(System.IO.File.ReadAllText(path), "application/json") : NotFound();
        }

		[HttpGet("/cdn/config/LoadingScreenTipData")]
        public IActionResult GetLoadingScreenTips()
		{
			string path = Path.Join(Program.dataDir, "APIS", "LoadingScreenTips.json");
			return System.IO.File.Exists(path) ? Content(System.IO.File.ReadAllText(path), "application/json") : NotFound();
		}

		[HttpGet("/api/announcement/v1/get")]
        [HttpGet("/api/PlayerReporting/v1/voteToKickReasons")]
        [HttpGet("/api/avatar/v3/saved")]

        [HttpGet("/api/equipment/v2/getUnlocked")]
        public IActionResult GetUnlockedEquipment()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null)
                return Unauthorized();
            string path = Path.Join(Program.dataDir, "..", "Data", "APIS", "Items", "Equipment.json");
            return Ok(new
            {
                path
            });
        }

        [HttpGet("/api/consumables/v2/getUnlocked")]
        public IActionResult GetUnlockedConsumables()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null)
                return Unauthorized();
            string path = Path.Join(Program.dataDir, "..", "Data", "APIS", "Items", "Consumables.json");
            return Ok(new
            {
                path
            });
        }

        [HttpGet("/api/images/v2/named")]
        [HttpGet("/api/avatar/v2/gifts")]
        [HttpGet("/api/gamerewards/v1/pending")]
        [HttpGet("/api/roomkeys/v1/mine")]
        [HttpGet("/api/roomcurrencies/v1/currencies")]

        [HttpGet("/api/inventions/v2/mine")]
        public IActionResult GetMyInventions()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null)
                return Unauthorized();

            var inventions = InventionDB.Inventions
                .Find(x => x.CreatorPlayerId == (long)id)
                .ToList();

            var results = inventions.Select(inv => new InventionDBClasses.InventionData
            {
                AllowTrial = inv.AllowTrial,
                CheerCount = inv.CheerCount,
                CreatedAt = inv.CreatedAt.ToString("O"),
                CreatorPermission = inv.CreatorPermission,
                CreatorPlayerId = inv.CreatorPlayerId,
                CurrentVersionNumber = inv.CurrentVersionNumber,
                Description = inv.Description,
                GeneralPermission = inv.GeneralPermission,
                HideFromPlayer = inv.HideFromPlayer,
                ImageName = inv.ImageName,
                InventionId = inv.InventionId,
                IsAGInvention = inv.IsAGInvention,
                IsCertifiedInvention = inv.IsCertifiedInvention,
                IsPublished = inv.IsPublished,
                ModifiedAt = inv.ModifiedAt.ToString("O"),
                Name = inv.Name,
                NumDownloads = inv.NumDownloads,
                NumPlayersHaveUsedInRoom = inv.NumPlayersHaveUsedInRoom,
                Price = inv.Price,
                ReplicationId = inv.ReplicationId
            }).ToList();

            return Ok(results);
        }
        
        [HttpGet("/api/playerevents/v1/all")]
        public IActionResult GetAllPlayerEvents()
        {
            return Ok(new 
            {
            	Created = Array.Empty<object>(),
                Responses = Array.Empty<object>()
            });
        }
        
        [HttpGet("/api/customAvatarItems/v1/isCreationAllowedForAccount")]
        public IActionResult GetCustomAvatarItemsIsCreationAllowedForAccount()
        {
            return Ok(new
            {
            	success = true,
                value = (object?)null
            });
        }

        [HttpPost("/api/images/v4/uploadsaved")]
        public async Task<IActionResult> UploadSaved()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null)
                return Unauthorized();

            var file = Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            string imagesPath = Path.Join(Program.dataDir, "Images");
            Directory.CreateDirectory(imagesPath);

            var originalName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = file.ContentType switch
                {
                    "image/png" => ".png",
                    "image/jpeg" => ".jpg",
                    "image/jpg" => ".jpg",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ".png"
                };
            }

            var safeName = $"ImageData{Guid.NewGuid()}{extension}";
            var filePath = Path.Join(imagesPath, safeName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new
            {
                ImageName = safeName
            });
        }

        [HttpGet("/api/customAvatarItems/v1/isCreationEnabled")]
        [HttpGet("/api/customAvatarItems/v1/isRenderingEnabled")]
        public IActionResult CustomAvatarItemsIsEnabled()
        {
            return Ok(true);
        }
        
        [HttpGet("/api/images/v6")]
        public IActionResult GetImageV6([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest();

            return Ok(new
            {
                fileName = name
            });
        }

        [HttpGet("/api/roomconsumables/v1/roomConsumable/room/{roomId}")]
        public IActionResult GetRoomConsumablesForRoom(long roomId)
        {
        	return Ok(ServerConfig.Bracket);
        }
        
        [HttpPost("/api/sanitize/v1")]
        public IActionResult SanitizeV1([FromBody] SanitizeRequest request)
        {
        	return Ok(JsonSerializer.Serialize(request.Value));
        }
        
        [HttpPost("/api/sanitize/v1/isPure")]
        public IActionResult SanitizeV1IsPure()
        {
        	return Ok(new
            {
                IsPure = true
            });
        }

        [HttpGet("/api/influencerpartnerprogram/influencers")]
        public IActionResult GetInfluencers([FromQuery] int? take)
        {
            var influencerIds = new List<int> { 2, 12 };
            if (take.HasValue && take.Value > 0)
            {
                influencerIds = influencerIds.Take(take.Value).ToList();
            }
            return Ok(new
            {
                ContinuationToken = (string?)null,
                InfluencerIds = influencerIds
            });
        }

        [HttpPost("/api/playerevents/v2")]
        public IActionResult CreatePlayerEvent([FromBody] PlayerEventRequest request)
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null)
                return Unauthorized();

            if (request == null || string.IsNullOrWhiteSpace(request.Name) || request.RoomId <= 0)
                return BadRequest("Invalid request");

            var response = EventDB.CreatePlayerEvent((long)id, request);
            
            if (response.Result != 0)
                return BadRequest("Failed to create event");

            return Ok(response);
        }
        
        [HttpDelete("/api/playerevents/v2/delete/{eventId}")]
        public IActionResult DeletePlayerEvent(long eventId)
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null)
                return Unauthorized();

            var deleted = EventDB.DeletePlayerEvent(eventId, (long)id);
            
            return Ok(new
            {
                success = deleted
            });
        }
        
        [HttpGet("/cdn/data/{fileName}")]
        public IActionResult GetCdnData(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File is deleted off SSCS.");

            string basePath = Path.GetFullPath(Path.Join(Program.dataDir, "..", "Data", "cdn"));
            string filePath = Path.Join(basePath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            try
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = GetContentType(fileName);
                return File(fileBytes, contentType, fileName);
            }
            catch
            {
                return StatusCode(500, "Error reading file");
            }
        }

        [HttpGet("/cdn/invention/{fileName}")]
        public IActionResult GetInventionData(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File is deleted off SSCS.");

            string basePath = Path.GetFullPath(Path.Join(Program.dataDir, "..", "Data", "cdn"));
            string filePath = Path.Join(basePath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            try
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = GetContentType(fileName);
                return File(fileBytes, contentType, fileName);
            }
            catch
            {
                return StatusCode(500, "Error reading file"); 
            }
        }

        [HttpGet("/cdn/video/{fileName}")]
        public IActionResult GetVideoData(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File is deleted off SSCS.");

            string basePath = Path.GetFullPath(Path.Join(Program.dataDir, "..", "Data", "cdn", "video"));
            string filePath = Path.Join(basePath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            try
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = GetContentType(fileName);
                return File(fileBytes, contentType, fileName);
            }
            catch
            {
                return StatusCode(500, "Error reading file");
            }
        }

        [HttpGet("/cdn/room/{fileName}")]
        public IActionResult GetRoomData(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File is deleted off SSCS.");

            string basePath = Path.GetFullPath(Path.Join(Program.dataDir, "..", "Data", "cdn"));
            string filePath = Path.Join(basePath, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            try
            {
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = GetContentType(fileName);
                return File(fileBytes, contentType, fileName);
            }
            catch
            {
                return StatusCode(500, "Error reading file");
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".room" => "application/octet-stream",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bin" => "application/octet-stream",
                ".json" => "application/json",
                _ => "application/octet-stream"
            };
        }
        
        public class SanitizeRequest
        {
            public string Value { get; set; } = string.Empty;
        }
    }
}