using Microsoft.AspNetCore.Mvc;
using sscs2023.Classes;
using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using sscs2023.Auth;
using static sscs2023.Classes.DBs.DBClasses.PlayerDBClasses;

namespace sscs2023.Controllers
{
    [ApiController]
    [Route("/")]
    public class AccountController : ControllerBase
    {
        [HttpGet("acc/account/bulk")]
        public IActionResult GetAccountsBulk([FromQuery] List<long> id)
        {
            return Ok(PlayerDB.GetAccountsBulk(id));
        }

        [HttpPut("acc/account/me/profileimage")]
        public IActionResult PostAccountMeProfileImage([FromForm] string imageName)
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(imageName))
                return BadRequest("hahaha you are bad at this");

            var player = PlayerDB.Players.FindById(id.Value);
            if (player == null)
                return NotFound("Player not found.");

            player.Player.ProfileImage = imageName;
            PlayerDB.Players.Update(player);

            return Ok();
        }

        [HttpGet("acc/account/me")]
        public IActionResult GetAccountMe()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();

            var account = PlayerDB.GetAccountMe(id.Value);
            return account != null ? Ok(account) : NotFound();
        }

        [HttpPost("acc/account/me/email")]
        public IActionResult AccAccountMeEmail()
        {
            return Ok(new 
            {
                success = true
            });
        }

        [HttpGet("role/moderator/{playerId}")]
        public IActionResult CheckModerator(long playerId)
        {
            var player = PlayerDB.Players.FindById(playerId);
            if (player == null)
                return NotFound();

            var isModerator = player.PlayerRoles?.Contains(PlayerRoles.Moderator) ?? false;
            return Ok(isModerator);
        }

        [HttpGet("role/developer/{playerId}")]
        public IActionResult CheckDeveloper(long playerId)
        {
            var player = PlayerDB.Players.FindById(playerId);
            if (player == null)
                return NotFound();

            var isDeveloper = player.PlayerRoles?.Contains(PlayerRoles.Developer) ?? false;
            return Ok(isDeveloper);
        }
    }
}