/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"RememberSound.cs"
 * 
 *	This script is attached to Sound objects in the scene
 *	we wish to save.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Attach this script to Sound objects you wish to save.
	 */
	[RequireComponent (typeof (AudioSource))]
	[RequireComponent (typeof (Sound))]
	[AddComponentMenu("Adventure Creator/Save system/Remember Sound")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_sound.html")]
	public class RememberSound : Remember
	{

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			Sound sound = GetComponent <Sound>();

			SoundData soundData = new SoundData();
			soundData.objectID = constantID;
			soundData.savePrevented = savePrevented;

			soundData = sound.GetSaveData (soundData);

			return Serializer.SaveScriptData <SoundData> (soundData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 * <param name = "restoringSaveFile">True if the game is currently loading a saved game file, as opposed to just switching scene</param>
		 */
		public override void LoadData (string stringData, bool restoringSaveFile = false)
		{
			SoundData data = Serializer.LoadScriptData <SoundData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			Sound sound = GetComponent <Sound>();
			if (sound is Music) return;

			if (!restoringSaveFile && sound.surviveSceneChange)
			{
				return;
			}

			sound.LoadData (data);
		}
		
	}
	

	/**
	 * A data container used by the RememberSound script.
	 */
	[System.Serializable]
	public class SoundData : RememberData
	{

		/** True if a sound is playing */
		public bool isPlaying;
		/** True if a sound is looping */
		public bool isLooping;
		/** How far along the track a sound is */
		public int samplePoint;
		/** A unique identifier for the currently-playing AudioClip */
		public string clipID;
		/** The relative volume on the Sound component */
		public float relativeVolume;
		/** The Sound's maximum volume (internally calculated) */
		public float maxVolume;
		/** The Sound's smoothed-out volume (internally calculated) */
		public float smoothVolume;

		/** The time remaining in a fade effect */
		public float fadeTime;
		/** The original time duration of the active fade effect */
		public float originalFadeTime;
		/** The fade type, where 0 = FadeIn, 1 = FadeOut */
		public int fadeType;
		/** The volume if the Sound's soundType is SoundType.Other */
		public float otherVolume;

		/** The Sound's new relative volume, if changing over time */
		public float targetRelativeVolume;
		/** The Sound's original relative volume, if changing over time */
		public float originalRelativeVolume;
		/** The time remaining in a change in relative volume */
		public float relativeChangeTime;
		/** The original time duration of the active change in relative volume */
		public float originalRelativeChangeTime;

		/**
		 * The default Constructor.
		 */
		public SoundData () { }

	}
	
}