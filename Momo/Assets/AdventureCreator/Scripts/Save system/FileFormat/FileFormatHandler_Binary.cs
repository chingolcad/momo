#if !(UNITY_WP8 || UNITY_WINRT || UNITY_WII || UNITY_PS4)
#define CAN_USE_BINARY
#endif

using System.Collections.Generic;
using System;

#if CAN_USE_BINARY
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace AC
{

	public class FileFormatHandler_Binary : iFileFormatHandler
	{

		public string GetSaveMethod ()
		{
			return "Binary";
		}


		public string GetSaveExtension ()
		{
			return ".save";
		}


		public string SerializeObject <T> (object dataObject)
		{
			#if CAN_USE_BINARY
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			MemoryStream memoryStream = new MemoryStream ();
			binaryFormatter.Serialize (memoryStream, dataObject);
			return (Convert.ToBase64String (memoryStream.GetBuffer ()));
			#else
			return "";
			#endif
		}


		public T DeserializeObject <T> (string dataString)
		{
			#if CAN_USE_BINARY
			BinaryFormatter binaryFormatter = new BinaryFormatter ();
			MemoryStream memoryStream = new MemoryStream (Convert.FromBase64String (dataString));
			return (T) binaryFormatter.Deserialize (memoryStream);
			#else
			return default (T);
			#endif
		}


		public string SerializeAllRoomData (List<SingleLevelData> dataObjects)
		{
			return SerializeObject <List<SingleLevelData>> (dataObjects);
		}


		public List<SingleLevelData> DeserializeAllRoomData (string dataString)
		{
			return (List<SingleLevelData>) DeserializeObject <List<SingleLevelData>> (dataString);
		}


		public T LoadScriptData <T> (string dataString) where T : RememberData
		{
			#if CAN_USE_BINARY
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			MemoryStream memoryStream = new MemoryStream (Convert.FromBase64String (dataString));
			T myObject;
			myObject = binaryFormatter.Deserialize (memoryStream) as T;
			return myObject;
			#else
			return null;
			#endif
		}

	}

}