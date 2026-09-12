using Microsoft.AspNetCore.Mvc;
using sscs2023.Classes;
using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using sscs2023.Auth;
using Microsoft.CodeAnalysis;

namespace sscs2023.Controllers
{
    [ApiController]
    [Route("/match")]
    public class MatchController : ControllerBase
    {
    	[HttpGet("player")]
        public IActionResult GetPlayerHeartbeatBulk(List<long> id)
        {
        	var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null) 
            	return Unauthorized();
                
            return Ok(PlayerDB.GetPlayerHeartbeatsBulk(id));
        }
        
        [HttpPost("player/login")]
        [HttpPost("player/exclusivelogin")]
        public IActionResult PlayerLogin()
        {
            return Ok();
        }
        
        [HttpPost("player/logout")]
        public IActionResult PlayerLogout()
        {
            return Ok();
        }
        
        [HttpPost("player/heartbeat")]
        public IActionResult GetPlayerHeartbeat()
        {
        	var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();
            
            return Ok(PlayerDB.GetPlayerHeartbeat((long)id));
        }
        
        [HttpPost("matchmake/none")]
        public IActionResult MatchmakeNone()
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null) 
            	return Unauthorized();
            
            return Ok(PlayerDB.GetPlayerHeartbeat((long)id));
        }
        
        [HttpPost("matchmake/dorm")]
        public IActionResult MatchmakeDorm()
        {
            var player = AuthStuff.GetCurrentPlayer(Request);
            if (player == null)
            	return Unauthorized();
            
            return Ok(Sessions.CreateDorm((long)player.PlayerId, player.Player.Username));
        }
        
        [HttpPost("matchmake/room/{roomId}")]
        public IActionResult MatchmakeRoomRoomId(long roomId, [FromQuery] long? subRoomId = null)
        {
            var player = AuthStuff.GetCurrentPlayer(Request);
            if (player == null)
            	return Unauthorized();
            
            RoomDB.IncrementRoomVisitCount(roomId);
            
            PlayerDB.UpdateRoomLastVisitedAt((long)player.PlayerId, roomId);
            
            return Ok(Sessions.CreateRoom((long)player.PlayerId, roomId, subRoomId));
        }

        [HttpPost("matchmake/room/{roomId}/{subRoomId}")]
        public IActionResult MatchmakeRoomWithSubRoom(long roomId, long subRoomId)
        {
            var player = AuthStuff.GetCurrentPlayer(Request);
            if (player == null)
            	return Unauthorized();
            
            RoomDB.IncrementRoomVisitCount(roomId);
            
            PlayerDB.UpdateRoomLastVisitedAt((long)player.PlayerId, roomId);
            
            return Ok(Sessions.CreateRoom((long)player.PlayerId, roomId, subRoomId));
        }
    }
}