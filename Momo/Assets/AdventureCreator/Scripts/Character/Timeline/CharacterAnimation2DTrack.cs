/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"CharacterAnimation2DTrack.cs"
 * 
 *	A TrackAsset used by CharacterAnimation2DBehaviour.
 * 
 */

using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

namespace AC
{

	[TrackClipType (typeof (CharacterAnimation2DShot))]
	[TrackBindingType (typeof (AC.Char))]
	[TrackColor (0.2f, 0.6f, 0.9f)]
	public class CharacterAnimation2DTrack : TrackAsset
	{}

}