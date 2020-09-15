/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"InvInteraction.cs"
 * 
 *	This script is a container class for inventory interactions.
 * 
 */

namespace AC
{

	/**
	 * A data container for inventory interactions.
	 */
	[System.Serializable]
	public class InvInteraction
	{

		#region Variables

		/** The ActionList to run when the interaction is triggered */
		public ActionListAsset actionList;
		/** The icon, defined in CursorManager, associated with the interaction */
		public CursorIcon icon;

		#endregion


		#region Constructors

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "_icon">The icon, defined in CursorManager, associated with the interaction</param>
		 */
		public InvInteraction (CursorIcon _icon)
		{
			icon = _icon;
			actionList = null;
		}

		#endregion

	}

}