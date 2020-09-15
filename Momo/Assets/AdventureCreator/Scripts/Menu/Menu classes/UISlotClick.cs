using UnityEngine;
using UnityEngine.EventSystems;

namespace AC
{

	/**
	 * This component acts as a click handler for Unity UI Buttons, and is added automatically by UISlot.
	 */
	public class UISlotClick : MonoBehaviour, IPointerClickHandler
	{

		private AC.Menu menu;
		private MenuElement menuElement;
		private int slot;


		/**
		 * <summary>Syncs the component to a slot within a menu.</summary>
		 * <param name = "_menu">The Menu that the Button is linked to</param>
		 * <param name = "_element">The MenuElement within _menu that the Button is linked to</param>
		 * <param name = "_slot">The index number of the slot within _element that the Button is linked to</param>
		 */
		public void Setup (AC.Menu _menu, MenuElement _element, int _slot)
		{
			if (_menu == null)
			{
				return;
			}

			menu = _menu;
			menuElement = _element;
			slot = _slot;
		}


		private void Update ()
		{
			if (menuElement)
			{
				if (KickStarter.playerInput != null && KickStarter.playerInput.InputGetButtonDown ("InteractionB"))
				{
					if (KickStarter.playerMenus.IsEventSystemSelectingObject (gameObject))
					{
						menuElement.ProcessClick (menu, slot, MouseState.RightClick);
					}
				}
			}
		}


		public void OnPointerClick (PointerEventData eventData)
		{
			if (menuElement)
			{
				if (eventData.button == PointerEventData.InputButton.Right)
				{
					menuElement.ProcessClick (menu, slot, MouseState.RightClick);
				}
			}
		}

	}

}