/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MusicStorage.cs"
 * 
 *	A data container for any music track that can be played using the 'Sound: Play music' Action.
 * 
 */

using UnityEngine;

namespace AC
{

	[System.Serializable]
	public class MusicStorage
	{

		#region Variables

		/** A unique identifier */
		public int ID;
		/** The music's AudioClip */
		public AudioClip audioClip;
		/** The relative volume to play the music at, as a decimal of the global music volume */
		public float relativeVolume;

		#endregion


		#region PublicFunctions

		/**
		 * <summary>The default Constructor.</summary>
		 * <param name = "idArray">An array of already-used ID numbers, so that a unique one can be generated</param>
		 */
		public MusicStorage (int[] idArray)
		{
			ID = 0;
			audioClip = null;
			relativeVolume = 1f;
			
			// Update id based on array
			if (idArray != null && idArray.Length > 0)
			{
				foreach (int _id in idArray)
				{
					if (ID == _id)
						ID ++;
				}
			}
		}

		#endregion

	}

}