/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"RememberFootstepSounds.cs"
 * 
 *	This script is attached to FootstepSound components whose change in AudioClips you wish to save. 
 * 
 */

using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace AC
{

	/**
	 * This script is attached to FootstepSound components whose change in AudioClips you wish to save. 
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Footstep Sounds")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_footstep_sounds.html")]
	public class RememberFootstepSounds : Remember
	{

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			FootstepSoundData footstepSoundData = new FootstepSoundData ();

			footstepSoundData.objectID = constantID;
			footstepSoundData.savePrevented = savePrevented;

			if (GetComponent <FootstepSounds>())
			{
				FootstepSounds footstepSounds = GetComponent <FootstepSounds>();
				footstepSoundData.walkSounds = SoundsToString (footstepSounds.footstepSounds);
				footstepSoundData.runSounds = SoundsToString (footstepSounds.runSounds);
			}

			return Serializer.SaveScriptData <FootstepSoundData> (footstepSoundData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			FootstepSoundData data = Serializer.LoadScriptData <FootstepSoundData> (stringData);
			if (data == null)
			{
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			if (GetComponent <FootstepSounds>())
			{
				FootstepSounds footstepSounds = GetComponent <FootstepSounds>();

				AudioClip[] walkSounds = StringToSounds (data.walkSounds);
				if (walkSounds != null && walkSounds.Length > 0)
				{
					footstepSounds.footstepSounds = walkSounds;
				}

				AudioClip[] runSounds = StringToSounds (data.runSounds);
				if (runSounds != null && runSounds.Length > 0)
				{
					footstepSounds.runSounds = runSounds;
				}
			}
		}


		private AudioClip[] StringToSounds (string dataString)
		{
			if (string.IsNullOrEmpty (dataString))
			{
				return null;
			}

			List<AudioClip> soundsList = new List<AudioClip>();
			
			string[] valuesArray = dataString.Split (SaveSystem.pipe[0]);
			for (int i=0; i<valuesArray.Length; i++)
			{
				string audioClipName = valuesArray[i];
				AudioClip audioClip = AssetLoader.RetrieveAudioClip (audioClipName);
				if (audioClip != null)
				{
					soundsList.Add (audioClip);
				}
			}

			return soundsList.ToArray ();
		}


		private string SoundsToString (AudioClip[] audioClips)
		{
			StringBuilder soundString = new StringBuilder ();

			for (int i=0; i<audioClips.Length; i++)
			{
				if (audioClips[i] != null)
				{
					soundString.Append (AssetLoader.GetAssetInstanceID (audioClips[i]));

					if (i < audioClips.Length-1)
					{
						soundString.Append (SaveSystem.pipe);
					}
				}
			}

			return soundString.ToString ();
		}

	}


	/**
	 * A data container used by the RememberFootstepSounds script.
	 */
	[System.Serializable]
	public class FootstepSoundData : RememberData
	{

		public string walkSounds;
		public string runSounds;

		/**
		 * The default Constructor.
		 */
		public FootstepSoundData () { }

	}

}