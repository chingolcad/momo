/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"ActionParamCheck.cs"
 * 
 *	This action checks to see if a Parameter has been assigned a certain value,
 *	and performs something accordingly.
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
	public class ActionParamCheck : ActionCheck
	{

		public ActionListSource actionListSource = ActionListSource.InScene;
		public ActionListAsset actionListAsset;
		public ActionList actionList;
		public int actionListConstantID;

		public int parameterID = -1;
		public int compareParameterID = -1;

		public bool checkOwn = true;

		public int intValue;
		public float floatValue;
		public IntCondition intCondition;
		public string stringValue;
		public int compareVariableID;
		public Variables compareVariables;
		public Vector3 vector3Value;

		public GameObject compareObject;
		public int compareObjectConstantID;
		protected GameObject runtimeCompareObject;

		public Object compareUnityObject;

		public BoolValue boolValue = BoolValue.True;
		public BoolCondition boolCondition;

		public VectorCondition vectorCondition = VectorCondition.EqualTo;
		protected ActionParameter _parameter, _compareParameter;
		protected Variables runtimeCompareVariables;
		#if UNITY_EDITOR
		[SerializeField] private string parameterLabel = "";
		#endif

		[SerializeField] protected GameObjectCompareType gameObjectCompareType = GameObjectCompareType.GameObject;
		protected enum GameObjectCompareType { GameObject, ConstantID };


		public ActionParamCheck ()
		{
			this.isDisplayed = true;
			category = ActionCategory.ActionList;
			title = "Check parameter";
			description = "Queries the value of parameters defined in the parent ActionList.";
		}
		
		
		public override void AssignValues (List<ActionParameter> parameters)
		{
			_compareParameter = null;
			_parameter = null;
			runtimeCompareVariables = null;

			if (!checkOwn)
			{
				if (actionListSource == ActionListSource.InScene)
				{
					actionList = AssignFile <ActionList> (actionListConstantID, actionList);
					if (actionList != null)
					{
						if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null)
						{
							if (actionList.syncParamValues && actionList.assetFile.useParameters)
							{
								_parameter = GetParameterWithID (actionList.assetFile.GetParameters (), parameterID);
								_compareParameter = GetParameterWithID (actionList.assetFile.GetParameters (), compareParameterID);
							}
							else
							{
								_parameter = GetParameterWithID (actionList.parameters, parameterID);
								_compareParameter = GetParameterWithID (actionList.parameters, compareParameterID);
							}
						}
						else if (actionList.source == ActionListSource.InScene && actionList.useParameters)
						{
							_parameter = GetParameterWithID (actionList.parameters, parameterID);
							_compareParameter = GetParameterWithID (actionList.parameters, compareParameterID);
						}
					}
				}
				else if (actionListSource == ActionListSource.AssetFile)
				{
					if (actionListAsset != null)
					{
						_parameter = GetParameterWithID (actionListAsset.GetParameters (), parameterID);
						_compareParameter = GetParameterWithID (actionListAsset.GetParameters (), compareParameterID);
					}
				}
			}
			else
			{
				_parameter = GetParameterWithID (parameters, parameterID);
				_compareParameter = GetParameterWithID (parameters, compareParameterID);
			}

			if (_compareParameter == _parameter) _compareParameter = null;

			runtimeCompareObject = AssignFile (compareObjectConstantID, compareObject);
		}
		
		
		public override ActionEnd End (List<Action> actions)
		{
			if (_parameter == null)
			{
				return GenerateStopActionEnd ();
			}

			GVar compareVar = null;
			InvItem compareItem = null;
			Document compareDoc = null;

			if (_parameter.parameterType == ParameterType.GlobalVariable ||
				_parameter.parameterType == ParameterType.LocalVariable ||
				_parameter.parameterType == ParameterType.ComponentVariable ||
				_parameter.parameterType == ParameterType.InventoryItem ||
				_parameter.parameterType == ParameterType.Document)
			{
				if (compareVariableID == -1)
				{
					return GenerateStopActionEnd ();
				}
				
				if (_parameter.parameterType == ParameterType.GlobalVariable)
				{
					compareVar = GlobalVariables.GetVariable (compareVariableID, true);
				}
				else if (_parameter.parameterType == ParameterType.LocalVariable && !isAssetFile)
				{
					compareVar = LocalVariables.GetVariable (compareVariableID);
				}
				else if (_parameter.parameterType == ParameterType.ComponentVariable)
				{
					runtimeCompareVariables = AssignFile <Variables> (compareObjectConstantID, compareVariables);
					if (runtimeCompareVariables != null)
					{
						compareVar = runtimeCompareVariables.GetVariable (compareVariableID);
					}
				}
				else if (_parameter.parameterType == ParameterType.InventoryItem)
				{
					compareItem = KickStarter.inventoryManager.GetItem (compareVariableID);
				}
				else if (_parameter.parameterType == ParameterType.Document)
				{
					compareDoc = KickStarter.inventoryManager.GetDocument (compareVariableID);
				}
			}

			return ProcessResult (CheckCondition (compareItem, compareVar, compareDoc), actions);
		}
		
		
		protected bool CheckCondition (InvItem _compareItem, GVar _compareVar, Document _compareDoc)
		{
			if (_parameter == null)
			{
				LogWarning ("Cannot check state of variable since it cannot be found!");
				return false;
			}
			
			if (_parameter.parameterType == ParameterType.Boolean)
			{
				int fieldValue = _parameter.intValue;
				int compareValue = (int) boolValue;

				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareValue = _compareParameter.intValue;
				}

				switch (boolCondition)
				{
					case BoolCondition.EqualTo:
						return (fieldValue == compareValue);

					case BoolCondition.NotEqualTo:
						return (fieldValue != compareValue);
				}
			}
			
			else if (_parameter.parameterType == ParameterType.Integer)
			{
				int fieldValue = _parameter.intValue;
				int compareValue = intValue;

				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareValue = _compareParameter.intValue;
				}

				switch (intCondition)
				{
					case IntCondition.EqualTo:
						return (fieldValue == compareValue);

					case IntCondition.NotEqualTo:
						return (fieldValue != compareValue);

					case IntCondition.LessThan:
						return (fieldValue < compareValue);

					case IntCondition.MoreThan:
						return (fieldValue > compareValue);
				}
			}
			
			else if (_parameter.parameterType == ParameterType.Float)
			{
				float fieldValue = _parameter.floatValue;
				float compareValue = floatValue;

				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareValue = _compareParameter.floatValue;
				}

				switch (intCondition)
				{
					case IntCondition.EqualTo:
						return Mathf.Approximately (fieldValue, compareValue);

					case IntCondition.NotEqualTo:
						return !Mathf.Approximately (fieldValue, compareValue);

					case IntCondition.MoreThan:
						return (fieldValue > compareValue);

					case IntCondition.LessThan:
						return (fieldValue < compareValue);
				}
			}

			else if (_parameter.parameterType == ParameterType.Vector3)
			{
				switch (vectorCondition)
				{
					case VectorCondition.EqualTo:
						if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
						{
							return (_parameter.vector3Value == _compareParameter.vector3Value);
						}
						return (_parameter.vector3Value == vector3Value);

					case VectorCondition.MagnitudeGreaterThan:
						if (_compareParameter != null && _compareParameter.parameterType == ParameterType.Float)
						{
							return (_parameter.vector3Value.magnitude > _compareParameter.floatValue);
						}
						return (_parameter.vector3Value.magnitude > floatValue);
				}
			}
			
			else if (_parameter.parameterType == ParameterType.String)
			{
				string fieldValue = _parameter.stringValue;
				string compareValue = AdvGame.ConvertTokens (stringValue);

				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareValue = _compareParameter.stringValue;
				}

				switch (boolCondition)
				{
					case BoolCondition.EqualTo:
						return (fieldValue == compareValue);

					case BoolCondition.NotEqualTo:
						return (fieldValue != compareValue);
				}
			}

			else if (_parameter.parameterType == ParameterType.GameObject)
			{
				switch (gameObjectCompareType)
				{
					case GameObjectCompareType.GameObject:
					{
						if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
						{
							compareObjectConstantID = _compareParameter.intValue;
							runtimeCompareObject = _compareParameter.gameObject;
						}

						if ((runtimeCompareObject != null && _parameter.gameObject == runtimeCompareObject) ||
							(compareObjectConstantID != 0 && _parameter.intValue == compareObjectConstantID))
						{
							return true;
						}
						if (runtimeCompareObject == null && _parameter.gameObject == null)
						{
							return true;
						}
						break;
					}

					case GameObjectCompareType.ConstantID:
					{
						int compareValue = intValue;
						if (_compareParameter != null && _compareParameter.parameterType == ParameterType.Integer)
						{
							compareValue = _compareParameter.intValue;
						}

						return (_parameter.intValue == compareValue);
					}
				}


			}

			else if (_parameter.parameterType == ParameterType.UnityObject)
			{
				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					compareUnityObject = _compareParameter.objectValue;
				}

				if (compareUnityObject != null && _parameter.objectValue == (Object) compareUnityObject)
				{
					return true;
				}
				if (compareUnityObject == null && _parameter.objectValue == null)
				{
					return true;
				}
			}

			else if (_parameter.parameterType == ParameterType.GlobalVariable || _parameter.parameterType == ParameterType.LocalVariable)
			{
				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					return (_compareParameter.intValue == _parameter.intValue);
				}

				if (_compareVar != null && _parameter.intValue == _compareVar.id)
				{
					return true;
				}
			}

			else if (_parameter.parameterType == ParameterType.ComponentVariable)
			{
				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					return (_compareParameter.intValue == _parameter.intValue && _compareParameter.variables == _parameter.variables);
				}

				if (_compareVar != null && _parameter.intValue == _compareVar.id && _parameter.variables == runtimeCompareVariables)
				{
					return true;
				}
			}

			else if (_parameter.parameterType == ParameterType.InventoryItem)
			{
				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					return (_compareParameter.intValue == _parameter.intValue);
				}

				if (_compareItem != null && _parameter.intValue == _compareItem.id)
				{
					return true;
				}
			}

			else if (_parameter.parameterType == ParameterType.Document)
			{
				if (_compareParameter != null && _compareParameter.parameterType == _parameter.parameterType)
				{
					return (_compareParameter.intValue == _parameter.intValue);
				}

				if (_compareDoc != null && _parameter.intValue == _compareDoc.ID)
				{
					return true;
				}
			}

			return false;
		}
		
		
		#if UNITY_EDITOR
		
		public override void ShowGUI (List<ActionParameter> parameters)
		{
			checkOwn = EditorGUILayout.Toggle ("Check own?", checkOwn);
			if (checkOwn)
			{
				parameterID = Action.ChooseParameterGUI (parameters, parameterID);
				ShowVarGUI (parameters, GetParameterWithID (parameters, parameterID), isAssetFile);
			}
			else
			{
				actionListSource = (ActionListSource) EditorGUILayout.EnumPopup ("Source:", actionListSource);
				if (actionListSource == ActionListSource.InScene)
				{
					actionList = (ActionList) EditorGUILayout.ObjectField ("ActionList:", actionList, typeof (ActionList), true);
					
					actionListConstantID = FieldToID <ActionList> (actionList, actionListConstantID);
					actionList = IDToField <ActionList> (actionList, actionListConstantID, true);

					if (actionList != null)
					{
						if (actionList.source == ActionListSource.InScene)
						{
							if (actionList.NumParameters > 0)
							{
								parameterID = Action.ChooseParameterGUI (actionList.parameters, parameterID);
								ShowVarGUI (actionList.parameters, GetParameterWithID (actionList.parameters, parameterID), false);
							}
							else
							{
								EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
							}
						}
						else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null)
						{
							if (actionList.assetFile.NumParameters > 0)
							{
								parameterID = Action.ChooseParameterGUI (actionList.assetFile.DefaultParameters, parameterID);
								ShowVarGUI (actionList.assetFile.DefaultParameters, GetParameterWithID (actionList.assetFile.DefaultParameters, parameterID), true);
							}
							else
							{
								EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
							}
						}
						else
						{
							EditorGUILayout.HelpBox ("This ActionList has no parameters defined!", MessageType.Warning);
						}
					}
				}
				else if (actionListSource == ActionListSource.AssetFile)
				{
					actionListAsset = (ActionListAsset) EditorGUILayout.ObjectField ("ActionList asset:", actionListAsset, typeof (ActionListAsset), true);
					if (actionListAsset != null)
					{
						if (actionListAsset.NumParameters > 0)
						{
							parameterID = Action.ChooseParameterGUI (actionListAsset.GetParameters (), parameterID);
							ShowVarGUI (actionListAsset.GetParameters (), GetParameterWithID (actionListAsset.GetParameters (), parameterID), true);
						}
						else
						{
							EditorGUILayout.HelpBox ("This ActionList Asset has no parameters defined!", MessageType.Warning);
						}
					}
				}
			}
		}
		
		
		private void ShowVarGUI (List<ActionParameter> parameters, ActionParameter parameter, bool inAsset)
		{
			if (parameters == null || parameters.Count == 0 || parameter == null)
			{
				EditorGUILayout.HelpBox ("No parameters exist! Please define one in the Inspector.", MessageType.Warning);
				parameterLabel = "";
				return;
			}

			parameterLabel = parameter.label;
			EditorGUILayout.BeginHorizontal ();

			if (parameter.parameterType == ParameterType.Boolean)
			{
				boolCondition = (BoolCondition) EditorGUILayout.EnumPopup (boolCondition);

				compareParameterID = Action.ChooseParameterGUI ("", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				if (compareParameterID < 0)
				{
					boolValue = (BoolValue) EditorGUILayout.EnumPopup (boolValue);
				}
			}
			else if (parameter.parameterType == ParameterType.Integer)
			{
				intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

				compareParameterID = Action.ChooseParameterGUI ("", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				if (compareParameterID < 0)
				{
					intValue = EditorGUILayout.IntField (intValue);
				}
			}
			else if (parameter.parameterType == ParameterType.Float)
			{
				intCondition = (IntCondition) EditorGUILayout.EnumPopup (intCondition);

				compareParameterID = Action.ChooseParameterGUI ("", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				if (compareParameterID < 0)
				{
					floatValue = EditorGUILayout.FloatField (floatValue);
				}
			}
			else if (parameter.parameterType == ParameterType.String)
			{
				boolCondition = (BoolCondition) EditorGUILayout.EnumPopup (boolCondition);

				compareParameterID = Action.ChooseParameterGUI ("", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				if (compareParameterID < 0)
				{
					stringValue = EditorGUILayout.TextField (stringValue);
				}
			}
			else if (parameter.parameterType == ParameterType.Vector3)
			{
				vectorCondition = (VectorCondition) EditorGUILayout.EnumPopup ("Condition:", vectorCondition);

				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();

				if (vectorCondition == VectorCondition.MagnitudeGreaterThan)
				{
					compareParameterID = Action.ChooseParameterGUI ("Float:", parameters, compareParameterID, ParameterType.Float, parameter.ID);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					if (compareParameterID < 0)
					{
						floatValue = EditorGUILayout.FloatField ("Float:", floatValue);
					}
				}
				else if (vectorCondition == VectorCondition.EqualTo)
				{
					compareParameterID = Action.ChooseParameterGUI ("Vector3", parameters, compareParameterID, parameter.parameterType, parameter.ID);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					if (compareParameterID < 0)
					{
						EditorGUILayout.BeginHorizontal ();
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.LabelField ("Vector3:", GUILayout.MaxWidth (60f));
						vector3Value = EditorGUILayout.Vector3Field ("", vector3Value);
					}
				}
			}
			else if (parameter.parameterType == ParameterType.GameObject)
			{
				if (inAsset)
				{
					EditorGUILayout.EndHorizontal ();
					gameObjectCompareType = (GameObjectCompareType) EditorGUILayout.EnumPopup ("Compare:", gameObjectCompareType);
					EditorGUILayout.BeginHorizontal ();
				}
				else
				{
					gameObjectCompareType = GameObjectCompareType.GameObject;
				}

				switch (gameObjectCompareType)
				{
					case GameObjectCompareType.GameObject:
					{
						compareParameterID = Action.ChooseParameterGUI ("Is equal to:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal ();
						if (compareParameterID < 0)
						{
							compareObject = (GameObject) EditorGUILayout.ObjectField ("Is equal to:", compareObject, typeof (GameObject), true);

							EditorGUILayout.EndHorizontal ();
							EditorGUILayout.BeginHorizontal ();
							compareObjectConstantID = FieldToID (compareObject, compareObjectConstantID);
							compareObject = IDToField (compareObject, compareObjectConstantID, false);
						}
						break;
					}

					case GameObjectCompareType.ConstantID:
					{
						compareParameterID = Action.ChooseParameterGUI ("Is equal to:", parameters, compareParameterID, ParameterType.Integer, parameter.ID);
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal ();
						if (compareParameterID < 0)
						{
							intValue = EditorGUILayout.IntField ("Is equal to:", intValue);

							EditorGUILayout.EndHorizontal ();
							EditorGUILayout.BeginHorizontal ();
						}
						break;
					}
				}
			}
			else if (parameter.parameterType == ParameterType.UnityObject)
			{
				compareParameterID = Action.ChooseParameterGUI ("Is equal to:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					compareUnityObject = (Object) EditorGUILayout.ObjectField ("Is equal to:", compareUnityObject, typeof (Object), true);
				}
			}
			else if (parameter.parameterType == ParameterType.GlobalVariable)
			{
				compareParameterID = Action.ChooseParameterGUI ("Is global variable:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					if (AdvGame.GetReferences ().variablesManager == null || AdvGame.GetReferences ().variablesManager.vars == null || AdvGame.GetReferences ().variablesManager.vars.Count == 0)
					{
						EditorGUILayout.HelpBox ("No Global variables exist!", MessageType.Info);
					}
					else
					{
						compareVariableID = ShowVarSelectorGUI (AdvGame.GetReferences ().variablesManager.vars, compareVariableID);
					}
				}
			}
			else if (parameter.parameterType == ParameterType.ComponentVariable)
			{
				compareParameterID = Action.ChooseParameterGUI ("Is component variable:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					compareVariables = (Variables) EditorGUILayout.ObjectField ("Component:", compareVariables, typeof (Variables), true);
					compareObjectConstantID = FieldToID <Variables> (compareVariables, compareObjectConstantID);
					compareVariables = IDToField <Variables> (compareVariables, compareObjectConstantID, false);

					if (compareVariables != null)
					{
						EditorGUILayout.EndHorizontal ();
						EditorGUILayout.BeginHorizontal ();
						compareVariableID = ShowVarSelectorGUI (compareVariables.vars, compareVariableID);
					}
				}
			}
			else if (parameter.parameterType == ParameterType.InventoryItem)
			{
				compareParameterID = Action.ChooseParameterGUI ("Is inventory item:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					compareVariableID = ShowInvSelectorGUI (compareVariableID);
				}
			}
			else if (parameter.parameterType == ParameterType.Document)
			{
				compareParameterID = Action.ChooseParameterGUI ("Is document:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					compareVariableID = ShowDocSelectorGUI (compareVariableID);
				}
			}
			else if (parameter.parameterType == ParameterType.LocalVariable)
			{
				compareParameterID = Action.ChooseParameterGUI ("Is local variable:", parameters, compareParameterID, parameter.parameterType, parameter.ID);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				if (compareParameterID < 0)
				{
					if (isAssetFile)
					{
						EditorGUILayout.HelpBox ("Cannot compare local variables in an asset file.", MessageType.Warning);
					}
					else if (KickStarter.localVariables == null || KickStarter.localVariables.localVars == null || KickStarter.localVariables.localVars.Count == 0)
					{
						EditorGUILayout.HelpBox ("No Local variables exist!", MessageType.Info);
					}
					else
					{
						compareVariableID = ShowVarSelectorGUI (KickStarter.localVariables.localVars, compareVariableID);
					}
				}
			}

			EditorGUILayout.EndHorizontal ();
		}


		public override void AssignConstantIDs (bool saveScriptsToo, bool fromAssetFile)
		{
			AssignConstantID (compareObject, compareObjectConstantID, 0);
		}
		
		
		public override string SetLabel ()
		{
			return parameterLabel;
		}


		private int ShowVarSelectorGUI (List<GVar> vars, int ID)
		{
			int variableNumber = -1;
			
			List<string> labelList = new List<string>();
			foreach (GVar _var in vars)
			{
				labelList.Add (_var.label);
			}
			
			variableNumber = GetVarNumber (vars, ID);
			
			if (variableNumber == -1)
			{
				// Wasn't found (variable was deleted?), so revert to zero
				ACDebug.LogWarning ("Previously chosen variable no longer exists!");
				variableNumber = 0;
				ID = 0;
			}
			
			variableNumber = EditorGUILayout.Popup ("Variable:", variableNumber, labelList.ToArray());
			ID = vars[variableNumber].id;
			
			return ID;
		}
		
		
		private int ShowInvSelectorGUI (int ID)
		{
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			if (inventoryManager == null)
			{
				return ID;
			}
			
			int invNumber = -1;
			List<string> labelList = new List<string>();
			int i=0;
			foreach (InvItem _item in inventoryManager.items)
			{
				labelList.Add (_item.label);
				
				// If an item has been removed, make sure selected variable is still valid
				if (_item.id == ID)
				{
					invNumber = i;
				}
				
				i++;
			}
			
			if (invNumber == -1)
			{
				// Wasn't found (item was possibly deleted), so revert to zero
				ACDebug.LogWarning ("Previously chosen item no longer exists!");
				
				invNumber = 0;
				ID = 0;
			}
			
			invNumber = EditorGUILayout.Popup ("Is inventory item:", invNumber, labelList.ToArray());
			ID = inventoryManager.items[invNumber].id;
			
			return ID;
		}


		private int ShowDocSelectorGUI (int ID)
		{
			InventoryManager inventoryManager = AdvGame.GetReferences ().inventoryManager;
			if (inventoryManager == null)
			{
				return ID;
			}
			
			int docNumber = -1;
			List<string> labelList = new List<string>();
			int i=0;
			foreach (Document _document in inventoryManager.documents)
			{
				labelList.Add (_document.Title);
				
				// If an item has been removed, make sure selected variable is still valid
				if (_document.ID == ID)
				{
					docNumber = i;
				}
				
				i++;
			}
			
			if (docNumber == -1)
			{
				// Wasn't found (item was possibly deleted), so revert to zero
				ACDebug.LogWarning ("Previously chosen Document no longer exists!");
				
				docNumber = 0;
				ID = 0;
			}
			
			docNumber = EditorGUILayout.Popup ("Is document:", docNumber, labelList.ToArray());
			ID = inventoryManager.documents[docNumber].ID;
			
			return ID;
		}
		
		
		private int GetVarNumber (List<GVar> vars, int ID)
		{
			int i = 0;
			foreach (GVar _var in vars)
			{
				if (_var.id == ID)
				{
					return i;
				}
				i++;
			}
			return -1;
		}


		public override int GetVariableReferences (List<ActionParameter> parameters, VariableLocation location, int varID, Variables _variables)
		{
			int thisCount = 0;

			ActionParameter _param = null;
			if (checkOwn)
			{
				if (parameters != null)
				{
					_param = GetParameterWithID (parameters, parameterID);
				}
			}
			else
			{
				if (actionListSource == ActionListSource.InScene && actionList != null)
				{
					if (actionList.source == ActionListSource.InScene && actionList.useParameters)
					{
						_param = GetParameterWithID (actionList.parameters, parameterID);
					}
					else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
					{
						_param = GetParameterWithID (actionList.assetFile.DefaultParameters, parameterID);
					}
				}
				else if (actionListSource == ActionListSource.AssetFile && actionListAsset != null && actionListAsset.useParameters)
				{
					_param = GetParameterWithID (actionListAsset.DefaultParameters, parameterID);
				}
			}


			if (_param != null && _param.parameterType == ParameterType.LocalVariable && location == VariableLocation.Local && varID == intValue)
			{
				thisCount ++;
			}
			else if (_param != null && _param.parameterType == ParameterType.GlobalVariable && location == VariableLocation.Global && varID == intValue)
			{
				thisCount ++;
			}
			else if (_param != null && _param.parameterType == ParameterType.ComponentVariable && location == VariableLocation.Component && varID == intValue && _param.variables == _variables)
			{
				thisCount ++;
			}

			thisCount += base.GetVariableReferences (parameters, location, varID, _variables);
			return thisCount;
		}


		public override int GetInventoryReferences (List<ActionParameter> parameters, int _invID)
		{
			return GetParamReferences (parameters, _invID, ParameterType.InventoryItem);
		}


		public override int GetDocumentReferences (List<ActionParameter> parameters, int _docID)
		{
			return GetParamReferences (parameters, _docID, ParameterType.Document);
		}


		private int GetParamReferences (List<ActionParameter> parameters, int _ID, ParameterType _paramType)
		{
			ActionParameter _param = null;

			if (checkOwn)
			{
				if (parameters != null)
				{
					_param = GetParameterWithID (parameters, parameterID);
				}
			}
			else
			{
				if (actionListSource == ActionListSource.InScene && actionList != null)
				{
					if (actionList.source == ActionListSource.InScene && actionList.useParameters)
					{
						_param = GetParameterWithID (actionList.parameters, parameterID);
					}
					else if (actionList.source == ActionListSource.AssetFile && actionList.assetFile != null && actionList.assetFile.useParameters)
					{
						_param = GetParameterWithID (actionList.assetFile.DefaultParameters, parameterID);
					}
				}
				else if (actionListSource == ActionListSource.AssetFile && actionListAsset != null && actionListAsset.useParameters)
				{
					_param = GetParameterWithID (actionListAsset.DefaultParameters, parameterID);
				}
			}

			if (_param != null && _param.parameterType == _paramType && _ID == intValue)
			{
				return 1;
			}

			return 0;
		}


		public override bool ReferencesObjectOrID (GameObject _gameObject, int id)
		{
			if (!checkOwn && actionListSource == ActionListSource.InScene)
			{
				if (actionList != null && actionList.gameObject == _gameObject) return true;
				if (actionListConstantID == id) return true;
			}
			if (compareParameterID < 0)
			{
				if (compareObject != null && compareObject == _gameObject) return true;
				if (compareObjectConstantID == id) return true;
			}
			return false;
		}


		public override bool ReferencesAsset (ActionListAsset _actionListAsset)
		{
			if (!checkOwn && actionListSource == ActionListSource.AssetFile && _actionListAsset == actionListAsset)
				return true;
			return false;
		}

		#endif


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Bool parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the Bool parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (int parameterID, bool checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = true;
			newAction.parameterID = parameterID;

			newAction.boolValue = (checkValue) ? BoolValue.True : BoolValue.False;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Bool parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Bool parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionList actionList, int parameterID, bool checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.boolValue = (checkValue) ? BoolValue.True : BoolValue.False;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Bool parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Bool parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionListAsset actionListAsset, int parameterID, bool checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.boolValue = (checkValue) ? BoolValue.True : BoolValue.False;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check an Integer parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the Integer parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <param name = "condition">The condition query to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (int parameterID, int checkValue, IntCondition condition = IntCondition.EqualTo)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = true;
			newAction.parameterID = parameterID;

			newAction.intValue = checkValue;
			newAction.intCondition = condition;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check an Integer parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Integer parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <param name = "condition">The condition query to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionList actionList, int parameterID, int checkValue, IntCondition condition = IntCondition.EqualTo)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.intValue = checkValue;
			newAction.intCondition = condition;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check an Integer parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Integer parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <param name = "condition">The condition query to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionListAsset actionListAsset, int parameterID, int checkValue, IntCondition condition = IntCondition.EqualTo)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.intValue = checkValue;
			newAction.intCondition = condition;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Float parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the Float parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <param name = "condition">The condition query to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (int parameterID, float checkValue, IntCondition condition = IntCondition.EqualTo)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = true;
			newAction.parameterID = parameterID;

			newAction.floatValue = checkValue;
			newAction.intCondition = condition;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Float parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Float parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <param name = "condition">The condition query to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionList actionList, int parameterID, float checkValue, IntCondition condition = IntCondition.EqualTo)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.floatValue = checkValue;
			newAction.intCondition = condition;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Float parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Float parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <param name = "condition">The condition query to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionListAsset actionListAsset, int parameterID, float checkValue, IntCondition condition = IntCondition.EqualTo)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.floatValue = checkValue;
			newAction.intCondition = condition;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Float parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the Float parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (int parameterID, string checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = true;
			newAction.parameterID = parameterID;

			newAction.stringValue = checkValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a String parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the String parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionList actionList, int parameterID, string checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.stringValue = checkValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a String parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the String parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionListAsset actionListAsset, int parameterID, string checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.stringValue = checkValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Vector3 parameter on its own ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Vector3 parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <param name = "condition">The condition query to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (int parameterID, Vector3 checkValue, VectorCondition condition = VectorCondition.EqualTo)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = true;
			newAction.parameterID = parameterID;

			newAction.vector3Value = checkValue;
			newAction.floatValue = checkValue.magnitude;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Vector3 parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Vector3 parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <param name = "condition">The condition query to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionList actionList, int parameterID, Vector3 checkValue, VectorCondition condition = VectorCondition.EqualTo)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.vector3Value = checkValue;
			newAction.floatValue = checkValue.magnitude;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Vector3 parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Vector3 parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <param name = "condition">The condition query to make</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionListAsset actionListAsset, int parameterID, Vector3 checkValue, VectorCondition condition = VectorCondition.EqualTo)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.vector3Value = checkValue;
			newAction.floatValue = checkValue.magnitude;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Variable parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the Variable parameter</param>
		 * <param name = "variables">The Variables component to check for</param>
		 * <param name = "checkComponentVariableID">The Variable ID to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (int parameterID, Variables variables, int checkComponentVariableID)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = true;
			newAction.parameterID = parameterID;

			newAction.compareVariables = variables;
			newAction.compareVariableID = checkComponentVariableID;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Variable parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Variable parameter</param>
		 * <param name = "variables">The Variables component to check for</param>
		 * <param name = "checkComponentVariableID">The Variable ID to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionList actionList, int parameterID, Variables variables, int checkComponentVariableID)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.compareVariables = variables;
			newAction.compareVariableID = checkComponentVariableID;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a Variable parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the Variable parameter</param>
		 * <param name = "variables">The Variables component to check for</param>
		 * <param name = "checkComponentVariableID">The Variable ID to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionListAsset actionListAsset, int parameterID, Variables variables, int checkComponentVariableID)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.compareVariables = variables;
			newAction.compareVariableID = checkComponentVariableID;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a GameObject parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the GameObject parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (int parameterID, GameObject checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = true;
			newAction.parameterID = parameterID;

			newAction.compareObject = checkValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a GameObject parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the GameObject parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionList actionList, int parameterID, GameObject checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.compareObject = checkValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a GameObject parameter on its own ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the GameObject parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */		
		public static ActionParamCheck CreateNew (ActionListAsset actionListAsset, int parameterID, GameObject checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.compareObject = checkValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a UnityObject parameter on its own ActionList</summary>
		 * <param name = "parameterID">The ID number of the UnityObject parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (int parameterID, Object checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = true;
			newAction.parameterID = parameterID;

			newAction.compareUnityObject = checkValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a UnityObject parameter on another ActionList</summary>
		 * <param name = "actionList">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the UnityObject parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionList actionList, int parameterID, Object checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.InScene;
			newAction.actionList = actionList;
			newAction.parameterID = parameterID;

			newAction.compareUnityObject = checkValue;

			return newAction;
		}


		/**
		 * <summary>Creates a new instance of the 'ActionList: Check parameter' Action, set to check a UnityObject parameter on another ActionList</summary>
		 * <param name = "actionListAsset">The ActionList with the parameter</param>
		 * <param name = "parameterID">The ID number of the UnityObject parameter</param>
		 * <param name = "checkValue">The value to check for</param>
		 * <returns>The generated Action</returns>
		 */
		public static ActionParamCheck CreateNew (ActionListAsset actionListAsset, int parameterID, Object checkValue)
		{
			ActionParamCheck newAction = (ActionParamCheck) CreateInstance <ActionParamCheck>();
			newAction.checkOwn = false;
			newAction.actionListSource = ActionListSource.AssetFile;
			newAction.actionListAsset = actionListAsset;
			newAction.parameterID = parameterID;

			newAction.compareUnityObject = checkValue;

			return newAction;
		}
		
	}
	
}