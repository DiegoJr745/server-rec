using Microsoft.AspNetCore.Mvc;
using sscs2023.Auth;
using sscs2023.Classes;
using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using System;
using System.IO;
using System.Threading.Tasks;
using static sscs2023.Classes.DBs.DBClasses.PlayerDBClasses;

namespace sscs2023.Controllers
{
    [ApiController]
    [Route("admin")]
    public class AdminController : ControllerBase
    {
        // error codes
        public string accountinvalid = "ACCOUNT_INVALID";
        public string playernotfound = "PLAYER_NOT_FOUND";
        public string fileexists = "FILE_ALREADY_EXISTS"; // unused for now
        public string success = "Success";


        [HttpPost("api/customcdn")]
        public async Task<IActionResult> UploadCustomCDN(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided.");
            }

            try
            {
                string uploadPath = Path.Join("Data", "cdn", "custom");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string filePath = Path.Join(uploadPath, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { message = "File uploaded successfully.", fileName = file.FileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public string roomdelfail = "ROOM_NON_EXISTENT";

        [HttpDelete("rooms/{roomId}")]

        [HttpDelete("accounts/{playerId}")]
        public IActionResult DeleteAccount(long playerId)
        {
            var player = PlayerDB.Players.FindOne(p => p.PlayerId == playerId);

            if (player == null)
            {
                return StatusCode(405, new
                {
                    errorcode = accountinvalid,
                    error = "No accounts were found associated with this id."
                });
            }

            PlayerDB.Players.Delete(playerId);

            return Ok(new
            {
                error = success,
                playerId = playerId
            });
        }

        [HttpPut("accounts/{playerId}/username")]
        public IActionResult ChangeUsername(long playerId, [FromForm] string username)
        {
            var player = PlayerDB.Players.FindOne(p => p.PlayerId == playerId);
            if (player == null)
            {
                return StatusCode(405, new
                {
                    errorcode = accountinvalid,
                    error = "No accounts were found associated with this id."
                });
            }
            player.Player.Username = username;
            player.Player.DisplayName = username;
            PlayerDB.Players.Update(player);
            return Ok(new
            {
                error = success,
                playerId = playerId,
                newUsername = username,
                newDisplayName = username
            });
        }

        [HttpPut("accounts/{playerId}/roles")]
        public IActionResult ModifyPlayerRoles(
    long playerId,
    [FromQuery] bool add = false,
    [FromQuery] bool remove = false)
        {
            var player = PlayerDB.Players.FindOne(p => p.PlayerId == playerId);

            if (player == null)
            {
                return StatusCode(405, new
                {
                    errorcode = "ACCOUNT_INVALID",
                    error = "No accounts were found associated with this id."
                });
            }

            if (add == remove)
            {
                return BadRequest(new
                {
                    error = "cant use both"
                });
            }

            if (add)
            {
                player.PlayerRoles = Enum.GetValues(typeof(PlayerRoles))
                    .Cast<PlayerRoles>()
                    .ToList();
            }


            if (remove)
            {
                player.PlayerRoles.Clear();
            }

            PlayerDB.Players.Update(player);

            return Ok(new
            {
                error = "success",
                playerId,
                newRoles = player.PlayerRoles
            });
        }

        [HttpGet("api/heartbeat")]
        public IActionResult GetPlayerHeartbeat([FromQuery] string search)
        {
            PlayerDBClasses.FullPlayer player = null;
            if (long.TryParse(search, out long playerId))
            {
                player = PlayerDB.Players.FindOne(p => p.PlayerId == playerId);
            }
            else
            {
                player = PlayerDB.Players.FindOne(p => p.Player.Username.Equals(search, StringComparison.OrdinalIgnoreCase));
            }
            if (player == null)
            {
                return NotFound(new
                {
                    errorcode = playernotfound,
                    error = "No player found with the provided search query."
                });
            }
            return Ok(new
            {
                playerId = player.PlayerId,
                username = player.Player.Username,
                lastHeartbeat = player.Player.PlayerExtra.Heartbeat
            });
        }

        [HttpPost("matchmake")] // broken dont fix i will remove soon
        public IActionResult Matchmake(
        [FromForm] long userId,
        [FromForm] long roomId,
        [FromForm] long? subRoomId = null)
        {
            var player = PlayerDB.Players.FindById(userId);
            if (player == null)
                return NotFound();

            RoomDB.IncrementRoomVisitCount(roomId);
            PlayerDB.UpdateRoomLastVisitedAt(userId, roomId);

            var session = Sessions.CreateRoom(userId, roomId, subRoomId);

            return Ok(session);
        }

        [HttpGet("api/alldatabaseinfo")]
        public IActionResult GetAllDatabaseInfo()
        {
            var allData = new
            {
                Players = Classes.DBs.PlayerDB.Players.FindAll(),
                Rooms = Classes.DBs.RoomDB.Rooms.FindAll(),
                Inventions = Classes.DBs.InventionDB.Inventions.FindAll()
            };
            return Ok(allData); // no auth yet but nobody will know these api's lets be fr
        }
    }
}
