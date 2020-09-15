using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(Moveable_PickUp))]
	public class Moveable_PickUpEditor : DragBaseEditor
	{

		public override void OnInspectorGUI ()
		{
			Moveable_PickUp _target = (Moveable_PickUp) target;
			GetReferences ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Movment settings:", EditorStyles.boldLabel);
			_target.maxSpeed = CustomGUILayout.FloatField ("Max speed:", _target.maxSpeed, "", "The maximum force magnitude that can be applied to itself");
			_target.playerMovementReductionFactor = CustomGUILayout.Slider ("Player movement reduction:", _target.playerMovementReductionFactor, 0f, 1f, "", "How much player movement is reduced by when the object is being dragged");
			_target.invertInput = CustomGUILayout.Toggle ("Invert input?", _target.invertInput, "", "If True, input vectors will be inverted");
			_target.breakForce = CustomGUILayout.FloatField ("Break force:", _target.breakForce, "", "The maximum force magnitude that can be applied by the player - if exceeded, control will be removed");
			_target.initialLift = CustomGUILayout.Slider ("Initial lift:", _target.initialLift, 0f, 1f, "", "The lift to give objects picked up, so that they aren't touching the ground when initially held");
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Interactions", EditorStyles.boldLabel);

			_target.actionListSource = (ActionListSource) CustomGUILayout.EnumPopup ("Actions source:", _target.actionListSource, "", "The source of the commands that are run when the object is interacted with");

			if (_target.actionListSource == ActionListSource.InScene)
			{
				EditorGUILayout.BeginHorizontal ();
				_target.interactionOnGrab = (Interaction) CustomGUILayout.ObjectField <Interaction> ("Interaction on move:", _target.interactionOnGrab, true, "", "The Interaction to run whenever the object is moved by the player");
				if (_target.interactionOnGrab == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						Undo.RecordObject (_target, "Create Interaction");
						Interaction newInteraction = SceneManager.AddPrefab ("Logic", "Interaction", true, false, true).GetComponent <Interaction>();
						newInteraction.gameObject.name = AdvGame.UniqueName ("Grab : " + _target.gameObject.name);
						_target.interactionOnGrab = newInteraction;
					}
				}
				EditorGUILayout.EndHorizontal ();

				if (_target.interactionOnGrab != null && _target.interactionOnGrab.source == ActionListSource.InScene && _target.interactionOnGrab.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.moveParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.interactionOnGrab.parameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
					EditorGUILayout.EndHorizontal ();
				}
				else if (_target.interactionOnGrab != null && _target.interactionOnGrab.source == ActionListSource.AssetFile && _target.interactionOnGrab.assetFile != null && _target.interactionOnGrab.assetFile.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.moveParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.interactionOnGrab.assetFile.DefaultParameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
					EditorGUILayout.EndHorizontal ();
				}

				EditorGUILayout.BeginHorizontal ();
				_target.interactionOnDrop = (Interaction) CustomGUILayout.ObjectField <Interaction> ("Interaction on let go:", _target.interactionOnDrop, true, "", "The Interaction to run whenever the object is let go by the player");
				if (_target.interactionOnDrop == null)
				{
					if (GUILayout.Button ("Create", GUILayout.MaxWidth (60f)))
					{
						Undo.RecordObject (_target, "Create Interaction");
						Interaction newInteraction = SceneManager.AddPrefab ("Logic", "Interaction", true, false, true).GetComponent <Interaction>();
						newInteraction.gameObject.name = AdvGame.UniqueName ("LetGo : " + _target.gameObject.name);
						_target.interactionOnDrop = newInteraction;
					}
				}
				EditorGUILayout.EndHorizontal ();

				if (_target.interactionOnDrop != null)
				{
					if (_target.interactionOnDrop.source == ActionListSource.InScene && _target.interactionOnDrop.NumParameters > 0)
					{
						EditorGUILayout.BeginHorizontal ();
						_target.dropParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.interactionOnDrop.parameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
						EditorGUILayout.EndHorizontal ();
					}
					else if (_target.interactionOnDrop.source == ActionListSource.AssetFile && _target.interactionOnDrop.assetFile != null && _target.interactionOnDrop.assetFile.NumParameters > 0)
					{
						EditorGUILayout.BeginHorizontal ();
						_target.dropParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.interactionOnDrop.assetFile.DefaultParameters, _target.dropParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
						EditorGUILayout.EndHorizontal ();
					}
				}
			}
			else if (_target.actionListSource == ActionListSource.AssetFile)
			{
				_target.actionListAssetOnGrab = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("Interaction on grab:", _target.actionListAssetOnGrab, true, "", "The ActionList asset to run whenever the object is moved by the player");

				if (_target.actionListAssetOnGrab != null && _target.actionListAssetOnGrab.NumParameters> 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.moveParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.actionListAssetOnGrab.DefaultParameters, _target.moveParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
					EditorGUILayout.EndHorizontal ();
				}

				_target.actionListAssetOnDrop = (ActionListAsset) CustomGUILayout.ObjectField <ActionListAsset> ("Interaction on let go:", _target.actionListAssetOnDrop, true, "", "The ActionList asset to run whenever the object is let go by the player");

				if (_target.actionListAssetOnDrop != null && _target.actionListAssetOnDrop.NumParameters > 0)
				{
					EditorGUILayout.BeginHorizontal ();
					_target.dropParameterID = Action.ChooseParameterGUI ("PickUp parameter:", _target.actionListAssetOnDrop.DefaultParameters, _target.dropParameterID, ParameterType.GameObject, -1, "The GameObject parameter to automatically assign as this pickup object");
					EditorGUILayout.EndHorizontal ();
				}
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Rotation settings:", EditorStyles.boldLabel);
			_target.allowRotation = CustomGUILayout.Toggle ("Allow rotation?", _target.allowRotation, "", "If True, the object can be rotated");
			if (_target.allowRotation)
			{
				_target.rotationFactor = CustomGUILayout.FloatField ("Rotation factor:", _target.rotationFactor, "", "The speed by which the object can be rotated");
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Zoom settings:", EditorStyles.boldLabel);
			_target.allowZooming = CustomGUILayout.Toggle ("Allow zooming?", _target.allowZooming, "", "If True, the object can be moved towards and away from the camera");
			if (_target.allowZooming)
			{
				_target.zoomSpeed = CustomGUILayout.FloatField ("Zoom speed:", _target.zoomSpeed, "", "The speed at which the object can be moved towards and away from the camera");
				_target.minZoom = CustomGUILayout.FloatField ("Closest distance:", _target.minZoom, "", "The minimum distance that there can be between the object and the camera");
				_target.maxZoom = CustomGUILayout.FloatField ("Farthest distance:", _target.maxZoom, "", "The maximum distance that there can be between the object and the camera");
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Throw settings:", EditorStyles.boldLabel);
			_target.allowThrow = CustomGUILayout.Toggle ("Allow throwing?", _target.allowThrow, "", "If True, the object can be thrown");
			if (_target.allowThrow)
			{
				_target.throwForce = CustomGUILayout.FloatField ("Force scale:", _target.throwForce, "", "How far the object can be thrown");
				_target.chargeTime = CustomGUILayout.FloatField ("Charge time:", _target.chargeTime, "", "How long a 'charge' takes, if the object cen be thrown");
				_target.pullbackDistance = CustomGUILayout.FloatField ("Pull-back distance:", _target.pullbackDistance, "", "How far the object is pulled back while chargine, if the object can be thrown");
			}		
			EditorGUILayout.EndVertical ();

			SharedGUI (_target, false);

			DisplayInputList (_target);
		
			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void DisplayInputList (Moveable_PickUp _target)
		{
			string result = "";

			if (_target.allowRotation)
			{
				result += "\n";
				result += "- RotateMoveable (Button)";
				result += "\n";
				result += "- RotateMoveableToggle (Button";
			}
			if (_target.allowZooming)
			{
				result += "\n";
				result += "- ZoomMoveable (Axis)";
			}
			if (_target.allowThrow)
			{
				result += "\n";
				result += "- ThrowMoveable (Button)";
			}

			if (result != "")
			{
				EditorGUILayout.Space ();
				EditorGUILayout.LabelField ("Required inputs:", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox ("The following input axes are available for the chosen settings:" + result, MessageType.Info);
			}
		}

	}

}