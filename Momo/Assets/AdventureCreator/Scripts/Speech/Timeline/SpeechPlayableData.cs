/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"SpeechPlayableData.cs"
 * 
 *	A data container for SpeechPlayableClip
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AC
{

	/**
	 * A data container for SpeechPlayableClip
	 */
	[System.Serializable]
	public class SpeechPlayableData
	{

		#region Variables

		/** The Speech Manager ID of the speech line, used for translations */
		public int lineID = -1;
		/** The display text of the speech line */
		public string messageText;

		#endregion

	}

}