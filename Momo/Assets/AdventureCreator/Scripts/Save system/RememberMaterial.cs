/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"RememberMaterial.cs"
 * 
 *	This script is attached to renderers with materials we wish to record changes in.
 * 
 */

using UnityEngine;
using System.Collections.Generic;

namespace AC
{

	/**
	 * Attach this to Renderer components with Materials you wish to record changes in.
	 */
	[AddComponentMenu("Adventure Creator/Save system/Remember Material")]
	[RequireComponent (typeof (Renderer))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_remember_material.html")]
	public class RememberMaterial : Remember
	{

		/**
		 * <summary>Serialises appropriate GameObject values into a string.</summary>
		 * <returns>The data, serialised as a string</returns>
		 */
		public override string SaveData ()
		{
			MaterialData materialData = new MaterialData ();
			materialData.objectID = constantID;
			materialData.savePrevented = savePrevented;

			List<string> materialIDs = new List<string>();
			Material[] mats = GetComponent <Renderer>().materials;

			foreach (Material material in mats)
			{
				materialIDs.Add (AssetLoader. GetAssetInstanceID (material));
			}
			materialData._materialIDs = ArrayToString <string> (materialIDs.ToArray ());

			return Serializer.SaveScriptData <MaterialData> (materialData);
		}
		

		/**
		 * <summary>Deserialises a string of data, and restores the GameObject to its previous state.</summary>
		 * <param name = "stringData">The data, serialised as a string</param>
		 */
		public override void LoadData (string stringData)
		{
			MaterialData data = Serializer.LoadScriptData <MaterialData> (stringData);
			if (data == null) return;
			SavePrevented = data.savePrevented; if (savePrevented) return;

			Material[] mats = GetComponent <Renderer>().materials;

			string[] materialIDs = StringToStringArray (data._materialIDs);

			for (int i=0; i<materialIDs.Length; i++)
			{
				if (mats.Length >= i)
				{
					Material _material = AssetLoader.RetrieveAsset (mats[i], materialIDs[i]);
					if (_material != null)
					{
						mats[i] = _material;
					}
				}
			}
			
			GetComponent <Renderer>().materials = mats;
		}
		
	}
	

	/**
	 * A data container used by the RememberMaterial script.
	 */
	[System.Serializable]
	public class MaterialData : RememberData
	{

		/** The unique identifier of each Material in the Renderer */
		public string _materialIDs;

		/**
		 * The default Constructor.
		 */
		public MaterialData () { }

	}
	
}
