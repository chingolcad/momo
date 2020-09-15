/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MainCameraTrack.cs"
 * 
 *	A TrackAsset used by MainCameraMixer.  This is adapted from CinemachineTrack.cs, published by Unity Technologies, and all credit goes to its respective authors.
 * 
 */

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace AC
{

	[System.Serializable]
	[TrackClipType (typeof (MainCameraShot))]
	[TrackColor (0.73f, 0.1f, 0.1f)]
	/**
	 * A TrackAsset used by MainCameraMixer.  This is adapted from CinemachineTrack.cs, published by Unity Technologies, and all credit goes to its respective authors.
	 */
	public class MainCameraTrack : TrackAsset
	{

		#region Variables

		[SerializeField] private bool callCustomEvents;

		#endregion


		#region PublicFunctions

		public override Playable CreateTrackMixer (PlayableGraph graph, GameObject go, int inputCount)
		{
			foreach (TimelineClip clip in GetClips ()) 
			{
				MainCameraShot shot = (MainCameraShot) clip.asset;
				shot.callCustomEvents = callCustomEvents;
			}

			ScriptPlayable<MainCameraMixer> mixer = ScriptPlayable<MainCameraMixer>.Create (graph);
			mixer.SetInputCount (inputCount);
			return mixer;
		}

		#endregion


		#if UNITY_EDITOR

		public void ShowGUI ()
		{
			callCustomEvents = UnityEditor.EditorGUILayout.Toggle ("Calls custom events?", callCustomEvents);
			if (callCustomEvents)
			{
				UnityEditor.EditorGUILayout.HelpBox ("The OnCameraSwitch event's transition time will always be zero.", UnityEditor.MessageType.Info);
			}
		}

		#endif

	}

}