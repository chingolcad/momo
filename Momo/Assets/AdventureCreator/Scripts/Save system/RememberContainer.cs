/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"RememberContainer.cs"
 * 
 *	This script is attached to container objects in the scene
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * This script is attached to Container objects in the scene you wish to save.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Container")]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_container.html")]
	public class RememberContainer : Remember
	{

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			ContainerData containerData = new ContainerData ();
			containerData.objectID = constantID;
			containerData.savePrevented = savePrevented;
			
			if (_Container != null)
			{
				List<int> linkedIDs = new List<int>();
				List<int> counts = new List<int>();
				List<int> IDs = new List<int>();

				for (int i=0; i<_Container.items.Count; i++)
				{
					ContainerItem item = _Container.items[i];
					linkedIDs.Add (item.linkedID);
					counts.Add (item.count);
					IDs.Add (item.id);
				}

				containerData._linkedIDs = ArrayToString <int> (linkedIDs.ToArray ());
				containerData._counts = ArrayToString <int> (counts.ToArray ());
				containerData._IDs = ArrayToString <int> (IDs.ToArray ());
			}
			
			return Serializer.SaveScriptData <ContainerData> (containerData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			ContainerData data = Serializer.LoadScriptData <ContainerData> (stringData);

			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			if (_Container != null)
			{
				_Container.items.Clear ();

				int[] linkedIDs = StringToIntArray (data._linkedIDs);
				int[] counts = StringToIntArray (data._counts);
				int[] IDs = StringToIntArray (data._IDs);

				if (IDs != null)
				{
					for (int i=0; i<IDs.Length; i++)
					{
						ContainerItem newItem = new ContainerItem (linkedIDs[i], counts[i], IDs[i]);
						_Container.items.Add (newItem);
					}
				}
			}
		}


		private Container container;
		private Container _Container
		{
			get
			{
				if (container == null)
				{
					container = GetComponent <Container>();
				}
				return container;
			}
		}
		
	}
	

	/**
	 * A data container used by the RememberContainer script.
	 */
	[System.Serializable]
	public class ContainerData : RememberData
	{

		/** The ID numbers of the Inventory Items stored in the Container */
		public string _linkedIDs;
		/** The numbers of each Inventory Item stored in the Container */
		public string _counts;
		/** The unique ID of each ContainerItem stored within the Container */
		public string _IDs;

		/**
		 * The default Constructor.
		 */
		public ContainerData () { }

	}
	
}