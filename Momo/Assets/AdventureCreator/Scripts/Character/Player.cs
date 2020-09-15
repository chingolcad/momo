/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Player.cs"
 * 
 *	This is attached to the Player GameObject, which must be tagged as Player.
 * 
 */

using UnityEngine;
using System.Collections;

namespace AC
{

	/**
	 * Attaching this component to a GameObject and tagging it "Player" will make it an Adventure Creator Player.
	 */
	[AddComponentMenu("Adventure Creator/Characters/Player")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_player.html")]
	public class Player : Char
	{
		
		/** The Player's jump animation, if using Legacy animation */
		public AnimationClip jumpAnim;
		/** A unique identifier */
		public int ID;
		/** The DetectHotspots component used if SettingsManager's hotspotDetection = HotspotDetection.PlayerVicinity */
		public DetectHotspots hotspotDetector;

		/** The NPC counterpart of the Player, used as a stand-in when switching the active Player prefab */
		public NPC associatedNPCPrefab;

		protected bool lockedPath;
		protected float tankTurnFloat;
		/** True if running has been toggled */
		public bool toggleRun;

		protected bool lockHotspotHeadTurning = false;
		protected Transform fpCam;
		protected FirstPersonCamera firstPersonCamera;
		protected bool prepareToJump;

		protected SkinnedMeshRenderer[] skinnedMeshRenderers;


		protected void Awake ()
		{
			if (soundChild && soundChild.gameObject.GetComponent <AudioSource>())
			{
				audioSource = soundChild.gameObject.GetComponent <AudioSource>();
			}

			skinnedMeshRenderers = GetComponentsInChildren <SkinnedMeshRenderer>();

			if (KickStarter.playerMovement)
			{
				Transform fpCamTransform = KickStarter.playerMovement.AssignFPCamera ();
				if (fpCamTransform != null)
				{
					fpCam = KickStarter.playerMovement.AssignFPCamera ();
					if (fpCam != null)
					{
						firstPersonCamera = fpCam.GetComponent <FirstPersonCamera>();
					}
				}
			}

			_Awake ();

			if (GetAnimEngine () != null && KickStarter.settingsManager != null && KickStarter.settingsManager.hotspotDetection == HotspotDetection.PlayerVicinity && GetAnimEngine ().isSpriteBased && hotspotDetector != null)
			{
				if (spriteChild != null && hotspotDetector.transform == spriteChild) {} // OK
				else if (turn2DCharactersIn3DSpace)
				{
					if (hotspotDetector.transform == transform)
					{
						ACDebug.LogWarning ("The Player '" + name + "' has a Hotspot Detector assigned, but it is on the root.  Either parent it to the 'sprite child' instead, or check 'Turn root object in 3D space?' in the Player Inspector.", this);
					}
					else if (hotspotDetector.transform.parent == transform)
					{
						ACDebug.LogWarning ("The Player '" + name + "' has a Hotspot Detector assigned, but it is a direct child of a 2D root.  Either parent it to the 'sprite child' instead, or check 'Turn root object in 3D space?' in the Player Inspector.", this);
					}
				}
			}
		}


		/**
		 * <summary>Assigns or sets the FirstPersonCamera Transform. This is done automatically in regular First Person mode, but must be set manually
		 * if using a custom controller, eg. Ultimate FPS.</summary>
		 */
		public Transform FirstPersonCamera
		{
			get
			{
				return fpCam;
			}
			set
			{
				fpCam = value;
			}
		}


		/**
		 * Initialises the Player's animation.
		 */
		public void Initialise ()
		{
			if (GetAnimation ())
			{
				// Hack: Force idle of Legacy characters
				AdvGame.PlayAnimClip (GetAnimation (), AdvGame.GetAnimLayerInt (AnimLayer.Base), idleAnim, AnimationBlendMode.Blend, WrapMode.Loop, 0f, null, false);
			}
			else if (spriteChild)
			{
				// Hack: update 2D sprites
				InitSpriteChild ();
			}
			UpdateScale ();

			GetAnimEngine ().TurnHead (Vector2.zero);
			GetAnimEngine ().PlayIdle ();
		}


		/**
		 * The Player's "Update" function, called by StateHandler.
		 */
		public override void _Update ()
		{
			bool jumped = false;
			if (KickStarter.playerInput.InputGetButtonDown ("Jump") && KickStarter.stateHandler.IsInGameplay () && motionControl == MotionControl.Automatic && !KickStarter.stateHandler.MovementIsOff)
			{
				if (!KickStarter.playerInput.IsJumpLocked)
				{
					jumped = Jump ();
				}
			}

			if (hotspotDetector)
			{
				hotspotDetector._Update ();
			}
			
			if (activePath && !pausePath)
			{
				if (IsTurningBeforeWalking ())
				{
					if (charState == CharState.Move)
					{
						StartDecelerating ();
					}
					else if (charState == CharState.Custom)
					{
						charState = CharState.Idle;
					}
				}
				else if ((KickStarter.stateHandler.gameState == GameState.Cutscene && !lockedPath) || 
				         (KickStarter.settingsManager.movementMethod == MovementMethod.PointAndClick) ||
						 (KickStarter.settingsManager.movementMethod == MovementMethod.None) ||
				         (KickStarter.settingsManager.movementMethod == MovementMethod.StraightToCursor && KickStarter.settingsManager.singleTapStraight) || 
				         IsMovingToHotspot ())
				{
					charState = CharState.Move;
				}
			}
			else if (activePath == null && charState == CharState.Move && !KickStarter.stateHandler.IsInGameplay () && KickStarter.stateHandler.gameState != GameState.Paused)
			{
				StartDecelerating ();
			}

			if (isJumping && !jumped)
			{
				if (IsGrounded ())
				{
					isJumping = false;
				}
			}

			base._Update ();
		}

		
		/**
		 * <summary>Makes the Player spot-turn left during gameplay. This needs to be called every frame of the turn.</summary>
		 * <param name = "intensity">The relative speed of the turn. Set this to the value of the input axis for smooth movement.</param>
		 */
		public void TankTurnLeft (float intensity = 1f)
		{
			lookDirection = -(intensity * TransformRight) + ((1f - intensity) * TransformForward);
			tankTurning = true;
			turnFloat = tankTurnFloat = -intensity;
		}
		

		/**
		 * <summary>Makes the Player spot-turn right during gameplay. This needs to be called every frame of the turn.</summary>
		 * <param name = "intensity">The relative speed of the turn. Set this to the value of the input axis for smooth movement.</param>
		 */
		public void TankTurnRight (float intensity = 1f)
		{
			lookDirection = (intensity * TransformRight) + ((1f - intensity) * TransformForward);
			tankTurning = true;
			turnFloat = tankTurnFloat = intensity;
		}


		/**
		 * <summary>Stops the Player from re-calculating pathfinding calculations.</summary>
		 */
		public void CancelPathfindRecalculations ()
		{
			pathfindUpdateTime = 0f;
		}


		public override void StopTankTurning ()
		{
			lookDirection = TransformForward;
			tankTurning = false;
		}


		public override float GetTurnFloat ()
		{
			if (tankTurning)
			{
				return tankTurnFloat;
			}
			return base.GetTurnFloat ();
		}


		public void ForceTurnFloat (float _value)
		{
			turnFloat = _value;
		}


		/**
		 * <summary>Causes the Player to jump, so long as a Rigidbody component is attached.</summary>
		 * <return>True if the attempt to jump was succesful</returns>
		 */
		public bool Jump ()
		{
			if (isJumping)
			{
				return false;
			}

			if (IsGrounded () && activePath == null)
			{
				if (_rigidbody != null && !_rigidbody.isKinematic)
				{
					if (useRigidbodyForMovement)
					{	
						prepareToJump = true;
					}
					else
					{
						_rigidbody.velocity = Vector3.up * KickStarter.settingsManager.jumpSpeed;
					}
					isJumping = true;

					if (ignoreGravity)
					{
						ACDebug.LogWarning (gameObject.name + " is jumping - but 'Ignore gravity?' is enabled in the Player Inspector. Is this correct?", gameObject);
					}
					return true;
				}
				else
				{
					if (motionControl == MotionControl.Automatic)
					{
						if (_rigidbody != null && _rigidbody.isKinematic)
						{
							ACDebug.Log ("Player cannot jump without a non-kinematic Rigidbody component.", gameObject);
						}
						else
						{
							ACDebug.Log ("Player cannot jump without a Rigidbody component.", gameObject);
						}
					}
				}
			}
			else if (_collider == null)
			{
				ACDebug.Log (gameObject.name + " has no Collider component", gameObject);
			}

			return false;
		}


		public override void _FixedUpdate ()
		{
			if (prepareToJump)
			{
				prepareToJump = false;
				_rigidbody.AddForce (Vector3.up * KickStarter.settingsManager.jumpSpeed, ForceMode.Impulse);
			}

			base._FixedUpdate ();
		}
		
		
		protected bool IsMovingToHotspot ()
		{
			if (KickStarter.playerInteraction != null && KickStarter.playerInteraction.GetHotspotMovingTo () != null)
			{
				return true;
			}
			
			return false;
		}


		new public void EndPath ()
		{
			lockedPath = false;
			base.EndPath ();
		}
		

		/**
		 * <summary>Locks the Player to a Paths object during gameplay, if using Direct movement.
		 * This allows the designer to constrain the Player's movement to a Path, even though they can move freely along it.</summary>
		 * <param name = "pathOb">The Paths to lock the Player to</param>
		 */
		public void SetLockedPath (Paths pathOb)
		{
			// Ignore if using "point and click" or first person methods
			if (KickStarter.settingsManager)
			{
				if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct)
				{
					lockedPath = true;
					
					if (pathOb.pathSpeed == PathSpeed.Run)
					{
						isRunning = true;
					}
					else
					{
						isRunning = false;
					}
					
					if (pathOb.affectY)
					{
						transform.position = pathOb.transform.position;
					}
					else
					{
						transform.position = new Vector3 (pathOb.transform.position.x, transform.position.y, pathOb.transform.position.z);
					}
					
					activePath = pathOb;
					targetNode = 1;
					charState = CharState.Idle;
				}
				else
				{
					ACDebug.LogWarning ("Path-constrained player movement is only available with Direct control.", gameObject);
				}
			}
		}


		/**
		 * <summary>Checks if the Player is constrained to move along a Paths object during gameplay.</summary>
		 * <returns>True if the Player is constrained to move along a Paths object during gameplay</summary>
		 */
		public bool IsLockedToPath ()
		{
			return lockedPath;
		}

		
		/**
		 * <summary>Checks if the character can be controlled directly at this time.</summary>
		 * <returns>True if the character can be controlled directly at this time</returns>
		 */
		public override bool CanBeDirectControlled ()
		{
			if (KickStarter.stateHandler.gameState == GameState.Normal)
			{
				if (KickStarter.settingsManager.movementMethod == MovementMethod.Direct || KickStarter.settingsManager.movementMethod == MovementMethod.FirstPerson)
				{
					return KickStarter.playerInput.CanDirectControlPlayer ();
				}
			}
			return false;
		}
		
				
		protected override void Accelerate ()
		{
			/*if (AccurateDestination () && WillStopAtNextNode ())
			{
				AccurateAcc (GetTargetSpeed (), false);
			}
			else
			{
				//moveSpeed = Mathf.Lerp (moveSpeed, GetTargetSpeed (), Time.deltaTime * acceleration);
				moveSpeed = moveSpeedLerp.Update (moveSpeed, GetTargetSpeed (), acceleration, true);
			}*/

			float targetSpeed = GetTargetSpeed ();

			if (AccurateDestination () && WillStopAtNextNode ())
			{
				AccurateAcc (GetTargetSpeed (), false);
			}
			else
			{
				if (KickStarter.settingsManager.magnitudeAffectsDirect && KickStarter.settingsManager.movementMethod == MovementMethod.Direct && KickStarter.stateHandler.IsInGameplay () && !IsMovingToHotspot ())
				{
					targetSpeed -= (1f - KickStarter.playerInput.GetMoveKeys ().magnitude) / 2f;
				}

				moveSpeed = moveSpeedLerp.Update (moveSpeed, targetSpeed, acceleration);
			}
		}
		

		/**
		 * Checks if the Player's FirstPersonCamera is looking up or down.
		 */
		public bool IsTilting ()
		{
			if (firstPersonCamera != null)
			{
				return firstPersonCamera.IsTilting ();
			}
			return false;
		}


		/**
		 * Gets the angle by which the Player's FirstPersonCamera is looking up or down, with negative values looking upward.
		 */
		public float GetTilt ()
		{
			if (firstPersonCamera != null)
			{
				return firstPersonCamera.GetTilt ();;
			}
			return 0f;
		}
		

		/**
		 * <summary>Sets the tilt of a first-person camera.</summary>
		 * <param name = "lookAtPosition">The point in World Space to tilt the camera towards</param>
		 * <param name = "isInstant">If True, the camera will be rotated instantly</param>
		 */
		public void SetTilt (Vector3 lookAtPosition, bool isInstant)
		{
			if (fpCam == null)
			{
				return;
			}
			
			if (isInstant)
			{
				Vector3 lookDirection = (lookAtPosition - fpCam.position).normalized;
				float angle = Mathf.Asin (lookDirection.y) * Mathf.Rad2Deg;
				firstPersonCamera.SetPitch (-angle);
			}
			else
			{
				// Base the speed of tilt change on how much horizontal rotation is needed
				
				Quaternion oldRotation = fpCam.rotation;
				fpCam.transform.LookAt (lookAtPosition);
				float targetTilt = fpCam.localEulerAngles.x;
				fpCam.rotation = oldRotation;
				if (targetTilt > 180)
				{
					targetTilt = targetTilt - 360;
				}
				firstPersonCamera.SetPitch (targetTilt, false);
			}
		}


		/**
		 * <summary>Sets the tilt of a first-person camera.</summary>
		 * <param name = "pitchAngle">The angle to tilt the camera towards, with 0 being horizontal, positive looking downard, and negative looking upward</param>
		 * <param name = "isInstant">If True, the camera will be rotated instantly</param>
		 */
		public void SetTilt (float pitchAngle, bool isInstant)
		{
			if (firstPersonCamera == null)
			{
				return;
			}
			
			firstPersonCamera.SetPitch (pitchAngle, isInstant);
		}


		/**
		 * <summary>Controls the head-facing position.</summary>
		 * <param name = "_headTurnTarget">The Transform to face</param>
		 * <param name = "_headTurnTargetOffset">The position offset of the Transform</param>
		 * <param name = "isInstant">If True, the head will turn instantly</param>
		 * <param name = "_headFacing">What the head should face (Manual, Hotspot, None)</param>
		 */
		public override void SetHeadTurnTarget (Transform _headTurnTarget, Vector3 _headTurnTargetOffset, bool isInstant, HeadFacing _headFacing = HeadFacing.Manual)
		{
			if (_headFacing == HeadFacing.Hotspot && lockHotspotHeadTurning)
			{
				ClearHeadTurnTarget (false, HeadFacing.Hotspot);
			}
			else
			{
				base.SetHeadTurnTarget (_headTurnTarget, _headTurnTargetOffset, isInstant, _headFacing);
			}
		}


		/**
		 * <summary>Sets the enabled state of Player's ability to head-turn towards Hotspots.</summary>
		 * <param name = "state">If True, the Player's head will unable to face Hotspots</param>
		 */
		public void SetHotspotHeadTurnLock (bool state)
		{
			lockHotspotHeadTurning = state;
		}


		/**
		 * <summary>Updates a PlayerData class with its own variables that need saving.</summary>
		 * <param name = "playerData">The original PlayerData class</param>
		 * <returns>The updated PlayerData class</returns>
		 */
		public PlayerData SavePlayerData (PlayerData playerData)
		{
			playerData.playerID = ID;
			
			playerData.playerLocX = transform.position.x;
			playerData.playerLocY = transform.position.y;
			playerData.playerLocZ = transform.position.z;
			playerData.playerRotY = TransformRotation.eulerAngles.y;

			playerData.inCustomCharState = (charState == CharState.Custom && GetAnimator () != null && GetAnimator ().GetComponent <RememberAnimator>());
			
			playerData.playerWalkSpeed = walkSpeedScale;
			playerData.playerRunSpeed = runSpeedScale;

			// Animation clips
			playerData = GetAnimEngine ().SavePlayerData (playerData, this);
						
			// Sound
			playerData.playerWalkSound = AssetLoader.GetAssetInstanceID (walkSound);
			playerData.playerRunSound = AssetLoader.GetAssetInstanceID (runSound);
			
			// Portrait graphic
			playerData.playerPortraitGraphic = AssetLoader.GetAssetInstanceID (portraitIcon.texture);

			// Speech label
			playerData.playerSpeechLabel = GetName ();
			playerData.playerDisplayLineID = displayLineID;

			// Rendering
			playerData.playerLockDirection = lockDirection;
			playerData.playerLockScale = lockScale;
			if (spriteChild && spriteChild.GetComponent <FollowSortingMap>())
			{
				playerData.playerLockSorting = spriteChild.GetComponent <FollowSortingMap>().lockSorting;
			}
			else if (GetComponent <FollowSortingMap>())
			{
				playerData.playerLockSorting = GetComponent <FollowSortingMap>().lockSorting;
			}
			else
			{
				playerData.playerLockSorting = false;
			}
			playerData.playerSpriteDirection = spriteDirection;
			playerData.playerSpriteScale = spriteScale;
			if (spriteChild && spriteChild.GetComponent <Renderer>())
			{
				playerData.playerSortingOrder = spriteChild.GetComponent <Renderer>().sortingOrder;
				playerData.playerSortingLayer = spriteChild.GetComponent <Renderer>().sortingLayerName;
			}
			else if (GetComponent <Renderer>())
			{
				playerData.playerSortingOrder = GetComponent <Renderer>().sortingOrder;
				playerData.playerSortingLayer = GetComponent <Renderer>().sortingLayerName;
			}
			
			playerData.playerActivePath = 0;
			playerData.lastPlayerActivePath = 0;
			if (GetPath ())
			{
				playerData.playerTargetNode = GetTargetNode ();
				playerData.playerPrevNode = GetPreviousNode ();
				playerData.playerIsRunning = isRunning;
				playerData.playerPathAffectY = activePath.affectY;

				if (GetComponent <Paths>() && GetPath () == GetComponent <Paths>())
				{
					playerData.playerPathData = Serializer.CreatePathData (GetComponent <Paths>());
					playerData.playerLockedPath = false;
				}
				else
				{
					playerData.playerPathData = string.Empty;
					playerData.playerActivePath = Serializer.GetConstantID (GetPath ().gameObject);
					playerData.playerLockedPath = lockedPath;
				}
			}
			
			if (GetLastPath ())
			{
				playerData.lastPlayerTargetNode = GetLastTargetNode ();
				playerData.lastPlayerPrevNode = GetLastPrevNode ();
				playerData.lastPlayerActivePath = Serializer.GetConstantID (GetLastPath ().gameObject);
			}
			
			playerData.playerIgnoreGravity = ignoreGravity;
			
			// Head target
			playerData.playerLockHotspotHeadTurning = lockHotspotHeadTurning;
			if (headFacing == HeadFacing.Manual && headTurnTarget != null)
			{
				playerData.isHeadTurning = true;
				playerData.headTargetID = Serializer.GetConstantID (headTurnTarget);
				if (playerData.headTargetID == 0)
				{
					ACDebug.LogWarning ("The Player's head-turning target Transform, " + headTurnTarget + ", was not saved because it has no Constant ID", gameObject);
				}
				playerData.headTargetX = headTurnTargetOffset.x;
				playerData.headTargetY = headTurnTargetOffset.y;
				playerData.headTargetZ = headTurnTargetOffset.z;
			}
			else
			{
				playerData.isHeadTurning = false;
				playerData.headTargetID = 0;
				playerData.headTargetX = 0f;
				playerData.headTargetY = 0f;
				playerData.headTargetZ = 0f;
			}

			FollowSortingMap followSortingMap = GetComponentInChildren <FollowSortingMap>();
			if (followSortingMap != null)
			{
				playerData.followSortingMap = followSortingMap.followSortingMap;
				if (!playerData.followSortingMap && followSortingMap.GetSortingMap () != null)
				{
					if (followSortingMap.GetSortingMap ().GetComponent <ConstantID>() != null)
					{
						playerData.customSortingMapID = followSortingMap.GetSortingMap ().GetComponent <ConstantID>().constantID;
					}
					else
					{
						ACDebug.LogWarning ("The Player's SortingMap, " + followSortingMap.GetSortingMap ().name + ", was not saved because it has no Constant ID", gameObject);
						playerData.customSortingMapID = 0;
					}
				}
				else
				{
					playerData.customSortingMapID = 0;
				}
			}
			else
			{
				playerData.followSortingMap = false;
				playerData.customSortingMapID = 0;
			}

			// Remember scripts
			if (!IsLocalPlayer ())
			{
				playerData = KickStarter.levelStorage.SavePlayerData (this, playerData);
			}

			return playerData;
		}


		/** 
		 * <summary>Checks if the player is local to the scene, and not from a prefab.</summary>
		 * <returns>True if the player is local to the scene</returns>
		 */
		public bool IsLocalPlayer ()
		{
			return (ID <= -2);
		}


		/**
		 * <summary>Updates its own variables from a PlayerData class.</summary>
		 * <param name = "playerData">The PlayerData class to load from</param>
		 * <param name = "justAnimationData">If True, then only animation data (and sound) changes will be loaded, as opposed to position, rotaion, etc</param>
		 */
		public void LoadPlayerData (PlayerData playerData, bool justAnimationData = false)
		{
			if (!justAnimationData)
			{
				charState = (playerData.inCustomCharState) ? CharState.Custom : CharState.Idle;

				Teleport (new Vector3 (playerData.playerLocX, playerData.playerLocY, playerData.playerLocZ));
				SetRotation (playerData.playerRotY);
				SetMoveDirectionAsForward ();
			}

			walkSpeedScale = playerData.playerWalkSpeed;
			runSpeedScale = playerData.playerRunSpeed;
			
			// Animation clips
			GetAnimEngine ().LoadPlayerData (playerData, this);

			// Sound
			walkSound = AssetLoader.RetrieveAsset (walkSound, playerData.playerWalkSound);
			runSound = AssetLoader.RetrieveAsset (runSound, playerData.playerRunSound);

			// Portrait graphic
			portraitIcon.ReplaceTexture (AssetLoader.RetrieveAsset (portraitIcon.texture, playerData.playerPortraitGraphic));

			// Speech label
			if (!string.IsNullOrEmpty (playerData.playerSpeechLabel))
			{
				SetName (playerData.playerSpeechLabel, playerData.playerDisplayLineID);
			}
			speechLabel = playerData.playerSpeechLabel;
			
			// Rendering
			lockDirection = playerData.playerLockDirection;
			lockScale = playerData.playerLockScale;
			if (spriteChild && spriteChild.GetComponent <FollowSortingMap>())
			{
				spriteChild.GetComponent <FollowSortingMap>().lockSorting = playerData.playerLockSorting;
			}
			else if (GetComponent <FollowSortingMap>())
			{
				GetComponent <FollowSortingMap>().lockSorting = playerData.playerLockSorting;
			}
			else
			{
				ReleaseSorting ();
			}
			
			if (playerData.playerLockDirection)
			{
				spriteDirection = playerData.playerSpriteDirection;
			}
			if (playerData.playerLockScale)
			{
				spriteScale = playerData.playerSpriteScale;
			}
			if (playerData.playerLockSorting)
			{
				if (spriteChild && spriteChild.GetComponent <Renderer>())
				{
					spriteChild.GetComponent <Renderer>().sortingOrder = playerData.playerSortingOrder;
					spriteChild.GetComponent <Renderer>().sortingLayerName = playerData.playerSortingLayer;
				}
				else if (GetComponent <Renderer>())
				{
					GetComponent <Renderer>().sortingOrder = playerData.playerSortingOrder;
					GetComponent <Renderer>().sortingLayerName = playerData.playerSortingLayer;
				}
			}

			if (!justAnimationData)
			{
				// Active path
				Halt ();
				ForceIdle ();
			}

			if (!string.IsNullOrEmpty (playerData.playerPathData) && GetComponent <Paths>())
			{
				Paths savedPath = GetComponent <Paths>();
				savedPath = Serializer.RestorePathData (savedPath, playerData.playerPathData);
				SetPath (savedPath, playerData.playerTargetNode, playerData.playerPrevNode, playerData.playerPathAffectY);
				isRunning = playerData.playerIsRunning;
				lockedPath = false;
			}
			else if (playerData.playerActivePath != 0)
			{
				Paths savedPath = Serializer.returnComponent <Paths> (playerData.playerActivePath);
				if (savedPath)
				{
					lockedPath = playerData.playerLockedPath;
					
					if (lockedPath)
					{
						SetLockedPath (savedPath);
					}
					else
					{
						SetPath (savedPath, playerData.playerTargetNode, playerData.playerPrevNode);
					}
				}
				else
				{
					Halt ();
					ForceIdle ();
				}
			}
			else
			{
				Halt ();
				ForceIdle ();
			}
			
			// Previous path
			if (playerData.lastPlayerActivePath != 0)
			{
				Paths savedPath = Serializer.returnComponent <Paths> (playerData.lastPlayerActivePath);
				if (savedPath)
				{
					SetLastPath (savedPath, playerData.lastPlayerTargetNode, playerData.lastPlayerPrevNode);
				}
			}
			
			// Head target
			lockHotspotHeadTurning = playerData.playerLockHotspotHeadTurning;
			if (playerData.isHeadTurning)
			{
				ConstantID _headTargetID = Serializer.returnComponent <ConstantID> (playerData.headTargetID);
				if (_headTargetID != null)
				{
					SetHeadTurnTarget (_headTargetID.transform, new Vector3 (playerData.headTargetX, playerData.headTargetY, playerData.headTargetZ), true);
				}
				else
				{
					ClearHeadTurnTarget (true);
				}
			}
			else
			{
				ClearHeadTurnTarget (true);
			}
			
			ignoreGravity = playerData.playerIgnoreGravity;

			if (GetComponentsInChildren <FollowSortingMap>() != null)
			{
				FollowSortingMap[] followSortingMaps = GetComponentsInChildren <FollowSortingMap>();
				SortingMap customSortingMap = Serializer.returnComponent <SortingMap> (playerData.customSortingMapID);
				
				foreach (FollowSortingMap followSortingMap in followSortingMaps)
				{
					followSortingMap.followSortingMap = playerData.followSortingMap;
					if (!playerData.followSortingMap && customSortingMap != null)
					{
						followSortingMap.SetSortingMap (customSortingMap);
					}
					else
					{
						followSortingMap.SetSortingMap (KickStarter.sceneSettings.sortingMap);
					}
				}
			}

			ignoreGravity = playerData.playerIgnoreGravity;

			// Remember scripts
			if (!IsLocalPlayer ())
			{
				KickStarter.levelStorage.LoadPlayerData (this, playerData);
			}
		}


		/**
		 * Hides the player's SkinnedMeshRenderers, if any exist
		 */
		public virtual void Hide ()
		{
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
			{
				skinnedMeshRenderer.enabled = false;
			}
		}


		/**
		 * Shows the player's SkinnedMeshRenderers, if any exist
		 */
		public virtual void Show ()
		{
			foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers)
			{
				skinnedMeshRenderer.enabled = true;
			}
		}


		/**
		 * <summary>Repositions the Player at the same point as a given NPC, so that they have the same position, rotation and scale</summary>
		 * <param name = "npc">The NPC to use as reference</param>
		 */
		public void RepositionToTransform (Transform otherTransform)
		{
			Teleport (otherTransform.position);

			NPC otherNPC = otherTransform.gameObject.GetComponent <NPC>();
			Quaternion newRotation = (otherNPC != null) ? otherNPC.TransformRotation : otherTransform.rotation;
			SetRotation (newRotation);

			transform.localScale = otherTransform.localScale;
		}


		/**
		 * <summary>Gets the local scene instance of the Player's associatedNPCPrefab, if one is in the current scene.<summary>
		 * <returns>The local scene instance of the Player's associatedNPCPrefab, if one is in the current scene.<returns>
		 *
		 */
		public NPC GetRuntimeAssociatedNPC ()
		{
			if (associatedNPCPrefab != null)
			{
				ConstantID npcID = associatedNPCPrefab.GetComponent <ConstantID>();
				if (npcID != null)
				{
					NPC localAssociatedNPC = Serializer.returnComponent <NPC> (npcID.constantID);
					if (localAssociatedNPC != null && localAssociatedNPC.gameObject.activeInHierarchy)
					{
						return localAssociatedNPC;
					}
				}
			}
			return null;
		}



		#if UNITY_EDITOR

		[ContextMenu ("Convert to NPC")]
		/**
		 * Converts the Player to an NPC.
		 */
		public void ConvertToNPC ()
		{
			if (UnityVersionHandler.IsPrefabFile (gameObject))
			{
				UnityEditor.EditorUtility.DisplayDialog ("Convert " + name + " to NPC?", "Only scene objects can be converted. Place an instance of this prefab into your scene and try again.", "OK");
				return;
			}

			if (UnityEditor.EditorUtility.DisplayDialog ("Convert " + name + " to NPC?", "This will convert the Player into an NPC.  Player-only data will lost in the process, and you should back up your project first. Continue?", "OK", "Cancel"))
			{
				gameObject.tag = Tags.untagged;

				AC.Char playerAsCharacter = (AC.Char) this;
				string characterData = JsonUtility.ToJson (playerAsCharacter);
				
				NPC npc = gameObject.AddComponent <NPC>();
				JsonUtility.FromJsonOverwrite (characterData, npc);
				DestroyImmediate (this);
			}
		}

		#endif

	}
	

	/**
	 * A data container for a Player prefab.
	 */
	[System.Serializable]
	public class PlayerPrefab
	{

		/** The Player prefab */
		public Player playerOb;
		/** A unique identifier */
		public int ID;
		/** If True, this Player is the game's default */
		public bool isDefault;


		/**
		 * The default Constructor.
		 * An array of ID numbers is required, to ensure its own ID is unique.
		 */
		public PlayerPrefab (int[] idArray)
		{
			ID = 0;
			playerOb = null;
			
			if (idArray.Length > 0)
			{
				isDefault = false;
				
				foreach (int _id in idArray)
				{
					if (ID == _id)
						ID ++;
				}
			}
			else
			{
				isDefault = true;
			}
		}

	}
	
}