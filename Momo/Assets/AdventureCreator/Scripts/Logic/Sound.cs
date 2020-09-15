/*
 *
 *	Adventure Creator
 *	by Chris Burton, 2013-2020
 *	
 *	"Sound.cs"
 * 
 *	This script allows for easy playback of audio sources from within the ActionList system.
 * 
 */

using UnityEngine;

namespace AC
{

	/**
	 * This component controls the volume of the AudioSource component it is attached beside, according to the volume levels set within OptionsData by the player.
	 * It also allows for AudioSources to be controlled using Actions.
	 */
	[AddComponentMenu("Adventure Creator/Logic/Sound")]
	[RequireComponent (typeof (AudioSource))]
	[HelpURL("https://www.adventurecreator.org/scripting-guide/class_a_c_1_1_sound.html")]
	public class Sound : MonoBehaviour
	{

		#region Variables

		/** The type of sound, so far as volume levels go (SFX, Music, Other) */
		[HideInInspector] public SoundType soundType;
		/** If True, then the sound can play when the game is paused */
		[HideInInspector] public bool playWhilePaused = false;
		/** The volume of the sound, relative to its categoriy's "global" volume set within OptionsData */
		[HideInInspector] public float relativeVolume = 1f;
		/** If True, then the GameObject this is attached to will not be destroyed when changing scene */
		[HideInInspector] public bool surviveSceneChange = false;

		protected float maxVolume = 1f;
		protected float smoothVolume = 1f;
		protected float smoothUpdateSpeed = 20f;

		protected float fadeTime;
		protected float originalFadeTime;
		protected FadeType fadeType;

		protected Options options;
		/** The AudioSource that AudioClip assets are played from */
		public AudioSource audioSource { get; protected set; }
		protected float otherVolume = 1f;

		protected float originalRelativeVolume;
		protected float targetRelativeVolume;
		protected float relativeChangeTime;
		protected float originalRelativeChangeTime;

		#endregion


		#region UnityStandards
		
		protected void Awake ()
		{
			Initialise ();
		}


		protected void OnEnable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void Start ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Register (this);
		}


		protected void OnDisable ()
		{
			if (KickStarter.stateHandler) KickStarter.stateHandler.Unregister (this);
		}


		/**
		 * Updates the AudioSource's volume.
		 * This is called every frame by StateHandler.
		 */
		public virtual void _Update ()
		{
			float deltaTime = Time.deltaTime;
			if (KickStarter.stateHandler.gameState == GameState.Paused)
			{
				if (playWhilePaused)
				{
					deltaTime = Time.fixedDeltaTime;
				}
				else
				{
					return;
				}
			}

			if (relativeChangeTime > 0f)
			{
				relativeChangeTime -= deltaTime;
				float i = (originalRelativeChangeTime - relativeChangeTime) / originalRelativeChangeTime; // 0 -> 1
				
				if (relativeChangeTime <= 0f)
				{
					relativeVolume = targetRelativeVolume;
				}
				else
				{
					relativeVolume = (i * targetRelativeVolume) + ((1f - i) * originalRelativeVolume);
				}
				SetMaxVolume ();
			}

			if (fadeTime > 0f && audioSource.isPlaying)
			{
				smoothVolume = maxVolume;

				fadeTime -= deltaTime;
				float progress = (originalFadeTime - fadeTime) / originalFadeTime;

				if (fadeType == FadeType.fadeIn)
				{
					if (progress > 1f)
					{
						audioSource.volume = smoothVolume;
						fadeTime = 0f;
					}
					else
					{
						audioSource.volume = progress * smoothVolume;
					}
				}
				else if (fadeType == FadeType.fadeOut)
				{
					if (progress > 1f)
					{
						audioSource.volume = 0f;
						Stop ();
					}
					else
					{
						audioSource.volume = (1 - progress) * smoothVolume;
					}
				}
				SetSmoothVolume ();
			}
			else
			{
				SetSmoothVolume ();
				if (audioSource != null)
				{
					audioSource.volume = smoothVolume;
				}
			}
		}

		#endregion


		#region PublicFunctions

		/**
		 * Called after a scene change.
		 */
		public void AfterLoad ()
		{
			// Search for duplicates carried over from scene change
			ConstantID ownConstantID = GetComponent <ConstantID>();
			if (ownConstantID != null)
			{
				Sound[] allSceneSounds = FindObjectsOfType (typeof (Sound)) as Sound[];
				foreach (Sound otherSound in allSceneSounds)
				{
					if (otherSound != this)
					{
						ConstantID otherConstantID = otherSound.GetComponent <ConstantID>();
						if (otherConstantID != null && otherConstantID.constantID == ownConstantID.constantID)
						{
							if (otherSound.IsPlaying ())
	                        {
	                        	DestroyImmediate (gameObject);
	                        }
	                        else
	                        {
								DestroyImmediate (otherSound.gameObject);
	                        }
							return;
						}
					}
				}
			}
		}


		/**
		 * Initialises the AudioSource's volume, when the scene begins.
		 */
		public void AfterLoading ()
		{
			if (audioSource == null)
			{
				audioSource = GetComponent <AudioSource>();
			}

			if (audioSource != null)
			{
				audioSource.ignoreListenerPause = playWhilePaused;
				
				if (audioSource.playOnAwake && audioSource.clip != null)
				{
					FadeIn (0.5f, audioSource.loop);
				}
				else
				{
					SetMaxVolume ();
				}

				SnapSmoothVolume ();
			}
			else
			{
				ACDebug.LogWarning ("Sound object " + this.name + " has no AudioSource component.", this);
			}
		}


		/**
		 * Plays the AudioSource's current AudioClip.
		 */
		public void Interact ()
		{
			fadeTime = 0f;
			SetMaxVolume ();
			Play (audioSource.loop);
		}
		

		/**
		 * <summary>Fades in the AudioSource's current AudioClip, after which it continues to play.</summary>
		 * <param name = "_fadeTime">The fade duration, in seconds</param>
		 * <param name = "loop">If True, then the AudioClip will loop</param>
		 * <param name = "_timeSamples">The timeSamples to play from</param>
		 */
		public void FadeIn (float _fadeTime, bool loop, int _timeSamples = 0)
		{
			if (audioSource.clip == null)
			{
				return;
			}

			audioSource.loop = loop;

			fadeTime = originalFadeTime = _fadeTime;
			fadeType = FadeType.fadeIn;
			
			SetMaxVolume ();

			audioSource.volume = 0f;
			audioSource.timeSamples = _timeSamples;
			audioSource.Play ();

			KickStarter.eventManager.Call_OnPlaySound (this, audioSource, audioSource.clip, _fadeTime);
		}
		

		/**
		 * <summary>Fades out the AudioSource's current AudioClip, after which it stops.</summary>
		 * <param name = "_fadeTime">The fade duration, in seconds</param>
		 */
		public void FadeOut (float _fadeTime)
		{
			if (_fadeTime > 0f && audioSource.isPlaying)
			{
				fadeTime = originalFadeTime = _fadeTime;
				fadeType = FadeType.fadeOut;
				
				SetMaxVolume ();

				KickStarter.eventManager.Call_OnStopSound (this, audioSource, audioSource.clip, _fadeTime);
			}
			else
			{
				Stop ();
			}
		}


		/**
		 * <summary>Checks if the AudioSource's AudioClip is being faded out.</summary>
		 * <returns>True if the AudioSource's AudioClip is being faded out</returns>
		 */
		public bool IsFadingOut ()
		{
			if (fadeTime > 0f && fadeType == FadeType.fadeOut)
			{
				return true;
			}
			return false;
		}


		/**
		 * <summary>Plays the AudioSource's current AudioClip, without starting over if it was paused or changing its "loop" variable.</summary>
		 */
		public void Play ()
		{
			if (audioSource == null)
			{
				return;
			}
			fadeTime = 0f;
			SetMaxVolume ();
			audioSource.Play ();

			KickStarter.eventManager.Call_OnPlaySound (this, audioSource, audioSource.clip, 0f);
		}
		

		/**
		 * <summary>Plays the AudioSource's current AudioClip.</summary>
		 * <param name = "loop">If true, the AudioClip will be looped</param>
		 */
		public void Play (bool loop)
		{
			if (audioSource == null)
			{
				return;
			}
			audioSource.loop = loop;
			audioSource.timeSamples = 0;
			Play ();
		}


		/**
		 * <summary>Plays an AudioClip.</summary>
		 * <param name = "clip">The AudioClip to play</param>
		 * <param name = "loop">If true, the AudioClip will be looped</param>
		 * <param name = "_timeSamples">The timeSamples to play from</param>
		 */
		public void Play (AudioClip clip, bool loop, int _timeSamples = 0)
		{
			if (audioSource == null)
			{
				return;
			}
			audioSource.clip = clip;
			audioSource.loop = loop;
			audioSource.timeSamples = _timeSamples;
			Play ();
		}


		/**
		 * <summary>Plays the AudioSource's current AudioClip from a set point.</summary>
		 * <param name = "loop">If true, the AudioClip will be looped</param>
		 * <param name = "samplePoint">The playback position in PCM samples</param>
		 */
		public void PlayAtPoint (bool loop, int samplePoint)
		{
			if (audioSource == null)
			{
				return;
			}
			audioSource.loop = loop;
			audioSource.timeSamples = samplePoint;
			Play ();
		}
		

		/**
		 * Calculates the maximum volume that the AudioSource can have.
		 * This should be called whenever the volume in OptionsData is changed.
		 */
		public void SetMaxVolume ()
		{
			maxVolume = relativeVolume;

			if (KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				SetFinalVolume ();
				return;
			}

			if (Options.optionsData != null)
			{
				if (soundType == SoundType.Music)
				{
					maxVolume *= Options.optionsData.musicVolume;
				}
				else if (soundType == SoundType.SFX)
				{
					maxVolume *= Options.optionsData.sfxVolume;
				}
				else if (soundType == SoundType.Speech)
				{
					maxVolume *= Options.optionsData.speechVolume;
				}
			}
			if (soundType == SoundType.Other)
			{
				maxVolume *= otherVolume;
			}
			SetFinalVolume ();
		}


		/**
		 * <summary>Sets the volume, but takes relativeVolume into account as well.</summary>
		 * <param name = "volume">The volume to set</param>
		 */
		public void SetVolume (float volume)
		{
			if (KickStarter.settingsManager.volumeControl == VolumeControl.AudioMixerGroups)
			{
				volume = 1f;
			}

			maxVolume = relativeVolume * volume;
			otherVolume = volume;
			SetFinalVolume ();
		}


		/**
		 * <summary>Changes the relativeVolume value.</summary>
		 * <param name = "newRelativeVolume">The new value for relativeVolume</param>
		 * <param name = "changeTime">The time, in seconds, to make the change in</param>
		 */
		public void ChangeRelativeVolume (float newRelativeVolume, float changeTime = 0f)
		{
			if (changeTime <= 0)
			{
				relativeVolume = newRelativeVolume;
				relativeChangeTime = 0f;
				SetMaxVolume ();
			}
			else
			{
				originalRelativeVolume = relativeVolume;
				targetRelativeVolume = newRelativeVolume;
				relativeChangeTime = originalRelativeChangeTime = changeTime;
			}
		}


		/**
		 * Abruptly stops the currently-playing sound.
		 */
		public void Stop ()
		{
			AudioClip oldClip = audioSource.clip;

			fadeTime = 0f;
			audioSource.Stop ();

			KickStarter.eventManager.Call_OnStopSound (this, audioSource, oldClip, 0f);
		}


		/**
		 * <summary>Checks if the sound is fading in or out.</summary>
		 * <returns>True if the sound is fading in or out</summary>
		 */
		public bool IsFading ()
		{
			return (fadeTime > 0f) ? true : false;
		}


		/**
		 * <summary>Checks if sound is playing.</summary>
		 * <returns>True if sound is playing</summary>
		 */
		public bool IsPlaying ()
		{
			if (audioSource == null)
			{
				Initialise ();
			}

			if (audioSource != null)
			{
				if (KickStarter.stateHandler.IsPaused () && !playWhilePaused)
				{
					// Special case, since in Unity 2018 isPlaying returns false if paused
					return (audioSource.time > 0f);
				}

				return audioSource.isPlaying;
			}
			return false;
		}


		/**
		 * <summary>Checks if a particular AudioClip is playing.</summary>
		 * <param name = "clip">The AudioClip to check for</param>
		 * <returns>True if the AudioClip is playing</returns>
		 */
		public bool IsPlaying (AudioClip clip)
		{
			if (audioSource != null && clip != null && audioSource.clip != null && audioSource.clip == clip && audioSource.isPlaying)
			{
				return true;
			}
			return false;
		}
		

		/**
		 * Destroys itself, if it should do.
		 */
		public void TryDestroy ()
		{
			if (this is Music || this is Ambience)
			{}
			else if (surviveSceneChange && !audioSource.isPlaying)
			{
				if (gameObject.GetComponentInParent <Player>() == null &&
					GetComponent <Player>() == null &&
					GetComponentInChildren <Player>() == null)
				{
					ACDebug.Log ("Deleting Sound object '" + gameObject + "' as it is not currently playing any sound.", gameObject);
					DestroyImmediate (gameObject);
				}
			}
		}


		/**
		 * <summary>Fades out all sounds of a particular type being played.</summary>
		 * <param name = "soundType">If the soundType matches this, the sound will end</param>
		 * <param name = "ignoreSound">The Sound object to not affect</param>
		 */
		public void EndOld (SoundType _soundType, Sound ignoreSound)
		{
			if (soundType == _soundType && audioSource.isPlaying && this != ignoreSound)
			{
				if (fadeTime <= 0f || fadeType == FadeType.fadeIn)
				{
					FadeOut (0.1f);
				}
			}
		}


		/**
		 * <summary>Updates a SoundData class with its own variables that need saving.</summary>
		 * <param name = "soundData">The original SoundData class</param>
		 * <returns>The updated SoundData class</returns>
		 */
		public SoundData GetSaveData (SoundData soundData)
		{
			soundData.isPlaying = IsPlaying ();

			soundData.isLooping = audioSource.loop;
			soundData.samplePoint = audioSource.timeSamples;
			soundData.relativeVolume = relativeVolume;

			soundData.maxVolume = maxVolume;
			soundData.smoothVolume = smoothVolume;

			soundData.fadeTime = fadeTime;
			soundData.originalFadeTime = originalFadeTime;
			soundData.fadeType = (int) fadeType;
			soundData.otherVolume = otherVolume;
			
			soundData.originalRelativeVolume = originalRelativeVolume;
			soundData.targetRelativeVolume = targetRelativeVolume;
			soundData.relativeChangeTime = relativeChangeTime;
			soundData.originalRelativeChangeTime = originalRelativeChangeTime;

			if (audioSource.clip != null)
			{
				soundData.clipID = AssetLoader.GetAssetInstanceID (audioSource.clip);
			}
			return soundData;
		}


		/**
		 * <summary>Updates its own variables from a SoundData class.</summary>
		 * <param name = "soundData">The SoundData class to load from</param>
		 */
		public void LoadData (SoundData soundData)
		{
			if (soundData.isPlaying)
			{
				audioSource.clip = AssetLoader.RetrieveAsset (audioSource.clip, soundData.clipID);
				PlayAtPoint (soundData.isLooping, soundData.samplePoint);
			}
			else
			{
				Stop ();
			}

			relativeVolume = soundData.relativeVolume;
			
			maxVolume = soundData.maxVolume;
			smoothVolume = soundData.smoothVolume;

			fadeTime = soundData.fadeTime;
			originalFadeTime = soundData.originalFadeTime;
			fadeType = (FadeType) soundData.fadeType;
			otherVolume = soundData.otherVolume;
			
			originalRelativeVolume = soundData.originalRelativeVolume;
			targetRelativeVolume = soundData.targetRelativeVolume;
			relativeChangeTime = soundData.relativeChangeTime;
			originalRelativeChangeTime = soundData.originalRelativeChangeTime;
		}

		#endregion


		#region ProtectedFunctions

		protected void Initialise ()
		{
			if (surviveSceneChange)
			{
				if (transform.root != null && transform.root != gameObject.transform)
				{
					transform.SetParent (null);
				}
				DontDestroyOnLoad (this);
			}
			
			audioSource = GetComponent <AudioSource>();
			if (audioSource != null)
			{
				if (audioSource.playOnAwake)
				{
					audioSource.playOnAwake = false;
				}

				audioSource.ignoreListenerPause = playWhilePaused;
				AdvGame.AssignMixerGroup (audioSource, soundType);
			}
		}


		protected void SetSmoothVolume ()
		{
			if (!Mathf.Approximately (smoothVolume, maxVolume))
			{
				if (smoothUpdateSpeed > 0)
				{
					smoothVolume = Mathf.Lerp (smoothVolume, maxVolume, (KickStarter.stateHandler.gameState == GameState.Paused) ? Time.fixedDeltaTime : Time.deltaTime * smoothUpdateSpeed);
				}
				else
				{
					SnapSmoothVolume ();
				}
			}
		}


		protected void SnapSmoothVolume ()
		{
			smoothVolume = maxVolume;
		}


		protected void SetFinalVolume ()
		{
			if (KickStarter.dialog.AudioIsPlaying ())
			{
				if (soundType == SoundType.SFX)
				{
					maxVolume *= 1f - KickStarter.speechManager.sfxDucking;
				}
				else if (soundType == SoundType.Music)
				{
					maxVolume *= 1f - KickStarter.speechManager.musicDucking;
				}
			}
		}


		protected void TurnOn ()
		{
			audioSource.timeSamples = 0;
			Play ();
		}


		protected void TurnOff ()
		{
			FadeOut (0.2f);
		}


		protected void Kill ()
		{
			Stop ();
		}

		#endregion

	}
	
}
