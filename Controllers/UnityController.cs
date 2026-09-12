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
    [Route("/")]
    public class UnityController : ControllerBase
    {
        [HttpPost("v1/batch/rudderstack")] // rudderstack
        public IActionResult V1BatchRubberstack()
        {
            return Ok(new {});
        }



        [HttpPost("data/heartbeat")] // data heartbeat we need to make ts work idk what to respond with
        public IActionResult DataHeartbeat()
        {
            return Ok(new {});
        }
    }
}