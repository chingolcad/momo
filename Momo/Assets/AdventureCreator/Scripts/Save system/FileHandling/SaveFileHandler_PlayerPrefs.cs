#if !(UNITY_WEBPLAYER || UNITY_WINRT || UNITY_WII || UNITY_PS4 || UNITY_WSA)
#define CAN_HANDLE_SCREENSHOTS
#endif

using UnityEngine;
using System;
using System.Collections.Generic;

namespace AC
{

	public class SaveFileHandler_PlayerPrefs : iSaveFileHandler
	{

		private string screenshotKey = "_screenshot";


		public string GetDefaultSaveLabel (int saveID)
		{
			string label = "Save " + saveID.ToString ();
			if (saveID == 0)
			{
				label = "Autosave";
			}
			return label;
		}


		public void DeleteAll (int profileID)
		{
			List<SaveFile> allSaveFiles = GatherSaveFiles (profileID);
			foreach (SaveFile saveFile in allSaveFiles)
			{
				Delete (saveFile);
			}
		}


		public bool Delete (SaveFile saveFile)
		{
			string filename = saveFile.fileName;

			if (PlayerPrefs.HasKey (filename))
			{
				PlayerPrefs.DeleteKey (filename);
				ACDebug.Log ("PlayerPrefs key deleted: " + filename);

				if (KickStarter.settingsManager.takeSaveScreenshots)
				{
					if (PlayerPrefs.HasKey (filename + screenshotKey))
					{
						PlayerPrefs.DeleteKey (filename + screenshotKey);
					}
				}
				return true;
			}
			return false;
		}


		public void Save (SaveFile saveFile, string dataToSave)
		{
			string fullFilename = GetSaveFilename (saveFile.saveID, saveFile.profileID);
			bool isSuccessful = false;

			try
			{
				PlayerPrefs.SetString (fullFilename, dataToSave);
				#if UNITY_PS4
				PlayerPrefs.Save ();
				#endif
				ACDebug.Log ("PlayerPrefs key written: " + fullFilename);
				isSuccessful = true;
			}
			catch (Exception e)
 			{
				ACDebug.LogWarning ("Could not save PlayerPrefs data under key " + fullFilename + ". Exception: " + e);
 			}

 			if (isSuccessful)
 			{
 				string dateKey = fullFilename + "_timestamp";

	 			try
	 			{
			        DateTime startDate = new DateTime (2000, 1, 1, 0, 0, 0).ToUniversalTime ();

					int secs = (int) (System.DateTime.UtcNow - startDate).TotalSeconds;
			        string timestampData = secs.ToString ();

			        PlayerPrefs.SetString (dateKey, timestampData);
	 				#if UNITY_PS4
					PlayerPrefs.Save ();
					#endif
	 			}
				catch (Exception e)
	 			{
					ACDebug.LogWarning ("Could not save PlayerPrefs data under key " + dateKey + ". Exception: " + e);
	 			}
	 		}

			KickStarter.saveSystem.OnFinishSaveRequest (saveFile, isSuccessful);
		}


		public string Load (SaveFile saveFile, bool doLog)
		{
			string filename = saveFile.fileName;
			string _data = PlayerPrefs.GetString (filename, string.Empty);
			
			if (doLog && !string.IsNullOrEmpty (_data))
			{
				ACDebug.Log ("PlayerPrefs key read: " + filename);
			}

			return _data;
		}


		public List<SaveFile> GatherSaveFiles (int profileID)
		{
			return GatherSaveFiles (profileID, false, -1, "");
		}


		public List<SaveFile> GatherImportFiles (int profileID, int boolID, string separateProductName, string separateFilePrefix)
		{
			if (!string.IsNullOrEmpty (separateProductName) && !string.IsNullOrEmpty (separateFilePrefix))
			{
				return GatherSaveFiles (profileID, true, boolID, separateFilePrefix);
			}
			return null;
		}


		protected List<SaveFile> GatherSaveFiles (int profileID, bool isImport, int boolID, string separateFilePrefix)
		{
			List<SaveFile> gatheredSaveFiles = new List<SaveFile>();

			for (int i=0; i<50; i++)
			{
				bool isAutoSave = false;
				string filename = (isImport) ? GetImportFilename (i, separateFilePrefix, profileID) : GetSaveFilename (i, profileID);

				if (PlayerPrefs.HasKey (filename))
				{
					string label = "Save " + i.ToString ();
					if (i == 0)
					{
						label = "Autosave";
						isAutoSave = true;
					}

					Texture2D screenShot = null;
					if (KickStarter.settingsManager.takeSaveScreenshots && PlayerPrefs.HasKey (filename + screenshotKey) && KickStarter.saveSystem != null)
					{
						try
						{
							string screenshotData = PlayerPrefs.GetString (filename + screenshotKey);
							if (!string.IsNullOrEmpty (screenshotData))
							{
								byte[] result = Convert.FromBase64String (screenshotData);
								if (result != null)
								{
									screenShot = new Texture2D (KickStarter.saveSystem.ScreenshotWidth, KickStarter.saveSystem.ScreenshotHeight, TextureFormat.RGB24, false);
									screenShot.LoadImage (result);
									screenShot.Apply ();
								}
							}
						}
						catch (Exception e)
			 			{
							ACDebug.LogWarning ("Could not load PlayerPrefs data from key " + filename + screenshotKey + ". Exception: " + e);
			 			}
					}

					int updateTime = 0;
					if (KickStarter.settingsManager.saveTimeDisplay != SaveTimeDisplay.None)
					{
						string dateKey = filename + "_timestamp";

						if (PlayerPrefs.HasKey (dateKey))
						{
							string timestampData = PlayerPrefs.GetString (dateKey);
							if (!string.IsNullOrEmpty (timestampData))
							{
								if (int.TryParse (timestampData, out updateTime) && !isAutoSave)
								{
									DateTime startDate = new DateTime (2000, 1, 1, 0, 0, 0).ToUniversalTime ();
									DateTime saveDate = startDate.AddSeconds (updateTime);

									label += GetTimeString (saveDate);
								}
							}
						}
					}

					gatheredSaveFiles.Add (new SaveFile (i, profileID, label, filename, isAutoSave, screenShot, string.Empty, updateTime));
				}
			}

			return gatheredSaveFiles;
		}


		public void SaveScreenshot (SaveFile saveFile)
		{
			string fullFilename = GetSaveFilename (saveFile.saveID, saveFile.profileID) + screenshotKey;

			try
			{
				byte[] bytes = saveFile.screenShot.EncodeToJPG ();
				string dataToSave = Convert.ToBase64String (bytes);

				PlayerPrefs.SetString (fullFilename, dataToSave);
				#if UNITY_PS4
				PlayerPrefs.Save ();
				#endif
				ACDebug.Log ("PlayerPrefs key written: " + fullFilename);
			}
			catch (Exception e)
 			{
				ACDebug.LogWarning ("Could not save PlayerPrefs data under key " + fullFilename + ". Exception: " + e);
 			}
		}


		protected string GetSaveFilename (int saveID, int profileID = -1)
		{
			if (profileID == -1)
			{
				profileID = Options.GetActiveProfileID ();
			}

			return KickStarter.settingsManager.SavePrefix + SaveSystem.GenerateSaveSuffix (saveID, profileID);
		}


		protected string GetImportFilename (int saveID, string filePrefix, int profileID = -1)
		{
			if (profileID == -1)
			{
				profileID = Options.GetActiveProfileID ();
			}

			return filePrefix + SaveSystem.GenerateSaveSuffix (saveID, profileID);
		}


		protected string GetTimeString (System.DateTime dateTime)
		{
			if (KickStarter.settingsManager.saveTimeDisplay != SaveTimeDisplay.None)
			{
				if (KickStarter.settingsManager.saveTimeDisplay == SaveTimeDisplay.CustomFormat)
				{
					string creationTime = dateTime.ToString (KickStarter.settingsManager.customSaveFormat);
					return " (" + creationTime + ")";
				}
				else
				{
					string creationTime = dateTime.ToShortDateString ();
					if (KickStarter.settingsManager.saveTimeDisplay == SaveTimeDisplay.TimeAndDate)
					{
						creationTime += " " + dateTime.ToShortTimeString ();
					}
					return " (" + creationTime + ")";
				}
			}

			return string.Empty;
		}

	}

}