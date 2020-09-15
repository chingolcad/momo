/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"InteractiveBoundary.cs"
 * 
 *	This script is used to limit Hotspot interactivity to players that are within a given volume.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Used to limit a Hotspot's interactivity to Players that are within a given volume.
	 * Attach this to a Trigger collider, and assign in a Hotspot's Inspector. When assigned, the Hotspot will only be interactable when the Player is within the collider's boundary.
	 */
	[AddComponentMenu("Adventure Creator/Hotspots/Interactive boundary")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_interactive_boundary.html")]
	public class InteractiveBoundary : MonoBehaviour
	{

		#region Variables

		protected bool forcePresence;
		protected bool playerIsPresent;

		#endregion


		#region UnityStandards

		protected void OnEnable ()
		{
			EventManager.OnSetPlayer += OnSwitchPlayer;
		}


		protected void OnDisable ()
		{
			EventManager.OnSetPlayer -= OnSwitchPlayer;
		}


		protected void OnSwitchPlayer (Player player)
		{
			playerIsPresent = false;
		}


		protected void OnTriggerEnter (Collider other)
		{
			if (KickStarter.player != null && other.gameObject == KickStarter.player.gameObject)
			{
				playerIsPresent = true;
			}
        }


		protected void OnTriggerExit (Collider other)
		{
			if (KickStarter.player != null && other.gameObject == KickStarter.player.gameObject)
			{
				playerIsPresent = false;
			}
        }


		protected void OnTriggerStay2D (Collider2D other)
		{
			if (KickStarter.player != null && other.gameObject == KickStarter.player.gameObject)
			{
				playerIsPresent = true;
			}
		}


		protected void OnTriggerExit2D (Collider2D other)
		{
			if (KickStarter.player != null && other.gameObject == KickStarter.player.gameObject)
			{
				playerIsPresent = false;
			}
		}

		#endregion


		#region GetSet

		/** True if the active Player is within the Collider boundary */
		public bool PlayerIsPresent
		{
			get
			{
				if (forcePresence)
				{
					return true;
				}
				return playerIsPresent;
			}
		}


		/** If True, the Player will always be considered as present within the Collider boundary, even when not physically so */
		public bool ForcePresence
		{
			set
			{
				forcePresence = value;
			}
		}

		#endregion

	}

}