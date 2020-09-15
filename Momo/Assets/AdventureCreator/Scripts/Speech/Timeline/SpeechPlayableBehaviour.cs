/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"SpeechPlayableBehaviour.cs"
 * 
 *	A PlayableBehaviour that allows for AC speech playback in Timelines
 * 
 */

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

#if UNITY_EDITOR
using System.Reflection;
using System;
#endif

namespace AC
{

	/**
	 * A PlayableBehaviour that allows for AC speech playback in Timelines
	 */
	[System.Serializable]
	public class SpeechPlayableBehaviour : PlayableBehaviour
	{

		#region Variables

		protected SpeechPlayableData speechPlayableData;
		protected SpeechTrackPlaybackMode speechTrackPlaybackMode;
		protected Char speaker;
		protected bool isPlaying;
		protected int trackInstanceID;

		#endregion


		#region PublicFunctions

		public void Init (SpeechPlayableData _speechPlayableData, Char _speaker, SpeechTrackPlaybackMode _speechTrackPlaybackMode, int _trackInstanceID)
		{
			speechPlayableData = _speechPlayableData;
			speaker = _speaker;
			speechTrackPlaybackMode = _speechTrackPlaybackMode;
			trackInstanceID = _trackInstanceID;
		}


		public override void OnBehaviourPlay (Playable playable, FrameData info)
		{
			isPlaying = IsValid ();

			base.OnBehaviourPlay (playable, info);
		}


		public override void ProcessFrame (Playable playable, FrameData info, object playerData)
		{
			if (isPlaying)
			{
				isPlaying = false;

				if (Application.isPlaying)
				{
					string messageText = speechPlayableData.messageText;

					int languageNumber = Options.GetLanguage ();
					if (languageNumber > 0)
					{
						// Not in original language, so pull translation in from Speech Manager
						messageText = KickStarter.runtimeLanguages.GetTranslation (messageText, speechPlayableData.lineID, languageNumber, AC_TextType.Speech);
					}

					if (speechTrackPlaybackMode == SpeechTrackPlaybackMode.ClipDuration)
					{
						messageText += "[hold]";
					}

					KickStarter.dialog.StartDialog (speaker, messageText, false, speechPlayableData.lineID, false, true);
				}
				#if UNITY_EDITOR
				else if (KickStarter.menuPreview)
				{
					Speech previewSpeech = new Speech (speaker, speechPlayableData.messageText);
					KickStarter.menuPreview.SetPreviewSpeech (previewSpeech, trackInstanceID);
				}
				#else
				else
				{
					ACDebug.Log ("Playing speech line with track ID: " + trackInstanceID);
				}
				#endif
			}

			base.ProcessFrame (playable, info, playerData);
		}

		#endregion


		#region ProtectedFunctions

		protected bool IsValid ()
		{
			if (speechPlayableData != null && !string.IsNullOrEmpty (speechPlayableData.messageText))
			{
				return true;
			}
			return false;
		}

		#endregion


		#region GetSet

		/** The speaking character */
		public Char Speaker
		{
			get
			{
				return speaker;
			}
		}

		#endregion

	}

}