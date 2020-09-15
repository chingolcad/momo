using UnityEngine;
using UnityEditor;

namespace AC
{

	[CustomEditor(typeof(GameCameraThirdPerson))]
	public class GameCameraThirdPersonEditor : Editor
	{
		
		public override void OnInspectorGUI ()
		{
			GameCameraThirdPerson _target = (GameCameraThirdPerson) target;

			// Target
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Target", EditorStyles.boldLabel);
			_target.targetIsPlayer = CustomGUILayout.Toggle ("Is player?", _target.targetIsPlayer, "", "If True, the camera will follow the active Player");
			if (!_target.targetIsPlayer)
			{
				_target.target = (Transform) CustomGUILayout.ObjectField <Transform> ("Target transform:", _target.target, true, "", "The object that the camera should follow");
			}
			_target.horizontalOffset = CustomGUILayout.FloatField ("Horizontal offset:", _target.horizontalOffset, "", "The horizontal position offset");
			_target.verticalOffset = CustomGUILayout.FloatField ("Vertical offset:", _target.verticalOffset, "", "The vertical position offset");
			EditorGUILayout.EndVertical ();

			// Distance
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Distance", EditorStyles.boldLabel);
			_target.distance = CustomGUILayout.FloatField ("Distance from target:", _target.distance, "", "The normal distance to keep from its target");
			_target.allowMouseWheelZooming = CustomGUILayout.Toggle ("Mousewheel zooming?", _target.allowMouseWheelZooming, "", "If True, the mousewheel can be used to zoom the camera's distance from the target");
			_target.detectCollisions = CustomGUILayout.Toggle ("Detect wall collisions?", _target.detectCollisions, "", "If True, then the camera will detect Colliders to try to avoid clipping through walls");

			if (_target.detectCollisions)
			{
				_target.collisionRadius = CustomGUILayout.FloatField ("Collision radius:", _target.collisionRadius, "", "The size of the SphereCast to use when detecting collisions");
				if (_target.collisionRadius < 0f)
				{
					_target.collisionRadius = 0f;
				}
				_target.collisionLayerMask = AdvGame.LayerMaskField ("Collision layer(s):", _target.collisionLayerMask, "The layers to check for when detecting collisions");
			}
			if (_target.allowMouseWheelZooming || _target.detectCollisions)
			{
				_target.minDistance = CustomGUILayout.FloatField ("Mininum distance:", _target.minDistance, "", "The minimum distance to keep from its target");
			}
			if (_target.allowMouseWheelZooming)
			{
				_target.maxDistance = CustomGUILayout.FloatField ("Maximum distance:", _target.maxDistance, "", "The maximum distance to keep from its target");
			}
			EditorGUILayout.EndVertical ();

			// Spin
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("Spin rotation", "How spin rotation is affected"), EditorStyles.boldLabel, GUILayout.Width (130f));
			_target.spinLock = (RotationLock) EditorGUILayout.EnumPopup (_target.spinLock);
			EditorGUILayout.EndHorizontal ();
			if (_target.spinLock != RotationLock.Locked)
			{
				_target.spinSpeed = CustomGUILayout.FloatField ("Speed:", _target.spinSpeed, "", "The speed of spin rotations");
				_target.spinAccleration = CustomGUILayout.FloatField ("Acceleration:", _target.spinAccleration, "", "The acceleration of spin rotations");
				_target.spinDeceleration = CustomGUILayout.FloatField ("Deceleration:", _target.spinDeceleration, "", "The deceleration of spin rotations");
				_target.isDragControlled = CustomGUILayout.Toggle ("Drag-controlled?", _target.isDragControlled, "", "If True, then the camera can be drag-controlled");
				_target.canRotateDuringCutscenes = CustomGUILayout.ToggleLeft ("Can rotate during cutscenes?", _target.canRotateDuringCutscenes, "", "If True, then spin and pitch can be altered when gameplay is blocked");
				_target.canRotateDuringConversations = CustomGUILayout.ToggleLeft ("Can rotate during Conversations?", _target.canRotateDuringConversations, "", "If True, then spin and pitch can be altered when a Conversation is active");
				if (!_target.isDragControlled)
				{
					_target.spinAxis = CustomGUILayout.TextField ("Input axis:", _target.spinAxis, "", "The name of the Input axis that controls spin rotation");
				}
				_target.inputAffectsSpeed = CustomGUILayout.ToggleLeft ("Scale speed with input magnitude?", _target.inputAffectsSpeed, "", "If True, then the magnitude of the input vector affects the magnitude of the rotation speed");
				_target.invertSpin = CustomGUILayout.Toggle ("Invert?", _target.invertSpin, "", "If True, then the direction of spin rotations will be reversed");
				_target.toggleCursor = CustomGUILayout.Toggle ("Cursor must be locked?", _target.toggleCursor, "", "If True, then the cursor must be locked for spin rotation to occur");
				_target.resetSpinWhenSwitch = CustomGUILayout.Toggle ("Reset angle on switch?", _target.resetSpinWhenSwitch, "", "If True, then the spin rotation will be reset when the camera is made active");

				if (_target.spinLock == RotationLock.Limited)
				{
					_target.maxSpin = CustomGUILayout.FloatField ("Maximum angle:", _target.maxSpin, "", "The maximum spin angle");
				}
			}

			if (_target.spinLock != RotationLock.Free)
			{
				_target.alwaysBehind = CustomGUILayout.Toggle ("Always behind target?", _target.alwaysBehind, "", "If True, then the camera's spin rotation will be relative to the target's rotation");
				if (_target.alwaysBehind)
				{
					_target.spinAccleration = CustomGUILayout.FloatField ("Acceleration:", _target.spinAccleration, "", "The acceleration of spin rotations");
					_target.spinOffset = CustomGUILayout.FloatField ("Offset angle:", _target.spinOffset, "", "The offset in spin (yaw) angle");
				}
			}
			EditorGUILayout.EndVertical ();

			// Pitch
			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("Pitch rotation", "How pitch rotation is affected"), EditorStyles.boldLabel, GUILayout.Width (130f));
			_target.pitchLock = (RotationLock) EditorGUILayout.EnumPopup (_target.pitchLock);
			EditorGUILayout.EndHorizontal ();
			if (_target.pitchLock != RotationLock.Locked)
			{
				_target.pitchSpeed = CustomGUILayout.FloatField ("Speed:", _target.pitchSpeed, "", "The speed of pitch rotations");
				_target.pitchAccleration = CustomGUILayout.FloatField ("Acceleration:", _target.pitchAccleration, "", "The acceleration of pitch rotations");
				_target.pitchDeceleration = CustomGUILayout.FloatField ("Deceleration:", _target.pitchDeceleration, "", "The deceleration of pitch rotations");
				_target.isDragControlled = CustomGUILayout.Toggle ("Drag-controlled?", _target.isDragControlled, "", "If True, then the camera can be drag-controlled");
				_target.canRotateDuringConversations = CustomGUILayout.ToggleLeft ("Can rotate during Conversations?", _target.canRotateDuringConversations, "", "If True, then spin and pitch can be altered when a Conversation is active");
				if (!_target.isDragControlled)
				{
					_target.pitchAxis = CustomGUILayout.TextField ("Input axis:", _target.pitchAxis, "", "The name of the Input axis that controls pitch rotation");
				}
				_target.inputAffectsSpeed = CustomGUILayout.ToggleLeft ("Scale speed with input magnitude?", _target.inputAffectsSpeed, "", "If True, then the magnitude of the input vector affects the magnitude of the rotation speed");
				_target.invertPitch = CustomGUILayout.Toggle ("Invert?", _target.invertPitch, "", "If True, then the direction of pitch rotations will be reversed");
				_target.resetPitchWhenSwitch = CustomGUILayout.Toggle ("Reset angle on switch?", _target.resetPitchWhenSwitch, "", "If True, then the pitch rotation will be reset when the camera is made active");

				if (_target.pitchLock == RotationLock.Limited)
				{
					_target.maxPitch = CustomGUILayout.FloatField ("Maximum angle:", _target.maxPitch, "", "The maximum pitch angle");
					_target.minPitch = CustomGUILayout.FloatField ("Minimum angle:", _target.minPitch, "", "The minimum pitch angle");
				}
			}
			else
			{
				_target.maxPitch = CustomGUILayout.FloatField ("Fixed angle:", _target.maxPitch, "", "The fixed pitch angle");
			}
			EditorGUILayout.EndVertical ();

			EditorGUILayout.BeginVertical ("Button");
			EditorGUILayout.LabelField ("Depth of field", EditorStyles.boldLabel);
			_target.focalPointIsTarget = CustomGUILayout.Toggle ("Focal point is target object?", _target.focalPointIsTarget, "", "If True, then the focal distance will match the distance to the target");
			if (!_target.focalPointIsTarget)
			{
				_target.focalDistance = CustomGUILayout.FloatField ("Focal distance:", _target.focalDistance, "", "The camera's focal distance.  When the MainCamera is attached to this camera, it can be read through script with 'AC.KickStarter.mainCamera.GetFocalDistance()' and used to update your post-processing method.");
			}
			else if (Application.isPlaying)
			{
				EditorGUILayout.LabelField ("Focal distance: " +  _target.focalDistance.ToString (), EditorStyles.miniLabel);
			}
			EditorGUILayout.EndVertical ();

			DisplayInputList (_target);

			UnityVersionHandler.CustomSetDirty (_target);
		}


		private void DisplayInputList (GameCameraThirdPerson _target)
		{
			string result = "";
			
			if (_target.allowMouseWheelZooming)
			{
				result += "\n";
				result += "- Mouse ScrollWheel";
			}
			if (!_target.isDragControlled)
			{
				if (_target.spinLock != RotationLock.Locked)
				{
					result += "\n";
					result += "- " + _target.spinAxis;
				}
				if (_target.pitchLock != RotationLock.Locked)
				{
					result += "\n";
					result += "- " + _target.pitchAxis;
				}
			}
			if (_target.toggleCursor)
			{
				result += "\n";
				result += "- ToggleCursor";
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