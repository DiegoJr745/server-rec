using Microsoft.AspNetCore.Mvc;
using sscs2023.Classes;
using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using static sscs2023.Classes.DBs.DBClasses.PlayerDBClasses;

namespace sscs2023.Controllers
{
    [ApiController]
    [Route("api/playerdb")]
    public class PlayerDBManagerController : ControllerBase
    {
        /// <summary>
        /// Get all players with optional search and pagination
        /// </summary>
        [HttpGet("players")]
        public IActionResult GetAllPlayers([FromQuery] string? search = null, [FromQuery] int skip = 0, [FromQuery] int take = 50)
        {
            try
            {
                var allPlayers = PlayerDB.Players.FindAll().ToList();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string searchLower = search.ToLower();
                    allPlayers = allPlayers.Where(p =>
                        p.Player?.Username?.ToLower().Contains(searchLower) == true ||
                        p.Player?.DisplayName?.ToLower().Contains(searchLower) == true ||
                        p.PlayerId.ToString().Contains(searchLower)
                    ).ToList();
                }

                var total = allPlayers.Count;
                var results = allPlayers.Skip(skip).Take(take).Select(p => new
                {
                    p.PlayerId,
                    Username = p.Player?.Username,
                    DisplayName = p.Player?.DisplayName,
                    CreatedAt = p.Player?.CreatedAt,
                    LastLoginAt = p.Player?.LastLoginAt,
                    IsJunior = p.Player?.IsJunior,
                    Level = p.Player?.Level
                }).ToList();

                return Ok(new { Results = results, Total = total });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific player by ID
        /// </summary>
        [HttpGet("player/{playerId}")]
        public IActionResult GetPlayer(long playerId)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                return Ok(MapPlayerToDTO(player));
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Update player basic information
        /// </summary>
        [HttpPut("player/{playerId}")]
        public IActionResult UpdatePlayer(long playerId, [FromBody] UpdatePlayerRequest request)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                if (player.Player == null)
                    player.Player = new Player();

                if (!string.IsNullOrWhiteSpace(request.Username))
                    player.Player.Username = request.Username;

                if (!string.IsNullOrWhiteSpace(request.DisplayName))
                    player.Player.DisplayName = request.DisplayName;

                if (!string.IsNullOrWhiteSpace(request.Bio))
                    player.Player.Bio = request.Bio;

                if (!string.IsNullOrWhiteSpace(request.ProfileImage))
                    player.Player.ProfileImage = request.ProfileImage;

                if (request.IsJunior.HasValue)
                    player.Player.IsJunior = request.IsJunior.Value;

                if (request.AvailableUsernameChanges.HasValue)
                    player.Player.AvailableUsernameChanges = request.AvailableUsernameChanges.Value;

                if (request.Level.HasValue)
                    player.Player.Level = request.Level.Value;

                if (request.XP.HasValue)
                    player.Player.XP = request.XP.Value;

                if (request.CreatedAt.HasValue)
                    player.Player.CreatedAt = request.CreatedAt.Value;

                if (request.LastLoginAt.HasValue)
                    player.Player.LastLoginAt = request.LastLoginAt.Value;

                if (request.Birthday.HasValue)
                    player.Player.Birthday = request.Birthday.Value;

                if (!string.IsNullOrWhiteSpace(request.Email))
                    player.Player.Email = request.Email;

                if (!string.IsNullOrWhiteSpace(request.Password))
                    player.Password = request.Password;

                PlayerDB.Players.Update(player);

                return Ok(new { Message = "Player updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Update player reputation
        /// </summary>
        [HttpPut("player/{playerId}/reputation")]
        public IActionResult UpdatePlayerReputation(long playerId, [FromBody] UpdateReputationRequest request)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                if (player.Player?.Reputation == null)
                    player.Player.Reputation = new Reputation();

                var rep = player.Player.Reputation;
                rep.AccountId = playerId;

                if (request.CheerGeneral.HasValue)
                    rep.CheerGeneral = request.CheerGeneral.Value;
                if (request.CheerHelpful.HasValue)
                    rep.CheerHelpful = request.CheerHelpful.Value;
                if (request.CheerCreative.HasValue)
                    rep.CheerCreative = request.CheerCreative.Value;
                if (request.CheerGreatHost.HasValue)
                    rep.CheerGreatHost = request.CheerGreatHost.Value;
                if (request.CheerSportsman.HasValue)
                    rep.CheerSportsman = request.CheerSportsman.Value;
                if (request.CheerCredit.HasValue)
                    rep.CheerCredit = request.CheerCredit.Value;
                if (request.Notoriety.HasValue)
                    rep.Noteriety = request.Notoriety.Value;
                if (request.SubscriberCount.HasValue)
                    rep.SubscriberCount = request.SubscriberCount.Value;
                if (request.IsCheerful.HasValue)
                    rep.IsCheerful = request.IsCheerful.Value;

                PlayerDB.Players.Update(player);

                return Ok(new { Message = "Reputation updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get player settings
        /// </summary>
        [HttpGet("player/{playerId}/settings")]
        public IActionResult GetPlayerSettings(long playerId)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                var settings = player.Player?.PlayerExtra?.Settings ?? new List<Setting>();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Add or update a player setting
        /// </summary>
        [HttpPost("player/{playerId}/settings")]
        public IActionResult AddPlayerSetting(long playerId, [FromBody] SettingRequest request)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Value))
                    return BadRequest(new { Error = "Key and Value are required" });

                PlayerDB.SetPlayerSetting(request.Key, request.Value, playerId);

                return Ok(new { Message = "Setting added/updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a player setting
        /// </summary>
        [HttpDelete("player/{playerId}/settings/{settingKey}")]
        public IActionResult DeletePlayerSetting(long playerId, string settingKey)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                if (player.Player?.PlayerExtra?.Settings != null)
                {
                    var setting = player.Player.PlayerExtra.Settings.FirstOrDefault(s => s.Key == settingKey);
                    if (setting != null)
                    {
                        player.Player.PlayerExtra.Settings.Remove(setting);
                        PlayerDB.Players.Update(player);
                    }
                }

                return Ok(new { Message = "Setting deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Add a platform to a player
        /// </summary>
        [HttpPost("player/{playerId}/platforms")]
        public IActionResult AddPlayerPlatform(long playerId, [FromBody] AddPlatformRequest request)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                if (player.PlatformIds == null)
                    player.PlatformIds = new List<mPlatformID>();

                var existingPlatform = player.PlatformIds.FirstOrDefault(p => p.Platform == request.Platform);
                if (existingPlatform != null)
                {
                    existingPlatform.PlatformId = request.PlatformId;
                }
                else
                {
                    player.PlatformIds.Add(new mPlatformID
                    {
                        Platform = request.Platform,
                        PlatformId = request.PlatformId
                    });
                }

                PlayerDB.Players.Update(player);
                return Ok(new { Message = "Platform added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Remove a platform from a player
        /// </summary>
        [HttpDelete("player/{playerId}/platforms/{platform}")]
        public IActionResult RemovePlayerPlatform(long playerId, int platform)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                if (player.PlatformIds != null)
                {
                    var platformToRemove = player.PlatformIds.FirstOrDefault(p => (int)p.Platform == platform);
                    if (platformToRemove != null)
                    {
                        player.PlatformIds.Remove(platformToRemove);
                        PlayerDB.Players.Update(player);
                    }
                }

                return Ok(new { Message = "Platform removed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Update player roles
        /// </summary>
        [HttpPut("player/{playerId}/roles")]
        public IActionResult UpdatePlayerRoles(long playerId, [FromBody] UpdateRolesRequest request)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                player.PlayerRoles = new List<PlayerRoles>();

                if (request.IsScreenshare)
                    player.PlayerRoles.Add(PlayerRoles.Screenshare);
                if (request.IsModerator)
                    player.PlayerRoles.Add(PlayerRoles.Moderator);
                if (request.IsDeveloper)
                    player.PlayerRoles.Add(PlayerRoles.Developer);

                PlayerDB.Players.Update(player);
                return Ok(new { Message = "Roles updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Generate a new auth token for a player
        /// </summary>
        [HttpPost("player/{playerId}/authtoken")]
        public IActionResult GenerateNewAuthToken(long playerId)
        {
            try
            {
                var player = PlayerDB.Players.FindById(playerId);
                if (player == null)
                    return NotFound(new { Error = "Player not found" });

                player.AuthToken = Guid.NewGuid().ToString();
                PlayerDB.Players.Update(player);

                return Ok(new { AuthToken = player.AuthToken, Message = "New auth token generated" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a player
        /// </summary>
        [HttpDelete("player/{playerId}")]
        public IActionResult DeletePlayer(long playerId)
        {
            try
            {
                var success = PlayerDB.Players.Delete(playerId);
                if (!success)
                    return NotFound(new { Error = "Player not found" });

                return Ok(new { Message = "Player deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Map a FullPlayer to a response DTO
        /// </summary>
        private object MapPlayerToDTO(FullPlayer player)
        {
            var p = player.Player ?? new Player();
            var rep = p.Reputation ?? new Reputation();

            return new
            {
                player.PlayerId,
                player.AuthToken,
                player.Password,
                player.PlayerRoles,
                player.PlatformIds,
                Player = new
                {
                    p.Username,
                    p.DisplayName,
                    p.Bio,
                    p.AvailableUsernameChanges,
                    p.IsJunior,
                    p.Level,
                    p.XP,
                    p.ProfileImage,
                    p.BannerImage,
                    p.Email,
                    p.CreatedAt,
                    p.LastLoginAt,
                    p.Birthday,
                    Reputation = new
                    {
                        rep.CheerGeneral,
                        rep.CheerHelpful,
                        rep.CheerCreative,
                        rep.CheerGreatHost,
                        rep.CheerSportsman,
                        rep.CheerCredit,
                        rep.Noteriety,
                        rep.SubscriberCount,
                        rep.IsCheerful
                    },
                    Settings = p.PlayerExtra?.Settings ?? new List<Setting>()
                }
            };
        }

        // Request/Response DTOs
        public class UpdatePlayerRequest
        {
            public string? Username { get; set; }
            public string? DisplayName { get; set; }
            public string? Bio { get; set; }
            public string? ProfileImage { get; set; }
            public string? Email { get; set; }
            public string? Password { get; set; }
            public bool? IsJunior { get; set; }
            public int? AvailableUsernameChanges { get; set; }
            public int? Level { get; set; }
            public int? XP { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? LastLoginAt { get; set; }
            public DateTime? Birthday { get; set; }
        }

        public class UpdateReputationRequest
        {
            public int? CheerGeneral { get; set; }
            public int? CheerHelpful { get; set; }
            public int? CheerCreative { get; set; }
            public int? CheerGreatHost { get; set; }
            public int? CheerSportsman { get; set; }
            public int? CheerCredit { get; set; }
            public double? Notoriety { get; set; }
            public int? SubscriberCount { get; set; }
            public bool? IsCheerful { get; set; }
        }

        public class SettingRequest
        {
            public required string Key { get; set; }
            public required string Value { get; set; }
        }

        public class AddPlatformRequest
        {
            public Platforms Platform { get; set; }
            public ulong PlatformId { get; set; }
        }

        public class UpdateRolesRequest
        {
            public bool IsScreenshare { get; set; }
            public bool IsModerator { get; set; }
            public bool IsDeveloper { get; set; }
        }
    }
}
