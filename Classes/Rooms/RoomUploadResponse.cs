using sscs2023.Classes.DBs;
using sscs2023.Classes.DBs.DBClasses;
using sscs2023.Controllers;

namespace sscs2023.Classes.Rooms
{
	public class RoomUploadResponse
	{
        internal bool success;
        internal string value;

        public RoomController.RoomDataResponse Room { get; set; }
		public RoomDBClasses.SubRooms SubRoomDataSave { get; set; }
	}
}
