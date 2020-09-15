#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/**
	 * Provides an EditorWindow to manage which music tracks can be played in-game.
	 */
	public class MusicStorageWindow : SoundtrackStorageWindow
	{

		[MenuItem ("Adventure Creator/Editors/Soundtrack/Music storage", false, 6)]
		public static void Init ()
		{
			Init <MusicStorageWindow> ("Music storage");
		}


		protected override List<MusicStorage> Storages
		{
			get
			{
				return KickStarter.settingsManager.musicStorages;
			}
			set
			{
				KickStarter.settingsManager.musicStorages = value;
			}
		}
		
		
		protected void OnGUI ()
		{
			SharedGUI ("Sound: Play music");

			if (KickStarter.settingsManager)
			{
				KickStarter.settingsManager.playMusicWhilePaused = EditorGUILayout.ToggleLeft ("Music can play when game is paused?", KickStarter.settingsManager.playMusicWhilePaused);

				if (GUI.changed)
				{
					EditorUtility.SetDirty (KickStarter.settingsManager);
				}
			}
		}

	}
	
}

#endif