/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"SpeechPlayableTrack.cs"
 * 
 *	A TrackAsset used by SpeechPlayableBehaviour
 * 
 */

using UnityEngine.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	public enum SpeechTrackPlaybackMode { Natural, ClipDuration };


	/**
	 * A TrackAsset used by SpeechPlayableBehaviour
	 */
	[TrackColor(0.9f, 0.4f, 0.9f)]
	[TrackClipType(typeof(SpeechPlayableClip))]
	public class SpeechTrack : TrackAsset, ITranslatable
	{

		#region Variables

		/** If True, the line is spoken by the active Player */
		public bool isPlayerLine;
		/** The prefab of the character who is speaking the lines on this track */
		public GameObject speakerObject;
		/** The ConstantID of the speaking character, used to locate the speaker in the scene at runtime */
		public int speakerConstantID;
		/** The playback mode for speech clips played on this track */
		public SpeechTrackPlaybackMode playbackMode = SpeechTrackPlaybackMode.Natural;

		#endregion


		#region PublicFunctions

		public override Playable CreateTrackMixer (PlayableGraph graph, GameObject go, int inputCount)
	    {
	    	foreach (TimelineClip timelineClip in GetClips ())
			{
				SpeechPlayableClip clip = (SpeechPlayableClip) timelineClip.asset;
				timelineClip.displayName = clip.GetDisplayName ();

				Char speaker = null;
				if (Application.isPlaying)
				{
					if (isPlayerLine)
					{
						speaker = KickStarter.player;
					}
					else if (speakerConstantID != 0)
					{
						speaker = Serializer.returnComponent <Char> (speakerConstantID);
					}
				}
				else
				{
					if (isPlayerLine)
					{
						if (KickStarter.settingsManager != null)
						{
							speaker = KickStarter.settingsManager.GetDefaultPlayer (false);
						}
					}
					else
					{
						speaker = SpeakerPrefab;
					}
				}

				clip.speechTrackPlaybackMode = playbackMode;
				clip.speaker = speaker;
				clip.trackInstanceID = GetInstanceID ();
			}

			ScriptPlayable<SpeechPlayableMixer> mixer = ScriptPlayable<SpeechPlayableMixer>.Create (graph);
			mixer.SetInputCount (inputCount);
			mixer.GetBehaviour ().trackInstanceID = GetInstanceID ();
			mixer.GetBehaviour ().playbackMode = playbackMode;
			return mixer;
	    }


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			isPlayerLine = CustomGUILayout.Toggle ("Player line?", isPlayerLine, "", "If True, the line is spoken by the active Player");
			if (!isPlayerLine)
			{
				// For some reason, dynamically generating an ID number for a Char component destroys the component (?!), so we need to store a GameObject instead and convert to Char in the GUI
				Char speakerPrefab = (speakerObject != null) ? speakerObject.GetComponent <Char>() : null;
				speakerPrefab = (Char) CustomGUILayout.ObjectField <Char> ("Speaker prefab:", speakerPrefab, false, "", "The prefab of the character who is speaking the lines on this track");
				speakerObject = (speakerPrefab != null) ? speakerPrefab.gameObject : null;

				if (speakerObject != null)
				{
					if (speakerObject.GetComponent <ConstantID>() == null || speakerObject.GetComponent <ConstantID>().constantID == 0)
					{
						UnityVersionHandler.AddConstantIDToGameObject <ConstantID> (speakerObject, true);
					}

					if (speakerObject.GetComponent <ConstantID>())
					{
						speakerConstantID = speakerObject.GetComponent <ConstantID>().constantID;
					}

					if (speakerObject.GetComponent <ConstantID>() == null || speakerConstantID == 0)
					{
						EditorGUILayout.HelpBox ("A Constant ID number must be assigned to " + speakerObject + ".  Attach a ConstantID component and check 'Retain in prefab?'", MessageType.Warning);
					}
					else
					{
						EditorGUILayout.BeginVertical ("Button");
						EditorGUILayout.LabelField ("Recorded ConstantID: " + speakerConstantID.ToString (), EditorStyles.miniLabel);
						EditorGUILayout.EndVertical ();
					}
				}
			}

			playbackMode = (SpeechTrackPlaybackMode) EditorGUILayout.EnumPopup ("Playback mode:", playbackMode);

			if (playbackMode == SpeechTrackPlaybackMode.Natural)
			{
				EditorGUILayout.HelpBox ("Speech lines will last as long as the settings in the Speech Manager dictate.", MessageType.Info);
			}
			else if (playbackMode == SpeechTrackPlaybackMode.ClipDuration)
			{
				EditorGUILayout.HelpBox ("Speech lines will last for the duration of their associated Timeline clip.", MessageType.Info);
			}
		}

		#endif

		#endregion


		#region ITranslatable

		public string GetTranslatableString (int index)
		{
			string text = GetClip (index).speechPlayableData.messageText;
			return text;
		}


		public int GetTranslationID (int index)
		{
			int lineID = GetClip (index).speechPlayableData.lineID;
			return lineID;
		}


		#if UNITY_EDITOR

		public int GetNumTranslatables ()
		{
			return GetClipsArray ().Length;
		}


		public bool CanTranslate (int index)
		{
			string text = GetClip (index).speechPlayableData.messageText;
			return !string.IsNullOrEmpty (text);
		}


		public bool HasExistingTranslation (int index)
		{
			int lineID = GetClip (index).speechPlayableData.lineID;
			return (lineID > -1);
		}


		public void SetTranslationID (int index, int lineID)
		{
			SpeechPlayableClip clip = GetClip (index);
			clip.speechPlayableData.lineID = lineID;
			UnityEditor.EditorUtility.SetDirty (clip);
		}


		public string GetOwner (int index)
		{
			string _speaker = string.Empty;
			bool _isPlayer = isPlayerLine;
			if (!_isPlayer && SpeakerPrefab != null && SpeakerPrefab is Player)
			{
				_isPlayer = true;
			}

			if (_isPlayer)
			{
				_speaker = "Player";

				if (isPlayerLine && KickStarter.settingsManager != null && KickStarter.settingsManager.playerSwitching == PlayerSwitching.DoNotAllow && KickStarter.settingsManager.player)
				{
					_speaker = KickStarter.settingsManager.player.name;
				}
				else if (!isPlayerLine && SpeakerPrefab != null)
				{
					_speaker = SpeakerPrefab.name;
				}
			}
			else
			{
				if (SpeakerPrefab)
				{
					_speaker = SpeakerPrefab.name;
				}
				else
				{
					_speaker = "Narrator";
				}
			}

			return _speaker;
		}


		public bool OwnerIsPlayer (int index)
		{
			if (isPlayerLine)
			{
				return true;
			}

			if (SpeakerPrefab != null && SpeakerPrefab is Player)
			{
				return true;
			}

			return false;
		}


		public AC_TextType GetTranslationType (int index)
		{
			return AC_TextType.Speech;
		}

		#endif

		#endregion


		#region ProtectedFunctions

		protected SpeechPlayableClip[] GetClipsArray ()
		{
			List<SpeechPlayableClip> clipsList = new List<SpeechPlayableClip>();
			IEnumerable<TimelineClip> timelineClips = GetClips ();
			foreach (TimelineClip timelineClip in timelineClips)
			{
				if (timelineClip != null && timelineClip.asset is SpeechPlayableClip)
				{	
					clipsList.Add (timelineClip.asset as SpeechPlayableClip);
				}
			}

			return clipsList.ToArray ();
		}


		protected SpeechPlayableClip GetClip (int index)
		{
			return GetClipsArray ()[index];
		}

		#endregion


		#region GetSet

		protected Char SpeakerPrefab
		{
			get
			{
				if (speakerObject != null)
				{
					return speakerObject.GetComponent <Char>();
				}
				return null;
			}
		}

		#endregion

	}

}