/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionInstantiate.cs"
 * 
 *	This Action spawns prefabs and deletes
 *  objects from the scene
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
	public class ActionInstantiate : Action
	{
		
		public GameObject gameObject;
		public int parameterID = -1;
		public int constantID = 0; 

		public GameObject replaceGameObject;
		public int replaceParameterID = -1;
		public int replaceConstantID = 0;

		public GameObject relativeGameObject = null;
		public int relativeGameObjectID = 0;
		public int relativeGameObjectParameterID = -1;

		public int relativeVectorParameterID = -1;
		public Vector3 relativeVector = Vector3.zero;

		public int vectorVarParameterID = -1;
		public int vectorVarID;
		public VariableLocation variableLocation = VariableLocation.Global;

		public InvAction invAction;
		public PositionRelativeTo positionRelativeTo = PositionRelativeTo.Nothing;
		protected GameObject _gameObject;

		public Variables variables;
		public int variablesConstantID = 0;

		protected GVar runtimeVariable;
		protected LocalVariables localVariables;

		public int spawnedObjectParameterID = -1;
		protected ActionParameter runtimeSpawnedObjectParameter;


		public ActionInstantiate ()
		{
			this.isDisplayed = true;
			category = ActionCategory.Object;
			title = "Add or remove";
			description = "Instantiates or deletes GameObjects within the current scene. To ensure this works with save games correctly, place any prefabs to be added in a Resources asset folder.";
		}


		public override void AssignValues (List<ActionParameter> parameters)
		{
			if (invAction == InvAction.Add || invAction == InvAction.Replace)
			{
				_gameObject = AssignFile (parameters, parameterID, 0, gameObject);

				if (invAction == InvAction.Replace)
				{
					replaceGameObject = AssignFile (parameters, replaceParameterID, replaceConstantID, replaceGameObject);
				}
				else if (invAction == InvAction.Add)
				{
					relativeGameObject = AssignFile (parameters, relativeGameObjectParameterID, relativeGameObjectID, relativeGameObject);
				}
			}
			else if (invAction == InvAction.Remove)
			{
				_gameObject = AssignFile (parameters, parameterID, constantID, gameObject);
			}

			relativeVector = AssignVector3 (parameters, relativeVectorParameterID, relativeVector);

			if (invAction == InvAction.Add && positionRelativeTo == PositionRelativeTo.VectorVariable)
			{
				runtimeVariable = null;
				switch (variableLocation)
				{
					case VariableLocation.Global:
						vectorVarID = AssignVariableID (parameters, vectorVarParameterID, vectorVarID);
						runtimeVariable = GlobalVariables.GetVariable (vectorVarID, true);
						break;

					case VariableLocation.Local:
						if (!isAssetFile)
						{
							vectorVarID = AssignVariableID (parameters, vectorVarParameterID, vectorVarID);
							runtimeVariable = LocalVariables.GetVariable (vectorVarID, localVariables);
						}
						break;

					case VariableLocation.Component:
						Variables runtimeVariables = AssignFile <Variables> (variablesConstantID, variables);
						if (runtimeVariables != null)
						{
							runtimeVariable = runtimeVariables.GetVariable (vectorVarID);
						}
						runtimeVariable = AssignVariable (parameters, vectorVarParameterID, runtimeVariable);
						break;
				}
			}

			runtimeSpawnedObjectParameter = null;
			if (invAction == InvAction.Add)
			{
				runtimeSpawnedObjectParameter = GetParameterWithID (parameters, spawnedObjectParameterID);
				if (runtimeSpawnedObjectParameter != null && runtimeSpawnedObjectParameter.parameterType != ParameterType.GameObject)
				{
					runtimeSpawnedObjectParameter = null;
				}
			}
		}


		public override void AssignParentList (ActionList actionList)
		{
			if (actionList != null)
			{
				localVariables = UnityVersionHandler.GetLocalVariablesOfGameObject (actionList.gameObject);
			}
			if (localVariables == null)
			{
				localVariables = KickStarter.localVariables;
			}

			base.AssignParentList (actionList);
		}
		
		
		public override float Run ()
		{
			if (_gameObject == null)
			{
				return 0f;
			}

			if (invAction == InvAction.Add)
			{
				// Instantiate

				GameObject oldOb = AssignFile (constantID, _gameObject);
				if (_gameObject.activeInHierarchy || (oldOb != null && oldOb.activeInHierarchy))
				{
					RememberTransform rememberTransform = oldOb.GetComponent <RememberTransform>();

					if (rememberTransform != null && rememberTransform.saveScenePresence && rememberTransform.linkedPrefabID != 0)
					{
						// Bypass this check
					}
					else
					{
						LogWarning (_gameObject.name + " won't be instantiated, as it is already present in the scene.", _gameObject);
						return 0f;
					}
				}

				Vector3 position = _gameObject.transform.position;
				Quaternion rotation = _gameObject.transform.rotation;
				
				if (positionRelativeTo != PositionRelativeTo.Nothing)
				{
					float forward = _gameObject.transform.position.z;
					float right = _gameObject.transform.position.x;
					float up = _gameObject.transform.position.y;

					if (positionRelativeTo == PositionRelativeTo.RelativeToActiveCamera)
					{
						Transform mainCam = KickStarter.mainCamera.transform;
						position = mainCam.position + (mainCam.forward * forward) + (mainCam.right * right) + (mainCam.up * up);
						rotation.eulerAngles += mainCam.transform.rotation.eulerAngles;
					}
					else if (positionRelativeTo == PositionRelativeTo.RelativeToPlayer)
					{
						if (KickStarter.player)
						{
							Transform playerTranform = KickStarter.player.transform;
							position = playerTranform.position + (playerTranform.forward * forward) + (playerTranform.right * right) + (playerTranform.up * up);
							rotation.eulerAngles += playerTranform.rotation.eulerAngles;
						}
					}
					else if (positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
					{
						if (relativeGameObject != null)
						{
							Transform relativeTransform = relativeGameObject.transform;
							position = relativeTransform.position + (relativeTransform.forward * forward) + (relativeTransform.right * right) + (relativeTransform.up * up);
							rotation.eulerAngles += relativeTransform.rotation.eulerAngles;
						}
					}
					else if (positionRelativeTo == PositionRelativeTo.EnteredValue)
					{
						position += relativeVector;
					}
					else if (positionRelativeTo == PositionRelativeTo.VectorVariable)
					{
						if (runtimeVariable != null)
						{
							position += runtimeVariable.Vector3Value;
						}
					}
				}

				GameObject newObject = (GameObject) Instantiate (_gameObject, position, rotation);
				newObject.name = _gameObject.name;

				if (newObject.GetComponent <RememberTransform>())
				{
					newObject.GetComponent <RememberTransform>().OnSpawn ();
				}

				KickStarter.stateHandler.IgnoreNavMeshCollisions ();

				if (runtimeSpawnedObjectParameter != null)
				{
					runtimeSpawnedObjectParameter.SetValue (newObject);
				}
			}
			else if (invAction == InvAction.Remove)
			{
				// Delete
				KickStarter.sceneChanger.ScheduleForDeletion (_gameObject);
			}
			else if (invAction == InvAction.Replace)
			{
				if (replaceGameObject == null)
				{
					LogWarning ("Cannot perform swap because the object to remove was not found in the scene.");
					return 0f;
				}

				Vector3 position = replaceGameObject.transform.position;
				Quaternion rotation = replaceGameObject.transform.rotation;

				GameObject oldOb = AssignFile (constantID, _gameObject);
				if (gameObject.activeInHierarchy || (oldOb != null && oldOb.activeInHierarchy))
				{
					Log (_gameObject.name + " won't be instantiated, as it is already present in the scene.", _gameObject);
					return 0f;
				}

				KickStarter.sceneChanger.ScheduleForDeletion (replaceGameObject);

				GameObject newObject = (GameObject) Instantiate (_gameObject, position, rotation);
				newObject.name = _gameObject.name;
				KickStarter.stateHandler.IgnoreNavMeshCollisions ();
			}

			return 0f;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			invAction = (InvAction) EditorGUILayout.EnumPopup ("Method:", invAction);

			string _label = "Object to instantiate:";
			if (invAction == InvAction.Remove)
			{
				_label = "Object to delete:";
			}

			parameterID = Action.ChooseParameterGUI (_label, parameters, parameterID, ParameterType.GameObject);
			if (parameterID >= 0)
			{
				constantID = 0;
				gameObject = null;
			}
			else
			{
				gameObject = (GameObject) EditorGUILayout.ObjectField (_label, gameObject, typeof (GameObject), true);

				constantID = FieldToID (gameObject, constantID);
				gameObject = IDToField (gameObject, constantID, false);
			}

			if (invAction == InvAction.Add)
			{
				positionRelativeTo = (PositionRelativeTo) EditorGUILayout.EnumPopup ("Position relative to:", positionRelativeTo);

				if (positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
				{
					relativeGameObjectParameterID = Action.ChooseParameterGUI ("Relative GameObject:", parameters, relativeGameObjectParameterID, ParameterType.GameObject);
					if (relativeGameObjectParameterID >= 0)
					{
						relativeGameObjectID = 0;
						relativeGameObject = null;
					}
					else
					{
						relativeGameObject = (GameObject) EditorGUILayout.ObjectField ("Relative GameObject:", relativeGameObject, typeof (GameObject), true);
						
						relativeGameObjectID = FieldToID (relativeGameObject, relativeGameObjectID);
						relativeGameObject = IDToField (relativeGameObject, relativeGameObjectID, false);
					}
				}
				else if (positionRelativeTo == PositionRelativeTo.EnteredValue)
				{
					relativeVectorParameterID = Action.ChooseParameterGUI ("Value:", parameters, relativeVectorParameterID, ParameterType.Vector3);
					if (relativeVectorParameterID < 0)
					{
						relativeVector = EditorGUILayout.Vector3Field ("Value:", relativeVector);
					}
				}
				else if (positionRelativeTo == PositionRelativeTo.VectorVariable)
				{
					variableLocation = (VariableLocation) EditorGUILayout.EnumPopup ("Source:", variableLocation);

					switch (variableLocation)
					{
						case VariableLocation.Global:
							vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.GlobalVariable);
							if (vectorVarParameterID < 0)
							{
								vectorVarID = AdvGame.GlobalVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3);
							}
							break;

						case VariableLocation.Local:
							if (!isAssetFile)
							{
								vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.LocalVariable);
								if (vectorVarParameterID < 0)
								{
									vectorVarID = AdvGame.LocalVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3);
								}
							}
							else
							{
								EditorGUILayout.HelpBox ("Local variables cannot be accessed in ActionList assets.", MessageType.Info);
							}
							break;

						case VariableLocation.Component:
							vectorVarParameterID = Action.ChooseParameterGUI ("Vector3 variable:", parameters, vectorVarParameterID, ParameterType.ComponentVariable);
							if (vectorVarParameterID >= 0)
							{
								variables = null;
								variablesConstantID = 0;	
							}
							else
							{
								variables = (Variables) EditorGUILayout.ObjectField ("Component:", variables, typeof (Variables), true);
								variablesConstantID = FieldToID <Variables> (variables, variablesConstantID);
								variables = IDToField <Variables> (variables, variablesConstantID, false);
								
								if (variables != null)
								{
									vectorVarID = AdvGame.ComponentVariableGUI ("Vector3 variable:", vectorVarID, VariableType.Vector3, variables);
								}
							}
							break;
					}
				}

				spawnedObjectParameterID = ChooseParameterGUI ("Send to parameter:", parameters, spawnedObjectParameterID, ParameterType.GameObject);
			}
			else if (invAction == InvAction.Replace)
			{
				EditorGUILayout.Space ();
				replaceParameterID = Action.ChooseParameterGUI ("Object to delete:", parameters, replaceParameterID, ParameterType.GameObject);
				if (replaceParameterID >= 0)
				{
					replaceConstantID = 0;
					replaceGameObject = null;
				}
				else
				{
					replaceGameObject = (GameObject) EditorGUILayout.ObjectField ("Object to delete:", replaceGameObject, typeof (GameObject), true);
					
					replaceConstantID = FieldToID (replaceGameObject, replaceConstantID);
					replaceGameObject = IDToField (replaceGameObject, replaceConstantID, false);
				}
			}

			AfterRunningOption ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			if (saveScriptsToo)
			{
				AddSaveScript <RememberTransform> (replaceGameObject);
				AddSaveScript <RememberTransform> (gameObject);

				if (replaceGameObject != null && replaceGameObject.GetComponent <RememberTransform>())
				{
					replaceGameObject.GetComponent <RememberTransform>().saveScenePresence = true;
				}
				if (gameObject != null && gameObject.GetComponent <RememberTransform>())
				{
					gameObject.GetComponent <RememberTransform>().saveScenePresence = true;
				}
			}

			if (invAction == InvAction.Replace)
			{
				AssignConstantID (replaceGameObject, replaceConstantID, replaceParameterID);
			}
			else if (invAction == InvAction.Remove)
			{
				AssignConstantID (gameObject, constantID, parameterID);
			}

			if (invAction == InvAction.Add &&
				positionRelativeTo == PositionRelativeTo.VectorVariable &&
				variableLocation == VariableLocation.Component)
			{
				AssignConstantID <Variables> (variables, variablesConstantID, vectorVarParameterID);
			}
		}

		
		public override string SetLabel ()
		{
			string labelAdd = invAction.ToString ();
			if (gameObject != null)
			{
				labelAdd += " " + gameObject.name;
			}
			return labelAdd;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (parameterID < 0)
			{
				if (gameObject != null && gameObject == _gameObject) return true;
				if (constantID == id) return true;
			}
			if (invAction == InvAction.Add && positionRelativeTo == PositionRelativeTo.RelativeToGameObject)
			{
				if (relativeGameObjectParameterID < 0)
				{
					if (relativeGameObject != null && relativeGameObject == _gameObject) return true;
					if (relativeGameObjectID == id) return true;
				}
			}
			if (invAction == InvAction.Replace)
			{
				if (replaceParameterID < 0)
				{
					if (replaceGameObject != null && replaceGameObject == _gameObject) return true;
					if (replaceConstantID == id) return true;
				}
			}
			if (invAction == InvAction.Add && positionRelativeTo == PositionRelativeTo.VectorVariable)
			{
				if (variableLocation == VariableLocation.Component && vectorVarParameterID < 0)
				{
					if (variables != null && variables.gameObject == _gameObject) return true;
					if (variablesConstantID == id) return true;
				}
			}
			return false;
		}
		
		#endif


		/**
		 * <summary>Creates a new instance of the 'Object: Add or remove' Action, set to spawn a new GameObject.</summary>
		 * <param name = "prefabToAdd">The prefab to spawn</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInstantiate CreateNew_Add (GameObject prefabToAdd)
		{
			ActionInstantiate newAction = (ActionInstantiate) CreateInstance <ActionInstantiate>();
			newAction.invAction = InvAction.Add;
			newAction.gameObject = prefabToAdd;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Add or remove' Action, set to remove a GameObject from the scene.</summary>
		 * <param name = "objectToRemove">The object to remove</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInstantiate CreateNew_Remove (GameObject objectToRemove)
		{
			ActionInstantiate newAction = (ActionInstantiate) CreateInstance <ActionInstantiate>();
			newAction.invAction = InvAction.Remove;
			newAction.gameObject = objectToRemove;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'Object: Add or remove' Action, set to replace one GameObject with another</summary>
		 * <param name = "prefabToAdd">The prefab to spawn</param>
		 * <param name = "objectToRemove">The object to remove</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionInstantiate CreateNew_Replace (GameObject prefabToAdd, GameObject objectToRemove)
		{
			ActionInstantiate newAction = (ActionInstantiate) CreateInstance <ActionInstantiate>();
			newAction.invAction = InvAction.Replace;
			newAction.gameObject = prefabToAdd;
			newAction.replaceGameObject = objectToRemove;

			return newAction;
		}
		
	}

}