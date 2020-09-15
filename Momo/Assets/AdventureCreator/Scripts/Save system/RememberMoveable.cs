/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"RememberMoveable.cs"
 * 
 *	This script, when attached to Moveable objects in the scene,
 *	will record appropriate positional data
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This script is attached to Moveable, Draggable or PickUp objects you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Moveable")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_moveable.html")]
	public class RememberMoveable : Remember
	{

		/** Determines whether the object is on or off when the game starts */
		public AC_OnOff startState = AC_OnOff.On;
		
		private bool loadedData = false;


		private void Awake ()
		{
			if (loadedData) return;

			if (KickStarter.settingsManager && GameIsPlaying ())
			{
				if (GetComponent <DragBase>())
				{
					if (startState == AC_OnOff.On)
					{
						GetComponent <DragBase>().TurnOn ();
					}
					else
					{
						GetComponent <DragBase>().TurnOff ();
					}
				}

				if (startState == AC_OnOff.On)
				{
					this.gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
				}
				else
				{
					this.gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);
				}
			}
		}


		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			MoveableData moveableData = new MoveableData ();
			
			moveableData.objectID = constantID;
			moveableData.savePrevented = savePrevented;

			if (gameObject.layer == LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer))
			{
				moveableData.isOn = true;
			}
			else
			{
				moveableData.isOn = false;
			}

			if (GetComponent <Moveable_Drag>())
			{
				Moveable_Drag moveable_Drag = GetComponent <Moveable_Drag>();
				moveableData.trackValue = moveable_Drag.trackValue;
				moveableData.revolutions = moveable_Drag.revolutions;
			}
			
			moveableData.LocX = transform.position.x;
			moveableData.LocY = transform.position.y;
			moveableData.LocZ = transform.position.z;

			moveableData.RotX = transform.eulerAngles.x;
			moveableData.RotY = transform.eulerAngles.y;
			moveableData.RotZ = transform.eulerAngles.z;
			
			moveableData.ScaleX = transform.localScale.x;
			moveableData.ScaleY = transform.localScale.y;
			moveableData.ScaleZ = transform.localScale.z;

			if (GetComponent <Moveable>())
			{
				moveableData = GetComponent <Moveable>().SaveData (moveableData);
			}

			return Serializer.SaveScriptData <MoveableData> (moveableData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			MoveableData data = Serializer.LoadScriptData <MoveableData> (stringData);
			if (data == null)
			{
				loadedData = false;
				return;
			}
			SavePrevented = data.savePrevented; if (savePrevented) return;

			if (GetComponent <DragBase>())
			{
				if (data.isOn)
				{
					GetComponent <DragBase>().TurnOn ();
				}
				else
				{
					GetComponent <DragBase>().TurnOff ();
				}
			}

			if (data.isOn)
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.hotspotLayer);
			}
			else
			{
				gameObject.layer = LayerMask.NameToLayer (KickStarter.settingsManager.deactivatedLayer);
			}

			transform.position = new Vector3 (data.LocX, data.LocY, data.LocZ);
			transform.eulerAngles = new Vector3 (data.RotX, data.RotY, data.RotZ);
			transform.localScale = new Vector3 (data.ScaleX, data.ScaleY, data.ScaleZ);

			if (GetComponent <Moveable_Drag>())
			{
				Moveable_Drag moveable_Drag = GetComponent <Moveable_Drag>();
				moveable_Drag.LetGo (true);
				if (moveable_Drag.dragMode == DragMode.LockToTrack && moveable_Drag.track != null)
				{
					moveable_Drag.trackValue = data.trackValue;
					moveable_Drag.revolutions = data.revolutions;
					moveable_Drag.StopAutoMove ();
					moveable_Drag.track.SetPositionAlong (data.trackValue, moveable_Drag);
				}
			}

			if (GetComponent <Moveable>())
			{
				GetComponent <Moveable>().LoadData (data);
			}

			loadedData = true;
		}
		
	}
	

	/**
	 * A data container used by the RememberMoveable script.
	 */
	[System.Serializable]
	public class MoveableData : RememberData
	{

		/** True if the object is on */
		public bool isOn;

		/** How far along a DragTrack a Draggable object is (if locked to a track) */
		public float trackValue;
		/** If a Draggable object is locked to a DragTrack_Curved, how many revolutions it has made */
		public int revolutions;

		/** Its X position */
		public float LocX;
		/** Its Y position */
		public float LocY;
		/** Its Z position */
		public float LocZ;

		/** If True, the attached Moveable component is rotating with euler angles, not quaternions */
		public bool doEulerRotation;
		/** Its W rotation */
		public float RotW;
		/** Its X rotation */
		public float RotX;
		/** Its Y position */
		public float RotY;
		/** Its Z position */
		public float RotZ;

		/** Its X scale */
		public float ScaleX;
		/** Its Y scale */
		public float ScaleY;
		/** Its Z scale */
		public float ScaleZ;

		/** If True, the movement is occuring in world-space */
		public bool inWorldSpace;


		/**
		 * The default Constructor.
		 */
		public MoveableData () { }
		
	}
	
}