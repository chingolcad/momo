/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"MusicCrossfade.cs"
 * 
 *	Handles the fading-out half of crossfading music.
 *	When music crossfades, the original track is copied here to be fade out, while the Music object fades in the next track.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * Handles the fading-out half of crossfading music.
 	 * When music crossfades, the original track is copied here to be fade out, while the Music object fades in the next track.
	 */
	[RequireComponent (typeof (AudioSource))]
	public class MusicCrossfade : MonoBehaviour
	{

		#region Variables

		protected AudioSource _audioSource;
		protected bool isFadingOut = false;
		protected float fadeTime = 0f;
		protected float originalFadeTime = 0f;
		protected float originalVolume = 0f;

		protected bool isPlaying;

		#endregion


		#region UnityStandards

		protected void Awake ()
		{
			_audioSource = GetComponent <AudioSource>();
			_audioSource.ignoreListenerPause = KickStarter.settingsManager.playMusicWhilePaused;
		}

		#endregion


		#region PublicFunctions

		/**
		 * Updates the AudioSource's volume.
		 * This is called every frame by Music.
		 */
		public void _Update ()
		{
			if (isFadingOut)
			{
				float i = fadeTime / originalFadeTime;  // starts as 1, ends as 0
				_audioSource.volume = originalVolume * i;

				if (Mathf.Approximately (Time.time, 0f))
				{
					fadeTime -= Time.fixedDeltaTime;
				}
				else
				{
					fadeTime -= Time.deltaTime;
				}

				if (fadeTime <= 0f)
				{
					Stop ();
				}
			}
		}


		/**
		 * Stops the current audio immediately/
		 */
		public void Stop ()
		{
			isFadingOut = false;
			_audioSource.Stop ();
			isPlaying = false;
		}


		/**
		 * <summary>Checks if the crossfade audio is playing</summary>
		 * <returns>True if the crossfade audio is playing</returns>
		 */
		public bool IsPlaying ()
		{
			return isPlaying;
		}


		/**
		 * <summary>Fades out a new AudioSource</summary>
		 * <param name = "audioSourceToCopy">The AudioSource to copy clip and volume data from</param>
		 * <param name = "_fadeTime">The duration, in seconds, of the fade effect</param>
		 */
		public void FadeOut (AudioSource audioSourceToCopy, float _fadeTime)
		{
			Stop ();

			if (audioSourceToCopy == null || audioSourceToCopy.clip == null || _fadeTime <= 0f)
			{
				return;
			}

			_audioSource.clip = audioSourceToCopy.clip;
			_audioSource.outputAudioMixerGroup = audioSourceToCopy.outputAudioMixerGroup;
			_audioSource.volume = audioSourceToCopy.volume;
			_audioSource.timeSamples = audioSourceToCopy.timeSamples;
			_audioSource.loop = false;
			_audioSource.Play ();
			isPlaying = true;

			originalFadeTime = _fadeTime;
			originalVolume = audioSourceToCopy.volume;
			fadeTime = _fadeTime;

			isFadingOut = true;
		}

		#endregion

	}

}