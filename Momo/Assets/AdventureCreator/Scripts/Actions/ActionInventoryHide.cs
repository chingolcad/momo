using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
    [System.Serializable]
    public class ActionInventoryHide : Action
    {
        public InvAction invAction;
        private bool shown = false;
        public ActionInventoryHide()
        {
            this.isDisplayed = true;
            category = ActionCategory.Custom;
            title = "Hide Inventory";
            description = "Hide Inventory when adding or removing an item from the Player's inventory.";
        }
        
        public override float Run()
        {

            if (KickStarter.runtimeInventory)
            {
               
                AC.PlayerMenus.GetMenuWithName("Inventory").TurnOff();
                AC.PlayerMenus.GetMenuWithName("Inventory").appearType = AppearType.MouseOver;
                    
                
            }
            return 0f;
        }
        public override void Skip()
        {
            
            base.Skip();
        }
    }
}

