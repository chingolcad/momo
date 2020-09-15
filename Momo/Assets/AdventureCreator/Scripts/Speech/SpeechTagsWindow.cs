#if UNITY_EDITOR

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace AC
{
	
	/**
	 * Provides an EditorWindow to manage speech tags
	 */
	public class SpeechTagsWindow : EditorWindow
	{

		private Vector2 scrollPos;
		

		/**
		 * Initialises the window.
		 */
		public static void Init ()
		{
			SpeechTagsWindow window = EditorWindow.GetWindowWithRect <SpeechTagsWindow> (new Rect (0, 0, 450, 303), true, "Speech Tags editor", true);
			window.titleContent.text = "Speech Tags editor";
			window.position = new Rect (300, 200, 450, 303);
		}
		
		
		private void OnGUI ()
		{
			if (AdvGame.GetReferences ().speechManager == null)
			{
				EditorGUILayout.HelpBox ("A Settings Manager must be assigned before this window can display correctly.", MessageType.Warning);
				return;
			}
			
			SpeechManager speechManager = AdvGame.GetReferences ().speechManager;
			EditorGUILayout.HelpBox ("Assign any labels you want to be able to tag 'Dialogue: Play speech' Actions as here.", MessageType.Info);
			EditorGUILayout.Space ();

			speechManager.useSpeechTags = EditorGUILayout.Toggle ("Use speech tags?", speechManager.useSpeechTags);
			if (speechManager.useSpeechTags)
			{
				EditorGUILayout.BeginVertical ();
				scrollPos = EditorGUILayout.BeginScrollView (scrollPos, GUILayout.Height (205f));

				if (speechManager.speechTags.Count == 0)
				{
					speechManager.speechTags.Add (new SpeechTag ("(Untagged)"));
				}
				
				for (int i=0; i<speechManager.speechTags.Count; i++)
				{
					EditorGUILayout.BeginVertical ("Button");
					EditorGUILayout.BeginHorizontal ();

					if (i == 0)
					{
						EditorGUILayout.TextField ("Tag " + speechManager.speechTags[i].ID.ToString () + ": " + speechManager.speechTags[0].label, EditorStyles.boldLabel);
					}
					else
					{
						SpeechTag speechTag = speechManager.speechTags[i];
						speechTag.label = EditorGUILayout.TextField ("Tag " + speechManager.speechTags[i].ID.ToString () + ":", speechTag.label);
						speechManager.speechTags[i] = speechTag;

						if (GUILayout.Button ("-", GUILayout.MaxWidth (20f)))
						{
							Undo.RecordObject (speechManager, "Delete tag");
							speechManager.speechTags.RemoveAt (i);
							i=0;
							return;
						}
					}
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.EndVertical ();
				}
			
				EditorGUILayout.EndScrollView ();
				EditorGUILayout.EndVertical ();
				
				if (GUILayout.Button ("Add new tag"))
				{
					Undo.RecordObject (speechManager, "Delete tag");
					speechManager.speechTags.Add (new SpeechTag (GetIDArray (speechManager.speechTags.ToArray ())));
				}
			}

			if (GUI.changed)
			{
				EditorUtility.SetDirty (speechManager);
			}
		}
		
		
		private int[] GetIDArray (SpeechTag[] speechTags)
		{
			List<int> idArray = new List<int>();
			foreach (SpeechTag speechTag in speechTags)
			{
				idArray.Add (speechTag.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}
		
	}
	
}

#endif