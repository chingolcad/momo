#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	public abstract class SoundtrackStorageWindow : EditorWindow
	{

		protected Vector2 scrollPos;


		protected static void Init <T> (string title) where T : SoundtrackStorageWindow
		{
			T window = EditorWindow.GetWindowWithRect <T> (new Rect (300, 200, 350, 360), true, title, true);
			window.titleContent.text = title;
		}


		protected virtual List<MusicStorage> Storages
		{
			get
			{
				return null;
			}
			set
			{}
		}
		
		
		protected void SharedGUI (string actionName)
		{
			if (AdvGame.GetReferences ().settingsManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}

			SettingsManager settingsManager = AdvGame.GetReferences ().settingsManager;
			EditorGUILayout.HelpBox ("Assign any music tracks you want to be able to play using the '" + actionName + "' Action here.", MessageType.Info);
			EditorGUILayout.Space ();

			scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (255f));

			List<MusicStorage> storages = Storages;
			for (int i=0; i<storages.Count; i++)
			{
				EditorGUILayout.BeginVertical ("Button");

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (storages[i].ID.ToString () + ":", EditorStyles.boldLabel);
				if (GUILayout.Button ("-", GUILayout.MaxWidth (20f)))
				{
					Undo.RecordObject (settingsManager, "Delete entry");
					storages.RemoveAt (i);
					i=0;
					return;
				}
				EditorGUILayout.EndHorizontal ();

				storages[i].audioClip = (AudioClip) EditorGUILayout.ObjectField ("Clip:", storages[i].audioClip, typeof (AudioClip), false);
				storages[i].relativeVolume = EditorGUILayout.Slider ("Relative volume:", storages[i].relativeVolume, 0f, 1f);

				EditorGUILayout.EndVertical ();
			}

			EditorGUILayout.EndScrollView ();

			if (GUILayout.Button ("Add new clip"))
			{
				Undo.RecordObject (settingsManager, "Delete music entry");
				storages.Add (new MusicStorage (GetIDArray (storages.ToArray ())));
			}

			EditorGUILayout.Space ();

			Storages = storages;
		}


		protected int[] GetIDArray (MusicStorage[] musicStorages)
		{
			List<int> idArray = new List<int>();
			foreach (MusicStorage musicStorage in musicStorages)
			{
				idArray.Add (musicStorage.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}

	}
	
}

#endif