using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AC
{

	[CustomEditor(typeof(Shapeable))]
	public class ShapeableEditor : Editor
	{
		
		private ShapeGroup selectedGroup;
		private ShapeKey selectedKey;
		
		
		public override void OnInspectorGUI()
		{
			Shapeable _target = (Shapeable) target;
			
			_target.shapeGroups = AllGroupsGUI (_target.shapeGroups);
			
			if (selectedGroup != null)
			{
				List<string> blendShapeNames = new List<string>();
				if (_target.GetComponent <SkinnedMeshRenderer>() && _target.GetComponent <SkinnedMeshRenderer>().sharedMesh)
				{
					for (int i=0; i<_target.GetComponent <SkinnedMeshRenderer>().sharedMesh.blendShapeCount; i++)
					{
						blendShapeNames.Add (_target.GetComponent <SkinnedMeshRenderer>().sharedMesh.GetBlendShapeName (i));
					}
				}
				else
				{
					EditorGUILayout.HelpBox ("This component should be attached to a Skinned Mesh Renderer.", MessageType.Warning);
				}

				selectedGroup = GroupGUI (selectedGroup, blendShapeNames.ToArray ());
			}
			
			UnityVersionHandler.CustomSetDirty (_target);
		}
		
		
		private ShapeGroup GroupGUI (ShapeGroup shapeGroup, string[] blendShapeNames)
		{
			EditorGUILayout.Space ();
			
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Shape group " + shapeGroup.label, EditorStyles.boldLabel);
			
			shapeGroup.label = CustomGUILayout.TextField ("Group label:", shapeGroup.label, "", "The editor-friendly name of the group");
			shapeGroup.shapeKeys = AllKeysGUI (shapeGroup.shapeKeys);
			
			if (selectedKey != null && shapeGroup.shapeKeys.Contains (selectedKey))
			{
				selectedKey = KeyGUI (selectedKey, blendShapeNames);
			}
			
			EditorGUILayout.EndVertical ();
			
			return shapeGroup;
		}
		
		
		private ShapeKey KeyGUI (ShapeKey shapeKey, string[] blendShapeNames)
		{
			EditorGUILayout.LabelField ("Shape key " + shapeKey.label, EditorStyles.boldLabel);
			
			shapeKey.label = CustomGUILayout.TextField ("Key label:", shapeKey.label, "", "An editor-friendly name of the blendshape");

			if (blendShapeNames != null && blendShapeNames.Length > 0)
			{
				shapeKey.index = CustomGUILayout.Popup ("Blendshape:", shapeKey.index, blendShapeNames, "", "The Blendshape that this relates to");
			}
			else
			{
				shapeKey.index = CustomGUILayout.IntField ("BlendShape index:", shapeKey.index, "", "The Blendshape that this relates to");
			}

			return shapeKey;
		}
		
		
		private List<ShapeGroup> AllGroupsGUI (List<ShapeGroup> shapeGroups)
		{
			EditorGUILayout.LabelField ("Shape groups", EditorStyles.boldLabel);
			
			foreach (ShapeGroup shapeGroup in shapeGroups)
			{
				EditorGUILayout.BeginHorizontal ();
				
				string buttonLabel = shapeGroup.ID + ": ";
				if (shapeGroup.label == "")
				{
					buttonLabel += "(Untitled)";	
				}
				else
				{
					buttonLabel += shapeGroup.label;
				}
				
				bool buttonOn = (selectedGroup == shapeGroup);
				if (GUILayout.Toggle (buttonOn, buttonLabel, "Button"))
				{
					if (selectedGroup != shapeGroup)
					{
						selectedGroup = shapeGroup;
						selectedKey = null;
					}
				}
				
				if (GUILayout.Button ("-", GUILayout.Width (20f), GUILayout.Height (15f)))
				{
					shapeGroups.Remove (shapeGroup);
					selectedGroup = null;
					selectedKey = null;
					break;
				}
				
				EditorGUILayout.EndHorizontal ();
			}
			
			if (GUILayout.Button ("Create new shape group"))
			{
				ShapeGroup newShapeGroup = new ShapeGroup (GetIDArray (shapeGroups));
				shapeGroups.Add (newShapeGroup);
				selectedGroup = newShapeGroup;
				selectedKey = null;
			}
			
			return shapeGroups;
		}
		
		
		private List<ShapeKey> AllKeysGUI (List<ShapeKey> shapeKeys)
		{
			EditorGUILayout.LabelField ("Shape keys", EditorStyles.boldLabel);
			
			foreach (ShapeKey shapeKey in shapeKeys)
			{
				EditorGUILayout.BeginHorizontal ();
				
				string buttonLabel = shapeKey.ID + ": ";
				if (shapeKey.label == "")
				{
					buttonLabel += "(Untitled)";	
				}
				else
				{
					buttonLabel += shapeKey.label;
				}
				
				bool buttonOn = (selectedKey == shapeKey);
				if (GUILayout.Toggle (buttonOn, buttonLabel, "Button"))
				{
					selectedKey = shapeKey;
				}
				
				if (GUILayout.Button ("-", GUILayout.Width (20f), GUILayout.Height (15f)))
				{
					shapeKeys.Remove (shapeKey);
					selectedKey = null;
					break;
				}
				
				EditorGUILayout.EndHorizontal ();
			}
			
			if (GUILayout.Button ("Create new shape key"))
			{
				ShapeKey newShapeKey = new ShapeKey (GetIDArray (shapeKeys));
				shapeKeys.Add (newShapeKey);
				selectedKey = newShapeKey;
			}
			
			return shapeKeys;
		}
		
		
		private int[] GetIDArray (List<ShapeKey> shapeKeys)
		{
			List<int> idArray = new List<int>();
			foreach (ShapeKey shapeKey in shapeKeys)
			{
				idArray.Add (shapeKey.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}
		
		
		private int[] GetIDArray (List<ShapeGroup> shapeGroups)
		{
			List<int> idArray = new List<int>();
			foreach (ShapeGroup shapeGroup in shapeGroups)
			{
				idArray.Add (shapeGroup.ID);
			}
			idArray.Sort ();
			return idArray.ToArray ();
		}
		
	}

}