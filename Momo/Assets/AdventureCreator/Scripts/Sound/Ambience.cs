/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Ambience.cs"
 * 
 *	This script handles the playback of Ambience when played using the 'Sound: Play ambience' Action.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script handles the playback of Ambience when played using the 'Sound: Play ambience' Action.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_ambience.html")]
	public class Ambience : Soundtrack
	{

		#region UnityStandards

		protected new void Awake ()
		{
			soundType = SoundType.SFX;
			playWhilePaused = false;
			base.Awake ();
		}

		#endregion


		#region PublicFunctions

		public override MainData SaveMainData (MainData mainData)
		{
			mainData.lastAmbienceQueueData = CreateLastSoundtrackString ();
			mainData.ambienceQueueData = CreateTimesampleString ();

			mainData.ambienceTimeSamples = 0;
			mainData.lastAmbienceTimeSamples = LastTimeSamples;

			if (GetCurrentTrackID () >= 0)
			{
				MusicStorage musicStorage = GetSoundtrack (GetCurrentTrackID ());
				if (musicStorage != null && musicStorage.audioClip != null && audioSource.clip == musicStorage.audioClip && IsPlaying ())
				{
					mainData.ambienceTimeSamples = audioSource.timeSamples;
				}
			}

			mainData.oldAmbienceTimeSamples = CreateOldTimesampleString ();

			return mainData;
		}


		public override void LoadMainData (MainData mainData)
		{
			LoadMainData (mainData.ambienceTimeSamples, mainData.oldAmbienceTimeSamples, mainData.lastAmbienceTimeSamples, mainData.lastAmbienceQueueData, mainData.ambienceQueueData);
		}

		#endregion


		#region GetSet

		protected override List<MusicStorage> Storages
		{
			get
			{
				return KickStarter.settingsManager.ambienceStorages;
			}
		}

		#endregion

	}

}