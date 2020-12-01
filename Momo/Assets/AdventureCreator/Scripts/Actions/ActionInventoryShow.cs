using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AC
{
    [System.Serializable]
    public class ActionInventoryShow : Action
    {
        public InvAction invAction;
        private bool shown = false;
        public ActionInventoryShow()
        {
            this.isDisplayed = true;
            category = ActionCategory.Custom;
            title = "Show Inventory";
            description = "Show Inventory when adding or removing an item from the Player's inventory.";
        }
        // Start is called before the first frame update
        //void Start()
        //{

        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}
        public override float Run()
        {
         
            if (KickStarter.runtimeInventory)
            {
                if (invAction == InvAction.Add || invAction == InvAction.Remove || invAction == InvAction.Replace)
                {
                    AC.PlayerMenus.GetMenuWithName("Inventory").appearType = AppearType.Manual;
                    AC.PlayerMenus.GetMenuWithName("Inventory").TurnOn();
                    

                }
                
            }
            return 200f;
        }
        public override void Skip()
        {
            AC.PlayerMenus.GetMenuWithName("Inventory").TurnOff();
            AC.PlayerMenus.GetMenuWithName("Inventory").appearType = AppearType.MouseOver;
            base.Skip();
        }
    }
}

