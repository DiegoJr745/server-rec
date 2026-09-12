using System;
using LiteDB;

namespace sscs2023.Classes.DBs.DBClasses
{
    public class InventionDBClasses
    {
        public class Invention
        {
            [BsonId]
            public long InventionId { get; set; }
            public bool AllowTrial { get; set; } = true;
            public int CheerCount { get; set; } = 0;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public int CreatorPermission { get; set; } = 100;
            public long CreatorPlayerId { get; set; }
            public int CurrentVersionNumber { get; set; } = 1;
            public string Description { get; set; } = string.Empty;
            public int GeneralPermission { get; set; } = 100;
            public bool HideFromPlayer { get; set; } = false;
            public string ImageName { get; set; } = string.Empty;
            public bool IsAGInvention { get; set; } = false;
            public bool IsCertifiedInvention { get; set; } = false;
            public bool IsPublished { get; set; } = false;
            public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
            public string Name { get; set; } = string.Empty;
            public int NumDownloads { get; set; } = 0;
            public int NumPlayersHaveUsedInRoom { get; set; } = 0;
            public int Price { get; set; } = 0;
            public string ReplicationId { get; set; } = string.Empty;
            public List<InventionTag> Tags { get; set; } = new();
        }

        public class InventionVersion
        {
            [BsonId]
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public long InventionId { get; set; }
            public int VersionNumber { get; set; }
            public string BlobName { get; set; } = string.Empty;
            public int ChipsCost { get; set; } = 0;
            public int CloudVariablesCost { get; set; } = 0;
            public int InstantiationCost { get; set; } = 0;
            public int LightsCost { get; set; } = 0;
            public string ReplicationId { get; set; } = string.Empty;
        }

        public class InventionTag
        {
            public string Tag { get; set; } = string.Empty;
            public int Type { get; set; } = 0; // 0 = AutoTag, 1 = CustomTag
        }

        public class SaveInventionRequest
        {
            public int aiCost { get; set; }
            public int chipsCost { get; set; }
            public int cloudVariablesCost { get; set; }
            public long creationRoomId { get; set; }
            public int creatorAccountRole { get; set; }
            public string description { get; set; } = string.Empty;
            public string imageName { get; set; } = string.Empty;
            public int instantiationCost { get; set; }
            public string inventionDataFilename { get; set; } = string.Empty;
            public int lightsCost { get; set; }
            public string name { get; set; } = string.Empty;
            public List<long> referencedInventions { get; set; } = new();
        }

        public class SaveInventionResponse
        {
            public InventionData Invention { get; set; }
            public InventionVersionData InventionVersion { get; set; }
            public int Status { get; set; }
        }

        public class InventionData
        {
            public bool AllowTrial { get; set; }
            public int CheerCount { get; set; }
            public string CreatedAt { get; set; }
            public int CreatorPermission { get; set; }
            public long CreatorPlayerId { get; set; }
            public int CurrentVersionNumber { get; set; }
            public string Description { get; set; }
            public int GeneralPermission { get; set; }
            public bool HideFromPlayer { get; set; }
            public string ImageName { get; set; }
            public long InventionId { get; set; }
            public bool IsAGInvention { get; set; }
            public bool IsCertifiedInvention { get; set; }
            public bool IsPublished { get; set; }
            public string ModifiedAt { get; set; }
            public string Name { get; set; }
            public int NumDownloads { get; set; }
            public int NumPlayersHaveUsedInRoom { get; set; }
            public int Price { get; set; }
            public string ReplicationId { get; set; }
        }

        public class InventionVersionData
        {
            public string BlobName { get; set; }
            public int ChipsCost { get; set; }
            public int CloudVariablesCost { get; set; }
            public int InstantiationCost { get; set; }
            public long InventionId { get; set; }
            public int LightsCost { get; set; }
            public string ReplicationId { get; set; }
            public int VersionNumber { get; set; }
        }

        public class SetTagsRequest
        {
            public List<string> AutoTags { get; set; } = new();
            public List<string> CustomTags { get; set; } = new();
            public long InventionId { get; set; }
        }

        public class SetTagsResponse
        {
            public int Result { get; set; }
            public List<string> Tags { get; set; }
        }

        public class DetailsResponse
        {
            public List<TagData> Tags { get; set; }
        }

        public class TagData
        {
            public string Tag { get; set; }
            public int Type { get; set; }
        }
    }
}
