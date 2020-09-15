#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/**
	 * Provides an EditorWindow to manage which ambience tracks can be played in-game.
	 */
	public class AmbienceStorageWindow : SoundtrackStorageWindow
	{

		[MenuItem ("Adventure Creator/Editors/Soundtrack/Ambience storage", false, 6)]
		public static void Init ()
		{
			Init <AmbienceStorageWindow> ("Ambience storage");
		}


		protected override List<MusicStorage> Storages
		{
			get
			{
				return KickStarter.settingsManager.ambienceStorages;
			}
			set
			{
				KickStarter.settingsManager.ambienceStorages = value;
			}
		}
		
		
		protected void OnGUI ()
		{
			SharedGUI ("Sound: Play ambience");

			if (KickStarter.settingsManager)
			{
				if (GUI.changed)
				{
					EditorUtility.SetDirty (KickStarter.settingsManager);
				}
			}
		}

	}
	
}

#endif