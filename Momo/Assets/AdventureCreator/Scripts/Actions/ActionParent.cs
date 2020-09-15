/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionParent.cs"
 * 
 *	This action is used to set and clear the parent of GameObjects.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{

	[System.Serializable]
	public class ActionParent : Action
	{

		public int parentTransformID = 0;
		public int parentTransformParameterID = -1;
		public int obToAffectID = 0;
		public int obToAffectParameterID = -1;

		public enum ParentAction { SetParent, ClearParent };
		public ParentAction parentAction;

		public Transform parentTransform;
		protected Transform runtimeParentTransform;
		
		public GameObject obToAffect;
		protected GameObject runtimeObToAffect;
		public bool isPlayer;
		
		public bool setPosition;
		public Vector3 newPosition;
		
		public bool setRotation;
		public Vector3 newRotation;


		public ActionParent ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Object;
			title = "Set parent";
			description = "Parent one GameObject to another. Can also set the child's local position and rotation.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			runtimeParentTransform = AssignFile (parameters, parentTransformParameterID, parentTransformID, parentTransform);
			runtimeObToAffect = AssignFile (parameters, obToAffectParameterID, obToAffectID, obToAffect);

			if (isPlayer && KickStarter.player)
			{
				runtimeObToAffect = KickStarter.player.gameObject;
			}
		}
		
		
		public override float Run ()
		{
			if (parentAction == ParentAction.SetParent && runtimeParentTransform)
			{
				runtimeObToAffect.transform.parent = runtimeParentTransform;
				
				if (setPosition)
				{
					runtimeObToAffect.transform.localPosition = newPosition;
				}
				
				if (setRotation)
				{
					runtimeObToAffect.transform.localRotation = Quaternion.LookRotation (newRotation);
				}
			}

			else if (parentAction == ParentAction.ClearParent)
			{
				runtimeObToAffect.transform.parent = null;
			}
			
			return 0f;
		}
		
		
		#if UNITY_EDITOR

		public override void ShowGUI (List<ActionParameter> parameters)
		{
			isPlayer = EditorGUILayout.Toggle ("Is Player?", isPlayer);
			if (!isPlayer)
			{
				obToAffectParameterID = Action.ChooseParameterGUI ("Object to affect:", parameters, obToAffectParameterID, ParameterType.GameObject);
				if (obToAffectParameterID >= 0)
				{
					obToAffectID = 0;
					obToAffect = null;
				}
				else
				{
					obToAffect = (GameObject) EditorGUILayout.ObjectField ("Object to affect:", obToAffect, typeof(GameObject), true);
					
					obToAffectID = FieldToID (obToAffect, obToAffectID);
					obToAffect = IDToField (obToAffect, obToAffectID, false);
				}
			}

			parentAction = (ParentAction) EditorGUILayout.EnumPopup ("Method:", parentAction);
			if (parentAction == ParentAction.SetParent)
			{
				parentTransformParameterID = Action.ChooseParameterGUI ("Parent to:", parameters, parentTransformParameterID, ParameterType.GameObject);
				if (parentTransformParameterID >= 0)
				{
					parentTransformID = 0;
					parentTransform = null;
				}
				else
				{
					parentTransform = (Transform) EditorGUILayout.ObjectField ("Parent to:", parentTransform, typeof(Transform), true);
					
					parentTransformID = FieldToID (parentTransform, parentTransformID);
					parentTransform = IDToField (parentTransform, parentTransformID, false);
				}
			
				setPosition = EditorGUILayout.Toggle ("Set local position?", setPosition);
				if (setPosition)
				{
					newPosition = EditorGUILayout.Vector3Field ("Position vector:", newPosition);
				}
				
				setRotation = EditorGUILayout.Toggle ("Set local rotation?", setRotation);
				if (setRotation)
				{
					newRotation = EditorGUILayout.Vector3Field ("Rotation vector:", newRotation);
				}
			}
			
			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberTransform> (obToAffect);
				if (parentTransform != null)
				{
					AddSaveScript <ConstantID> (parentTransform.gameObject);
				}
				if (obToAffect != null && obToAffect.GetComponent <RememberTransform>())
				{
					obToAffect.GetComponent <RememberTransform>().saveParent = true;

					if (obToAffect.transform.parent)
					{
						AddSaveScript <ConstantID> (obToAffect.transform.parent.gameObject);
					}
				}
			}

			AssignConstantID (obToAffect, obToAffectID, obToAffectParameterID);
			AssignConstantID (parentTransform, parentTransformID, parentTransformParameterID);
		}
		
		
		public override string SetLabel ()
		{
			if (obToAffect != null)
			{
				return obToAffect.name;
			}
			return string.Empty;
		}


		public override bool ReferencesObjectOrID (GameObject gameObject, int id)
		{
			if (parentAction == ParentAction.SetParent && parentTransformParameterID < 0)
			{
				if (parentTransform != null && parentTransform.gameObject == gameObject) return true;
				if (parentTransformID == id) return true;
			}
			if (!isPlayer && obToAffectParameterID < 0)
			{
				if (obToAffect != null && obToAffect == gameObject) return true;
				if (obToAffectID == id) return true;
			}
			if (isPlayer && gameObject.GetComponent <Player>() != null) return true;
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Set parent' Action, set to parent one GameObject to another</summary>
		 * <param name = "objectToParent">The GameObject to affect</param>
		 * <param name = "newParent">The GameObject's new Transform parent</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParent CreateNew_SetParent (GameObject objectToParent, Transform newParent)
		{
			ActionParent newAction = (ActionParent) CreateInstance <ActionParent>();
			newAction.parentAction = ParentAction.SetParent;
			newAction.obToAffect = objectToParent;
			newAction.parentTransform = newParent;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Set parent' Action, set to clear a GameObject's parent</summary>
		 * <param name = "objectToParent">The GameObject to affect</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParent CreateNew_ClearParent (GameObject objectToClear)
		{
			ActionParent newAction = (ActionParent) CreateInstance <ActionParent>();
			newAction.parentAction = ParentAction.ClearParent;
			newAction.obToAffect = objectToClear;

			return newAction;
		}

	}

}