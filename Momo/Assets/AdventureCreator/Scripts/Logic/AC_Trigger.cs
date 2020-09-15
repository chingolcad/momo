/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"AC_Trigger.cs"
 * 
 *	This ActionList runs when the Player enters it.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * An ActionList that is run when the Player, or another object, comes into contact with it.
	 */
	[AddComponentMenu("Adventure Creator/Logic/Trigger")]
	[System.Serializable]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_a_c___trigger.html")]
	public class AC_Trigger : ActionList
	{

		#region Variables

		/** If detectionMethod = TriggerDetectionMethod.RigidbodyCollision, what the Trigger will react to (Player, SetObject, AnyObject, AnyObjectWithComponent) */
		public TriggerDetects detects = TriggerDetects.Player;
		/** The GameObject that the Trigger reacts to, if detectionMethod = TriggerDetectionMethod.RigidbodyCollision and detects = TriggerDetects.SetObject */
		public GameObject obToDetect;
		/** The component that must be attached to an object for the Trigger to react to, if detectionMethod = TriggerDetectionMethod.RigidbodyCollision and detects = TriggerDetects.AnyObjectWithComponent */
		public string detectComponent = "";

		/** What kind of contact the Trigger reacts to (0 = "On enter", 1 = "Continuous", 2 = "On exit") */
		public int triggerType;
		/** If True, and the Player sets off the Trigger while walking towards a Hotspot Interaction, then the Player will stop, and the Interaction will be cancelled */
		public bool cancelInteractions = false;
		/** The state of the game under which the trigger reacts (OnlyDuringGameplay, OnlyDuringCutscenes, DuringCutscenesAndGameplay) */
		public TriggerReacts triggerReacts = TriggerReacts.OnlyDuringGameplay;
		/** The way in which objects are detected (RigidbodyCollision, TransformPosition) */
		public TriggerDetectionMethod detectionMethod = TriggerDetectionMethod.RigidbodyCollision;

		/** If True, and detectionMethod = TriggerDetectionMethod.TransformPosition, then the Trigger will react to the active Player */
		public bool detectsPlayer = true;
		/** The GameObjects that the Trigger reacts to, if detectionMethod = TriggerDetectionMethod.TransformPosition */
		public List<GameObject> obsToDetect = new List<GameObject>();

		public int gameObjectParameterID = -1;

		protected Collider2D _collider2D;
		protected Collider _collider;
		protected bool[] lastFrameWithins;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);

			_collider2D = GetComponent <Collider2D>();
			_collider = GetComponent <Collider>();
			lastFrameWithins = (detectsPlayer) ? new bool[obsToDetect.Count + 1] : new bool[obsToDetect.Count];

			if (_collider == null && _collider2D == null)
			{
				ACDebug.LogWarning ("Trigger '" + gameObject.name + " cannot detect collisions because it has no Collider!", this);
			}
		}


		protected void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}


		public void _Update ()
		{
			if (detectionMethod == TriggerDetectionMethod.TransformPosition)
			{
				for (int i=0; i<obsToDetect.Count; i++)
				{
					ProcessObject (obsToDetect[i], i);
				}

				if (detectsPlayer && KickStarter.player != null)
				{
					ProcessObject (KickStarter.player.gameObject, lastFrameWithins.Length - 1);
				}
			}
		}


		protected void OnTriggerEnter (Collider other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 0 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerEnter2D (Collider2D other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 0 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerStay (Collider other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 1 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerStay2D (Collider2D other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 1 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerExit (Collider other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 2 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}
		
		
		protected void OnTriggerExit2D (Collider2D other)
		{
			if (detectionMethod == TriggerDetectionMethod.RigidbodyCollision && triggerType == 2 && IsObjectCorrect (other.gameObject))
			{
				Interact (other.gameObject);
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * <summary>Checks if the Trigger is enabled.</summary>
		 * <returns>True if the Trigger is enabled.</summary>
		 */
		public bool IsOn ()
		{
			if (GetComponent <Collider>())
			{
				return GetComponent <Collider>().enabled;
			}
			else if (GetComponent <Collider2D>())
			{
				return GetComponent <Collider2D>().enabled;
			}
			return false;
		}
		

		/**
		 * <summary>Enables the Trigger.</summary>
		 */
		public void TurnOn ()
		{
			if (GetComponent <Collider>())
			{
				GetComponent <Collider>().enabled = true;
			}
			else if (GetComponent <Collider2D>())
			{
				GetComponent <Collider2D>().enabled = true;
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn " + this.name + " on because it has no Collider component.", this);
			}
		}
		

		/**
		 * <summary>Disables the Trigger.</summary>
		 */
		public void TurnOff ()
		{
			if (GetComponent <Collider>())
			{
				GetComponent <Collider>().enabled = false;
			}
			else if (GetComponent <Collider2D>())
			{
				GetComponent <Collider2D>().enabled = false;
			}
			else
			{
				ACDebug.LogWarning ("Cannot turn " + this.name + " off because it has no Collider component.", this);
			}

			if (lastFrameWithins != null)
			{
				for (int i=0; i<lastFrameWithins.Length; i++)
				{
					lastFrameWithins[i] = false;
				}
			}
		}
		
		#endregion


		#region ProtectedFunctions

		protected void Interact (GameObject collisionOb)
		{
			if (cancelInteractions)
			{
				KickStarter.playerInteraction.StopMovingToHotspot ();
			}
			
			if (actionListType == ActionListType.PauseGameplay)
			{
				KickStarter.playerInteraction.DeselectHotspot (false);
			}

			KickStarter.eventManager.Call_OnRunTrigger (this, collisionOb);

			// Set correct parameter
			if (collisionOb != null)
			{
				if (source == ActionListSource.InScene)
				{
					if (useParameters && parameters != null && parameters.Count >= 1)
					{
						if (parameters[0].parameterType == ParameterType.GameObject)
						{
							parameters[0].gameObject = collisionOb;
						}
						else
						{
							ACDebug.Log ("Cannot set the value of parameter 0 ('" + parameters[0].label + "') as it is not of the type 'Game Object'.", this);
						}
					}
				}
				else if (source == ActionListSource.AssetFile
						&& assetFile != null
						&& assetFile.NumParameters > 0
						&& gameObjectParameterID >= 0)
				{
					ActionParameter param = null;
					if (syncParamValues)
					{
						param = assetFile.GetParameter (gameObjectParameterID);
					}
					else
					{
						param = GetParameter (gameObjectParameterID);
					}

					if (param != null) param.SetValue (collisionOb);
				}
			}

			base.Interact ();
		}


		public override void Interact ()
		{
			Interact (null);
		}
		

		protected bool IsObjectCorrect (GameObject obToCheck)
		{
			if (KickStarter.stateHandler == null || KickStarter.stateHandler.gameState == GameState.Paused || obToCheck == null)
			{
				return false;
			}

			if (KickStarter.saveSystem.loadingGame != LoadingGame.No)
			{
				return false;
			}

			if (triggerReacts == TriggerReacts.OnlyDuringGameplay && KickStarter.stateHandler.gameState != GameState.Normal)
			{
				return false;
			}
			else if (triggerReacts == TriggerReacts.OnlyDuringCutscenes && KickStarter.stateHandler.IsInGameplay ())
			{
				return false;
			}

			if (KickStarter.stateHandler != null && KickStarter.stateHandler.AreTriggersDisabled ())
			{
				return false;
			}

			if (detectionMethod == TriggerDetectionMethod.TransformPosition)
			{
				return true;
			}

			if (detects == TriggerDetects.Player)
			{
				if (KickStarter.player != null && obToCheck == KickStarter.player.gameObject)
				{
					return true;
				}
			}
			else if (detects == TriggerDetects.SetObject)
			{
				if (obToDetect != null && obToCheck == obToDetect)
				{
					return true;
				}
			}
			else if (detects == TriggerDetects.AnyObjectWithComponent)
			{
				if (!string.IsNullOrEmpty (detectComponent))
				{
					string[] allComponents = detectComponent.Split (";"[0]);
					foreach (string component in allComponents)
					{
						if (!string.IsNullOrEmpty (component) && obToCheck.GetComponent (component))
						{
							return true;
						}
					}
				}
			}
			else if (detects == TriggerDetects.AnyObjectWithTag)
			{
				if (!string.IsNullOrEmpty (detectComponent))
				{
					string[] allComponents = detectComponent.Split (";"[0]);
					foreach (string component in allComponents)
					{
						if (!string.IsNullOrEmpty (component) && obToCheck.tag == component)
						{
							return true;
						}
					}
				}
			}
			else if (detects == TriggerDetects.AnyObject)
			{
				return true;
			}
			
			return false;
		}


		protected void ProcessObject (GameObject objectToCheck, int i)
		{
			if (objectToCheck != null)
			{
				bool isInside = CheckForPoint (objectToCheck.transform.position);
				if (DetermineValidity (isInside, i))
				{
					if (IsObjectCorrect (objectToCheck))
					{
						Interact (objectToCheck);
					}
				}
			}
		}


		protected bool DetermineValidity (bool thisFrameWithin, int i)
		{
			bool isValid = false;

			switch (triggerType)
			{
				case 0:
					// OnEnter
					if (thisFrameWithin && !lastFrameWithins[i])
					{
						isValid = true;
					}
					break;

				case 1:
					// Continuous
					isValid = thisFrameWithin;
					break;

				case 2:
					// OnExit
					if (!thisFrameWithin && lastFrameWithins[i])
					{
						isValid = true;
					}
					break;

				default:
					break;
			}

			lastFrameWithins[i] = thisFrameWithin;
			return isValid;
		}


		protected bool CheckForPoint (Vector3 position)
		{
			if (_collider2D != null)
			{
				if (_collider2D.enabled)
				{
					return _collider2D.OverlapPoint (position);
				}
				return false;
			}

			if (_collider != null && _collider.enabled)
			{
				return _collider.bounds.Contains (position);
			}

			return false;
		}

		#endregion


		#if UNITY_EDITOR
		
		protected void OnDrawGizmos ()
		{
			if (showInEditor)
			{
				DrawGizmos ();
			}
		}
		
		
		protected void OnDrawGizmosSelected ()
		{
			DrawGizmos ();
		}
		
		
		protected void DrawGizmos ()
		{
			Color gizmoColor = ACEditorPrefs.TriggerGizmoColor;

			if (GetComponent <PolygonCollider2D>())
			{
				AdvGame.DrawPolygonCollider (transform, GetComponent <PolygonCollider2D>(), gizmoColor);
			}
			else if (GetComponent <MeshCollider>())
			{
				AdvGame.DrawMeshCollider (transform, GetComponent <MeshCollider>().sharedMesh, gizmoColor);
			}
			else if (GetComponent <SphereCollider>())
			{
				AdvGame.DrawSphereCollider (transform, GetComponent <SphereCollider>(), gizmoColor);
			}
			else if (GetComponent <BoxCollider2D>() != null || GetComponent <BoxCollider>() != null)
			{
				AdvGame.DrawCubeCollider (transform, gizmoColor);
			}
		}


		protected bool showInEditor;
		/** If True, then a Gizmo will be drawn in the Scene window at the Trigger's position */
		public bool ShowInEditor
		{
			set
			{
				showInEditor = value;
			}
		}

		#endif

	}
	
}