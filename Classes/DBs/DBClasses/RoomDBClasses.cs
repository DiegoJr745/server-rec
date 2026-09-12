using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using sscs2023.Classes;
using LiteDB;

namespace sscs2023.Classes.DBs.DBClasses
{
    public class RoomDBClasses
    {
        public class Room
        {
            [BsonId]
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
            public string Name { get; set; }
            public string? Description { get; set; }
            public string ImageName { get; set; }
            public WarningMaskType WarningMask { get; set; }
            public string? CustomWarning { get; set; }
            public long CreatorAccountId { get; set; }
            public RoomState? State { get; set; }
            public RoomAccessibility Accessibility { get; set; }
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
            public DateTime CreatedAt { get; set; }
            public Stats Stats { get; set; } = new Stats();
            public string? RankedEntityId { get; set; }
            public string? RankingContext { get; set; }
            public List<SubRooms> SubRooms { get; set; } = new List<SubRooms>();
            public List<Roles> Roles { get; set; } = new List<Roles>();
            public string? DataBlob { get; set; }
            public int UgcVersion { get; set; }
            public List<Tags> Tags { get; set; } = new List<Tags>();
            public List<string> PromoImages { get; set; } = new List<string>();
            public List<PromoExternalContent> PromoExternalContent { get; set; } = new List<PromoExternalContent>();
            public List<LoadScreens> LoadScreens { get; set; } = new List<LoadScreens>();
        }

        public class SubRooms
        {
            public long SubRoomId { get; set; }
            public long RoomId { get; set; }
            public string Name { get; set; }
            public string? DataBlob { get; set; }
            public bool IsSandbox { get; set; }
            public int MaxPlayers { get; set; }
            public RoomAccessibility Accessibility { get; set; }
            public string UnitySceneId { get; set; }
            public long SavedByAccountId { get; set; }
        }

        public class SubRoomSave
        {
            [BsonId]
            public long SubRoomDataSaveId { get; set; }

            public long SubRoomId { get; set; }
            public long RoomId { get; set; }

            public string DataBlob { get; set; }
            public string DataBlobHash { get; set; }

            public List<long> ReferencedUnityAssetIds { get; set; } = new();
            public List<string> UnitySubAssets { get; set; } = new();
            public List<string> ReferencedUnityAssets { get; set; } = new();

            public int PersistenceVersion { get; set; }
            public int OMVersion { get; set; }
            public int UgcSubVersion { get; set; }

            public long? SavedByAccountId { get; set; }
            public int? SavedOnPlatform { get; set; }
            public int? SavedOnDeviceClass { get; set; }

            public string? Description { get; set; }
            public List<string> Tags { get; set; } = new();

            public int ModerationState { get; set; }

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        public class Tags
        {
            public required string Tag { get; set; }
            public TagType Type { get; set; }
        }

        public class PromoExternalContent
        {
            public PromoExternalContentType Type { get; set; }
            public required string Reference { get; set; }
        }

        public class CloneRoomRequest
        {
            public string Name { get; set; }
        }

        public class LoadScreens
        {
            public string ImageName { get; set; }
            public string Title { get; set; }
            public string Subtitle { get; set; }
        }

        public class Stats
        {
            public int CheerCount { get; set; } = 0;
            public int FavoriteCount { get; set; } = 0;
            public int VisitorCount { get; set; } = 0;
            public int VisitCount { get; set; } = 0;
        }

        public class Roles
        {
            public long AccountId { get; set; }
            public Role Role { get; set; }
            public Role InvitedRole { get; set; }
        }

        public class RoomEditPermission
        {
            public bool CanEditRoom { get; set; }
            public string Error { get; set; } = string.Empty;
        }

        public enum Role : byte
        {
            None,
            Banned,
            Host = 10,
            Moderator = 20,
            CoOwner = 30,
            TemporaryCoOwner,
            Creator = 255
        }

        [Flags]
        public enum WarningMaskType
        {
            None = 0,
            Scary = 1,
            Mature = 2,
            FlashingLights = 4,
            IntenseMotion = 8,
            Violence = 16,
            Custom = 32,
            Reports = 64
        }

        public enum RoomAccessibility
        {
            Private,
            Public,
            Unlisted
        }

        public enum RoomState
        {
            Active,
            PendingJunior = 11,
            Moderation_PendingReview = 100,
            Moderation_Closed,
            MarkedForDelete = 1000
        }

        public enum TagType
        {
            General,
            Auto,
            AGOnly,
            Banned
        }

        public enum PromoExternalContentType
        {
            YouTube
        }

        public enum JoinMode
        {
            PublicMatchmaking,
            PublicNewInstance,
            PrivateNewInstance
        }
    }
}