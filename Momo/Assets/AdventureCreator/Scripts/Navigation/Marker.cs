/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Marker.cs"
 * 
 *	This script allows a simple way of teleporting
 *	characters and objects around the scene.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * A component used to create reference transforms, as needed by the PlayerStart and various Actions.
	 * When the game begins, the renderer will be disabled and the GameObject will be rotated if the game is 2D.
	 */
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_marker.html")]
	[AddComponentMenu("Adventure Creator/Navigation/Marker")]
	public class Marker : MonoBehaviour
	{

		#region UnityStandards

		protected void Awake ()
		{
			Renderer _renderer = GetComponent <Renderer>();
			if (_renderer != null)
			{
				_renderer.enabled = false;
			}
			
			if (SceneSettings.IsUnity2D ())
			{
				transform.RotateAround (transform.position, Vector3.right, 90f);
				transform.RotateAround (transform.position, transform.right, -90f);
			}
		}

		#endregion
		
	}

}