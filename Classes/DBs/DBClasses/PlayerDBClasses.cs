using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using sscs2023.Classes;
using LiteDB;

namespace sscs2023.Classes.DBs.DBClasses
{
    public class PlayerDBClasses
    {
        public class FullPlayer
        {
            [BsonId]
            public long PlayerId { get; set; }
            public List<mPlatformID> PlatformIds { get; set; } = new();
            public List<string>? DeviceIds { get; set; } = new();
            public string? AuthToken { get; set; }
            public string? Password { get; set; }
            public List<PlayerRoles> PlayerRoles { get; set; } = new();
            public Player? Player { get; set; }
        }

        public class Player
        {
            public string? Username { get; set; }
            public string? DisplayName { get; set; }
            public string? Bio { get; set; }
            public int AvailableUsernameChanges { get; set; } = 3;
            public bool? IsJunior { get; set; }
            public int Level { get; set; } = 1;
            public int XP { get; set; } = 0;
            public string? ProfileImage { get; set; }
            public string? BannerImage { get; set; }
            public string? Email { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime LastLoginAt { get; set; }
            public DateTime? Birthday { get; set; }
            public CurrentAuthSession? CurrentAuthSession { get; set; } = new CurrentAuthSession();
            public Reputation Reputation { get; set; } = new Reputation();
            public List<long> VisitedRooms { get; set; } = new();
            public List<RoomVisit> RoomVisits { get; set; } = new();
            public List<long> CheeredRooms { get; set; } = new List<long>();
            public List<long> FavoritedRooms { get; set; } = new List<long>();
            public PlayerExtra PlayerExtra { get; set; } = new PlayerExtra();

            public List<PlayerRelationship> Relationships { get; set; } = new List<PlayerRelationship>();
        }

        public class RoomVisit
        {
            public long RoomId { get; set; }
            public DateTime LastVisitedAt { get; set; }
        }

        public class PlayerDTOBase
        {
            public long accountId { get; set; }
            public DateTime createdAt { get; set; }
            public string? displayName { get; set; }
            public bool? isJunior { get; set; }
            public int platforms { get; set; }
            public string? profileImage { get; set; }
            public string? username { get; set; }
            public int personalPronouns { get; set; }
            public int identityFlags { get; set; }
        }

        public class PlayerDTO : PlayerDTOBase { }
        public class PlayerMeDTO : PlayerDTOBase
        {
            public int availableUsernameChanges { get; set; } = 3;
            public DateTime? birthday { get; set; }
            public string? email { get; set; }
            public string? phone { get; set; }
        }

        public class CurrentAuthSession
        {
            public string? AuthSigningKey { get; set; }
        }

        public class PlayerRelationship
        {
            public long PlayerId { get; set; }
            public int Favorited { get; set; } = 0;
            public int Ignored { get; set; } = 0;
            public int Muted { get; set; } = 0;
            public int RelationshipType { get; set; } = 0;
        }

        public class PlayerExtra
        {
            public Avatar Avatar { get; set; } = new Avatar();
            public List<string> AvatarItems { get; set; } = new();
            public List<SavedOutfit> SavedAvatars { get; set; } = new();
            public ModerationBlockDetails? ModerationBlockDetails { get; set; } = new ModerationBlockDetails();
            public List<Setting> Settings { get; set; } = new();
            public Heartbeat Heartbeat { get; set; } = new Heartbeat();
            public List<PlayerCurrency> Currencies { get; set; } = new();
        }

        public class PlayerCurrency
        {
            public int Balance { get; set; }
            public CurrencyType CurrencyType { get; set; }
            public BalanceType BalanceType { get; set; }
        }

        public class Avatar
        {
            public string OutfitSelections { get; set; } = "";
            public string FaceFeatures { get; set; } = "";
            public string SkinColor { get; set; } = "";
            public string HairColor { get; set; } = "";
            // public byte[] ImageData { get; internal set; }
            //public string ContentType { get; internal set; }
        }

        public class Heartbeat
        {
            public string appVersion { get; set; } = ServerConfig.GameVersion.ToString();
            public DeviceClasses? deviceClass { get; set; } = DeviceClasses.Unknown;
            public MatchmakingErrorCode? errorCode { get; set; } = null;
            public bool isOnline { get; set; } = false;
            public long playerId { get; set; } = 0;
            public RoomInstance? roomInstance { get; set; } = null;
            public StatusVisibility statusVisibility { get; set; } = StatusVisibility.Online;
            public int vrMovementMode { get; set; } = 0;
        }

        public class RoomInstance
        {
            public bool encryptVoiceChat { get; set; }
            public long clubId { get; set; } = 0;
            public string? dataBlob { get; set; }
            public long eventId { get; set; } = 0;
            public bool isFull { get; set; }
            public bool isInProgress { get; set; }
            public bool isPrivate { get; set; }
            public string location { get; set; }
            public int maxCapacity { get; set; }
            public string Name { get; set; }
            public string photonRegion { get; set; }
            public string photonRegionId { get; set; }
            public string photonRoomId { get; set; }
            public string roomCode { get; set; } = "";
            public long roomId { get; set; }
            public long roomInstanceId { get; set; }
            public RoomInstanceType roomInstanceType { get; set; }
            public long subRoomId { get; set; }
        }

        public class Reputation
        {
            public long AccountId { get; set; }
            public bool IsCheerful { get; set; }
            public double Noteriety { get; set; }
            public CheerCategory SelectedCheer { get; set; }
            public int CheerCredit { get; set; }
            public int CheerGeneral { get; set; }
            public int CheerHelpful { get; set; }
            public int CheerCreative { get; set; }
            public int CheerGreatHost { get; set; }
            public int CheerSportsman { get; set; }
            public int SubscriberCount { get; set; }
            public int SubscribedCount { get; set; }
        }
        public class ModerationBlockDetails
        {
            public ReportCategory ReportCategory { get; set; } = ReportCategory.Moderator;
            public int Duration { get; set; } = 0;
            public long GameSessionId { get; set; } = 0;
            public bool? IsBan { get; set; } = false;
            public bool? IsHostKick { get; set; } = false;
            public string? Message { get; set; } = "";
            public ulong? PlayerIdReporter { get; set; } = null;
            [JsonIgnore]
            public long ModerationSetUnixTime { get; set; } = 0;
            [JsonIgnore]
            public ulong BannedByPlayerId { get; set; } = 0;
        }

        public class Setting
        {
            public required string Key { get; set; }
            public required string Value { get; set; }
        }

        public class mPlatformID
        {
            public Platforms Platform { get; set; }
            public ulong PlatformId { get; set; }
        }

        public class CachedLogins
        {
            public Platforms platform { get; set; }
            public string? platformId { get; set; }
            public long accountId { get; set; }
            public DateTime? lastLoginTime { get; set; }
            public bool requirePassword { get; set; }
        }
        
        public class PlayerProgressionDTO
        {
            public long PlayerId { get; set; }
            public int Level { get; set; }
            public int XP { get; set; }
        }

        public enum Platforms
        {
            All = -1,
            Steam,
            Oculus,
            PlayStation,
            Xbox,
            HeadlessBot,
            IOS,
            GooglePlay
        }

        public enum PlayerRoles
        {
            Screenshare,
            Moderator,
            Developer
        }

        public enum ReportCategory
        {
            Moderator = -1,
            Unknown,
            DEPRECATED_MicrophoneAbuse,
            Harassment,
            Cheating,
            DEPRECATED_ImmatureBehavior,
            AFK,
            Misc,
            Underage,
            VoteKick = 10,
            MisleadingPurchases,
            CoC_Underage = 100,
            CoC_Sexual,
            CoC_Discrimination,
            CoC_Trolling,
            CoC_NameOrProfile,
            IssuingInaccurateReports = 1000
        }

        public enum CheerCategory
        {
            General,
            Helpful = 10,
            Sportmanship = 20,
            GreatHost = 30,
            Creative = 40,
            RecRoomDeveloper = 9000
        }

        public enum MatchmakingErrorCode
        {
            UnknownError = -1,
            Success,
            NoSuchGame,
            PlayerNotOnline,
            InsufficientSpace,
            EventNotStarted,
            EventAlreadyFinished,
            BlockedFromRoom = 7,
            JuniorNotAllowed = 11,
            Banned,
            AlreadyInBestInstance,
            InsufficientRelationship,
            UpdateRequired = 16,
            AlreadyInTargetInstance,
            UGCNotAllowed = 19,
            NoSuchRoom,
            RoomIsNotActive = 22,
            RoomBlockedByCreator,
            RoomIsPrivate = 25,
            RoomInstanceIsPrivate,
            DeviceClassNotSupported = 30,
            DeviceClassNotSupportedByRoomOwner,
            MovementModeNotSupportedByRoomOwner,
            EventIsPrivate = 35,
            RoomInviteExpired = 40,
            NoAvailableRegion = 45,
            NotorietyTooPoor = 50,
            BannedFromRoom = 55,
            NoSuchRoomPlaylist = 60,
            RoomPlaylistIsNotActive,
            RoomPlaylistIsPrivate,
            NoSuchClub = 70,
            ClubHasNoClubhouse,
            ClubIsNotActive = 73,
            NotAMemberOfClub,
            BannedFromClub,
            InstanceJoinNotPermitted,
            LevelTooLow
        }

        public enum DeviceClasses
        {
            Unknown,
            VR,
            Screen,
            Mobile,
            VRLow,
            Quest2
        }

        public enum CurrencyType
        {
            Invalid,
            LaserTagTickets,
            RecCenterTokens,
            LostSkullsGold = 100,
            DraculaSilver,
            RecRoyale_Season1 = 200,
            RoomCurrency = 300
        }

        public enum BalanceType
        {
            NonPurchasedNotUsableInP2P = -2,
            NonPurchasedDefault,
            SteamPurchased,
            OculusPurchased,
            PlayStationPurchased,
            MicrosoftPurchased,
            IOSPurchased = 5,
            GooglePlayPurchased,
            PlayStationNonPurchasedP2P = 100,
            NonPlayStationNonPurchasedP2P,
            NonPurchasedEarnedByP2P = 1000
        }

        public enum StatusVisibility
        {
            Online,
            Away,
            Offline,
            Unknown = 100
        }

        public enum RoomInstanceType
        {
            Public,
            Private,
            Dormroom,
            Event,
            Meetup,
            Clubhouse
        }



        public class SavedOutfit : Avatar
        {
            public int Slot { get; set; }
            public string? PreviewImageName { get; set; }
        }
    }
}