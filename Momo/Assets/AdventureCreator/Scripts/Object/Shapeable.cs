/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Shapeable.cs"
 * 
 *	Attaching this script to an object with BlendShapes will allow
 *	them to be animated via the Actions Object: Animate and Character: Animate
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script can sort blendshapes on a SkinnedMeshRenderer into groups, and provides functions to easily interpolate their values - affecting all blendshapes within a group.
	 * If LipSyncing is set to affect GameObjects, then this componentt is necessary to animate phoneme shapes.
	 */
	[AddComponentMenu("Adventure Creator/Misc/Shapeable")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_shapeable.html")]
	public class Shapeable : MonoBehaviour
	{

		#region Variables

		/** A List of user-defined ShapeGroup instances, that define how the blendshapes are sorted */
		public List<ShapeGroup> shapeGroups = new List<ShapeGroup>();

		protected SkinnedMeshRenderer skinnedMeshRenderer;
		
		// OLD
		private bool isChanging = false;
		private float targetShape;
		private float actualShape;
		private float originalShape;
		private int shapeKey;
		private float startTime;
		private float deltaTime;

		#endregion


		#region UnityStandards		
		
		protected void Awake ()
		{
			if (SkinnedMeshRenderer != null)
			{
				// Set all values to zero
				foreach (ShapeGroup shapeGroup in shapeGroups)
				{
					shapeGroup.SetSMR (SkinnedMeshRenderer);
					
					foreach (ShapeKey shapeKey in shapeGroup.shapeKeys)
					{
						shapeKey.SetValue (0f, SkinnedMeshRenderer);
					}
				}
			}
		}


		protected void LateUpdate ()
		{
			foreach (ShapeGroup shapeGroup in shapeGroups)
			{
				shapeGroup.UpdateKeys ();
			}
			
			// OLD
			if (isChanging)
			{
				actualShape = Mathf.Lerp (originalShape, targetShape, AdvGame.Interpolate (startTime, deltaTime, AC.MoveMethod.Linear, null));
				
				if (Time.time > startTime + deltaTime)
				{
					isChanging = false;
					actualShape = targetShape;
				}
				
				if (SkinnedMeshRenderer != null)
				{
					SkinnedMeshRenderer.SetBlendShapeWeight (shapeKey, actualShape);
				}
			}
		}

		#endregion


		#region PublicFunctions		

		/**
		 * <summary>Disables all blendshapes within a ShapeGroup.</summary>
		 * <param name = "_groupID">The unique identifier of the ShapeGroup to affect</param>
		 * <param name = "_deltaTime">The duration, in seconds, that the group's blendshapes should be disabled</param>
		 * <param name = "_moveMethod">The interpolation method by which the blendshapes are affected (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_timeCurve">If _moveMethod = MoveMethod.CustomCurve, then the transition speed will be follow the shape of the supplied AnimationCurve. This curve can exceed "1" in the Y-scale, allowing for overshoot effects.</param>
		 */
		public void DisableAllKeys (int _groupID, float _deltaTime, MoveMethod _moveMethod, AnimationCurve _timeCurve)
		{
			foreach (ShapeGroup shapeGroup in shapeGroups)
			{
				if (shapeGroup.ID == _groupID)
				{
					shapeGroup.SetActive (-1, 0f, _deltaTime, _moveMethod, _timeCurve);
				}
			}
		}
		

		/**
		 * <summary>Sets a blendshape within a ShapeGroup as the "active" one, causing all others to be disabled.</summary>
		 * <param name = "_groupID">The unique identifier of the ShapeGroup to affect</param>
		 * <param name = "_keyID">The unique identifier of the blendshape to affect</param?
		 * <param name = "_value">The value to set the active blendshape</param>
		 * <param name = "_deltaTime">The duration, in seconds, that the group's blendshapes should be affected</param>
		 * <param name = "_moveMethod">The interpolation method by which the blendshapes are affected (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_timeCurve">If _moveMethod = MoveMethod.CustomCurve, then the transition speed will be follow the shape of the supplied AnimationCurve. This curve can exceed "1" in the Y-scale, allowing for overshoot effects.</param>
		 */
		public void SetActiveKey (int _groupID, int _keyID, float _value, float _deltaTime, MoveMethod _moveMethod, AnimationCurve _timeCurve)
		{
			foreach (ShapeGroup shapeGroup in shapeGroups)
			{
				if (shapeGroup.ID == _groupID)
				{
					shapeGroup.SetActive (_keyID, _value, _deltaTime, _moveMethod, _timeCurve);
				}
			}
		}


		/**
		 * <summary>Sets a blendshape within a ShapeGroup as the "active" one, causing all others to be disabled.</summary>
		 * <param name = "_groupID">The unique identifier of the ShapeGroup to affect</param>
		 * <param name = "_keyLabel">The name of the blendshape to affect</param?
		 * <param name = "_value">The value to set the active blendshape</param>
		 * <param name = "_deltaTime">The duration, in seconds, that the group's blendshapes should be affected</param>
		 * <param name = "_moveMethod">The interpolation method by which the blendshapes are affected (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_timeCurve">If _moveMethod = MoveMethod.CustomCurve, then the transition speed will be follow the shape of the supplied AnimationCurve. This curve can exceed "1" in the Y-scale, allowing for overshoot effects.</param>
		 */
		public void SetActiveKey (int _groupID, string _keyLabel, float _value, float _deltaTime, MoveMethod _moveMethod, AnimationCurve _timeCurve)
		{
			foreach (ShapeGroup shapeGroup in shapeGroups)
			{
				if (shapeGroup.ID == _groupID)
				{
					shapeGroup.SetActive (_keyLabel, _value, _deltaTime, _moveMethod, _timeCurve);
				}
			}
		}


		/**
		 * <summary>Gets the ShapeGroup associated with an ID number.</summary>
		 * <param name = "ID">A unique identifier for the ShapeGroup</param>
		 * <returns>The ShapeGroup associated with the ID number</returns>
		 */
		public ShapeGroup GetGroup (int ID)
		{
			foreach (ShapeGroup shapeGroup in shapeGroups)
			{
				if (shapeGroup.ID == ID)
				{
					return shapeGroup;
				}
			}
			return null;
		}


		/**
		 * <summary>Sets the value of a specific blendshape on the SkinnedMeshRenderer.</summary>
		 * <param name = "_shapeKey">The index number of the blendshape to affect</param>
		 * <param name = "_targetShape">The target intensity of the blendshape</param>
		 * <param name = "_deltaTime">The duration of the transition effect</param>
		 */
		public void Change (int _shapeKey, float _targetShape, float _deltaTime)
		{
			if (targetShape < 0f)
			{
				targetShape = 0f;
			}
			else if (targetShape > 100f)
			{
				targetShape = 100f;
			}
			
			isChanging = true;
			targetShape = _targetShape;
			deltaTime = _deltaTime;
			startTime = Time.time;
			shapeKey = _shapeKey;
			
			if (SkinnedMeshRenderer != null)
			{
				originalShape = SkinnedMeshRenderer.GetBlendShapeWeight (shapeKey);
			}
		}

		#endregion


		#region GetSet

		/** The SkinnedMeshRenderer that this component controls */
		protected SkinnedMeshRenderer SkinnedMeshRenderer
		{
			get
			{
				if (skinnedMeshRenderer == null)
				{
					skinnedMeshRenderer = GetComponent <SkinnedMeshRenderer> ();
					if (skinnedMeshRenderer == null)
					{
						skinnedMeshRenderer = GetComponentInChildren <SkinnedMeshRenderer>();
					}
					if (skinnedMeshRenderer == null)
					{
						ACDebug.LogWarning ("No Skinned Mesh Renderer found on Shapeable GameObject!", this);
					}
				}
				return skinnedMeshRenderer;
			}
		}

		#endregion

	}
	

	/**
	 * A data container for a group of blendshapes on a SkinnedMeshRenderer. By grouping blendshapes, we can make one "active" and have all others disable
	 */
	[System.Serializable]
	public class ShapeGroup
	{

		/** The editor-friendly name of the group */
		public string label = "";
		/** A unique identifier */
		public int ID = 0;
		/** A list of ShapeKey instances - each ShapeKey representing a blendshape */
		public List<ShapeKey> shapeKeys = new List<ShapeKey>();
		
		protected ShapeKey activeKey = null;
		protected SkinnedMeshRenderer smr;
		protected float startTime;
		protected float changeTime;
		protected AnimationCurve timeCurve;
		protected MoveMethod moveMethod;
		

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "idArray">An array of existing ShapeGroup ID numbers, to ensure that the groups's identifier is unique</param>
		 */
		public ShapeGroup (int[] idArray)
		{
			// Update id based on array
			ID = 0;
			foreach (int _id in idArray)
			{
				if (ID == _id)
				{
					ID ++;
				}
			}
		}
		

		/**
		 * <summary>Assigns the SkinnedMeshRenderer that this group is assocated with.</summary>
		 * <param name = "_smr">The SkinnedMeshRenderer that this group is associated with.</param>
		 */
		public void SetSMR (SkinnedMeshRenderer _smr)
		{
			smr = _smr;
		}
		

		/**
		 * <summary>Gets the ID number of the active blendshape.</summary>
		 * <returns>The ID number of the active blendshape.</returns>
		 */
		public int GetActiveKeyID ()
		{
			if (activeKey != null && shapeKeys.Contains (activeKey))
			{
				return activeKey.ID;
			}
			return -1;
		}
		

		/**
		 * <summary>Gets the intended value of the active blendshape.</summary>
		 * <returns>The intended value of the active blendshape</returns>
		 */
		public float GetActiveKeyValue ()
		{
			if (activeKey != null && shapeKeys.Contains (activeKey))
			{
				return activeKey.targetValue;
			}
			return 0f;
		}
		

		/**
		 * <summary>Sets a blendshape as the "active" one, causing all others to be disabled.</summary>
		 * <param name = "_ID">The unique identifier of the blendshape to affect</param>
		 * <param name = "_value">The value to set the active blendshape</param>
		 * <param name = "_changeTime">The duration, in seconds, that the group's blendshapes should be affected</param>
		 * <param name = "_moveMethod">The interpolation method by which the blendshapes are affected (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_timeCurve">If _moveMethod = MoveMethod.CustomCurve, then the transition speed will be follow the shape of the supplied AnimationCurve. This curve can exceed "1" in the Y-scale, allowing for overshoot effects.</param>
		 */
		public void SetActive (int _ID, float _value, float _changeTime, MoveMethod _moveMethod, AnimationCurve _timeCurve)
		{
			if (_changeTime < 0f)
			{
				return;
			}

			activeKey = null;
			foreach (ShapeKey shapeKey in shapeKeys)
			{
				if (shapeKey.ID == _ID)
				{
					activeKey = shapeKey;
					shapeKey.targetValue = _value;
				}
				else
				{
					shapeKey.targetValue = 0f;
				}

				shapeKey.ResetInitialValue ();
			}
			
			moveMethod = _moveMethod;
			timeCurve = _timeCurve;
			changeTime = _changeTime;
			startTime = Time.time;
		}


		/**
		 * <summary>Sets a blendshape as the "active" one, causing all others to be disabled.</summary>
		 * <param name = "_label">The name of the blendshape to affect</param>
		 * <param name = "_value">The value to set the active blendshape</param>
		 * <param name = "_changeTime">The duration, in seconds, that the group's blendshapes should be affected</param>
		 * <param name = "_moveMethod">The interpolation method by which the blendshapes are affected (Linear, Smooth, Curved, EaseIn, EaseOut, CustomCurve)</param>
		 * <param name = "_timeCurve">If _moveMethod = MoveMethod.CustomCurve, then the transition speed will be follow the shape of the supplied AnimationCurve. This curve can exceed "1" in the Y-scale, allowing for overshoot effects.</param>
		 */
		public void SetActive (string _label, float _value, float _changeTime, MoveMethod _moveMethod, AnimationCurve _timeCurve)
		{
			if (_changeTime < 0f)
			{
				return;
			}

			activeKey = null;
			foreach (ShapeKey shapeKey in shapeKeys)
			{
				if (shapeKey.label == _label)
				{
					activeKey = shapeKey;
					shapeKey.targetValue = _value;
				}
				else
				{
					shapeKey.targetValue = 0f;
				}

				shapeKey.ResetInitialValue ();
			}
			
			moveMethod = _moveMethod;
			timeCurve = _timeCurve;
			changeTime = _changeTime;
			startTime = Time.time;
		}
		

		/**
		 * Updates the values of all blendshapes within the group.
		 */
		public void UpdateKeys ()
		{
			if (smr == null)
			{
				return;
			}
			
			foreach (ShapeKey shapeKey in shapeKeys)
			{
				if (changeTime > 0f)
				{
					float newValue = Mathf.Lerp (shapeKey.InitialValue, shapeKey.targetValue, AdvGame.Interpolate (startTime, changeTime, moveMethod, timeCurve));
					shapeKey.SetValue (newValue, smr);
					if ((startTime + changeTime) < Time.time)
					{
						changeTime = 0f;
					}
				}
				else
				{
					shapeKey.SetValue (shapeKey.targetValue, smr);
				}
			}
		}
		
	}
	

	/**
	 * A data container for a blendshape, stored within a ShapeGroup.
	 */
	[System.Serializable]
	public class ShapeKey
	{

		/** The index number of the SkinnedMeshRenderer's blendshapes that this is linked to */
		public int index = 0;
		/** An editor-friendly name of the blendshape */
		public string label = "";
		/** A unique identifier */
		public int ID = 0;
		/** The current value of the blendshape */
		public float value = 0;
		/** The intended value of the blendshape */
		public float targetValue = 0;

		private float initialValue;


		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "idArray">An array of existing ShapeKey ID numbers, to ensure that the key's identifier is unique</param>
		 */
		public ShapeKey (int[] idArray)
		{
			// Update id based on array
			ID = 0;
			foreach (int _id in idArray)
			{
				if (ID == _id)
				{
					ID ++;
				}
			}
		}
		

		/**
		 * <summary>Sets the value of the associated blendshape</summary>
		 * <param name = "_value">The value that the blendshape should have</param>
		 * <param name = "smr">The SkinnedMeshRenderer component that the blendshape is a part of</param>
		 */
		public void SetValue (float _value, SkinnedMeshRenderer smr)
		{
			value = _value;
			smr.SetBlendShapeWeight (index, value);
		}


		public float InitialValue
		{
			get
			{
				return initialValue;
			}
		}


		public void ResetInitialValue ()
		{
			initialValue = value;
		}
		
	}
	
}