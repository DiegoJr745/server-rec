using Microsoft.AspNetCore.Mvc;
using sscs2023.Auth;
using sscs2023.Classes;
using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using static sscs2023.Classes.DBs.DBClasses.InventionDBClasses;

namespace sscs2023.Controllers
{
    [ApiController]
    public class InventionController : ControllerBase
    {
        [HttpPost("/api/inventions/v6/save")]
        public IActionResult SaveInvention([FromBody] SaveInventionRequest request)
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null)
                return Unauthorized();

            if (request == null || string.IsNullOrWhiteSpace(request.name))
                return BadRequest("Invalid request");

            var response = InventionDB.SaveInvention((long)id, request);
            
            if (response.Status != 0)
                return BadRequest("Failed to save invention");

            return Ok(response);
        }

        [HttpGet("/api/inventions/v1/details")]
        public IActionResult GetInventionDetails([FromQuery] long inventionId)
        {
            var invention = InventionDB.GetInvention(inventionId);
            if (invention == null)
                return NotFound();

            var tags = invention.Tags?.Select(t => new TagData
            {
                Tag = t.Tag,
                Type = t.Type
            }).ToList() ?? new List<TagData>();

            return Ok(new DetailsResponse { Tags = tags });
        }

        [HttpGet("/api/inventions/v2/batch")]
        public IActionResult GetInventionsBatch([FromQuery] List<long> id)
        {
            if (id == null || id.Count == 0)
                return Ok(new List<InventionData>());

            var inventions = InventionDB.GetInventionsBatch(id);
            var results = inventions.Select(inv => new InventionData
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

        [HttpGet("/api/inventions/v1/fulllineageowner")]
        public IActionResult GetFullLineageOwner([FromQuery] long id)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null)
                return Unauthorized();

            var invention = InventionDB.GetInvention(id);
            if (invention == null)
                return NotFound();

            return Ok(invention.CreatorPlayerId == playerId);
        }

        [HttpPost("/api/inventions/v1/settags")]
        public IActionResult SetInventionTags([FromBody] SetTagsRequest request)
        {
            var id = AuthStuff.GetPlayerId(Request);
            if (id == null)
                return Unauthorized();

            if (request == null || request.InventionId <= 0)
                return BadRequest("Invalid request");

            var response = InventionDB.SetTags(request.InventionId, request);
            
            if (response.Result != 0)
                return BadRequest("Failed to set tags");

            return Ok(response);
        }

        [HttpGet("/api/inventions/v1/delete")]
        public IActionResult DeleteInvention([FromQuery] long inventionId)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null)
                return Unauthorized();

            var invention = InventionDB.GetInvention(inventionId);
            if (invention == null)
                return NotFound();

            if (invention.CreatorPlayerId != playerId)
                return Forbid();

            try
            {
                var cdnPath = Path.Join(Program.dataDir, "cdn");

                if (Directory.Exists(cdnPath))
                {
                    var versions = InventionDB.Versions.Find(v => v.InventionId == inventionId).ToList();
                    foreach (var v in versions)
                    {
                        if (!string.IsNullOrWhiteSpace(v.ReplicationId))
                        {
                            var files = Directory.GetFiles(cdnPath, v.ReplicationId + "*");
                            foreach (var file in files)
                            {
                                try { System.IO.File.Delete(file); } catch { }
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(invention.ReplicationId))
                    {
                        var files = Directory.GetFiles(cdnPath, invention.ReplicationId + "*");
                        foreach (var file in files)
                        {
                            try { System.IO.File.Delete(file); } catch { }
                        }
                    }
                }

                InventionDB.Versions.DeleteMany(v => v.InventionId == inventionId);
                InventionDB.Inventions.Delete(inventionId);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeleteInvention] {ex.Message}");
                return Ok(new { success = false });
            }
        }

        [HttpGet("/api/inventions/v1/update")]
        public IActionResult UpdateInvention(
        [FromQuery] long inventionId,
        [FromQuery] string? name,
        [FromQuery] string? description,
        [FromQuery] string? imageName,
        [FromQuery] int? price,
        [FromQuery] bool? isPublished,
        [FromQuery] bool? allowTrial,
        [FromQuery] bool? hideFromPlayer,
        [FromQuery] int? generalPermission,
        [FromQuery] int? creatorPermission

)
        {
            var playerId = AuthStuff.GetPlayerId(Request);
            if (playerId == null)
                return Unauthorized();

            var invention = InventionDB.GetInvention(inventionId);
            if (invention == null)
                return NotFound();

            if (invention.CreatorPlayerId != playerId)
                return Forbid();

            if (name != null)
                invention.Name = name;

            if (description != null)
                invention.Description = description;

            if (imageName != null)
                invention.ImageName = imageName;

            if (price.HasValue)
                invention.Price = price.Value;

            if (isPublished.HasValue)
                invention.IsPublished = isPublished.Value;

            if (allowTrial.HasValue)
                invention.AllowTrial = allowTrial.Value;

            if (hideFromPlayer.HasValue)
                invention.HideFromPlayer = hideFromPlayer.Value;

            if (generalPermission.HasValue)
                invention.GeneralPermission = generalPermission.Value;

            if (creatorPermission.HasValue)
                invention.CreatorPermission = creatorPermission.Value;

            invention.ModifiedAt = DateTime.UtcNow;

            InventionDB.Inventions.Update(invention);

            return Ok(new InventionData
            {
                AllowTrial = invention.AllowTrial,
                CheerCount = invention.CheerCount,
                CreatedAt = invention.CreatedAt.ToString("O"),
                CreatorPermission = invention.CreatorPermission,
                CreatorPlayerId = invention.CreatorPlayerId,
                CurrentVersionNumber = invention.CurrentVersionNumber,
                Description = invention.Description,
                GeneralPermission = invention.GeneralPermission,
                HideFromPlayer = invention.HideFromPlayer,
                ImageName = invention.ImageName,
                InventionId = invention.InventionId,
                IsAGInvention = invention.IsAGInvention,
                IsCertifiedInvention = invention.IsCertifiedInvention,
                IsPublished = invention.IsPublished,
                ModifiedAt = invention.ModifiedAt.ToString("O"),
                Name = invention.Name,
                NumDownloads = invention.NumDownloads,
                NumPlayersHaveUsedInRoom = invention.NumPlayersHaveUsedInRoom,
                Price = invention.Price,
                ReplicationId = invention.ReplicationId
            });
        }

        [HttpGet("/api/inventions/v1/version")]
        public IActionResult GetInventionVersion([FromQuery] long inventionId, [FromQuery] int version)
        {
            var invVersion = InventionDB.GetVersion(inventionId, version);
            if (invVersion == null)
                return NotFound();

            return Ok(new InventionVersionData
            {
                BlobName = invVersion.BlobName,
                ChipsCost = invVersion.ChipsCost,
                CloudVariablesCost = invVersion.CloudVariablesCost,
                InstantiationCost = invVersion.InstantiationCost,
                InventionId = invVersion.InventionId,
                LightsCost = invVersion.LightsCost,
                ReplicationId = invVersion.ReplicationId,
                VersionNumber = invVersion.VersionNumber
            });
        }
    }
}
