/*
*
*   A very simple implementation of a very simple sound system.
*   @author Michael Heron
*   @version 1.0
*   
*/

using SDL;
using static SDL.SDL3;
using System;

namespace Shard
{
    public unsafe class SoundSDL : Sound
    {
        private static int masterVolumePercent = 60;
        private static MIX_Mixer* mixer;
        private MIX_Track[] tracks;

        private static MIX_Mixer* getMixerInstance()
        {
            if (mixer == null)
            {
                if (!SDL3_mixer.MIX_Init())
                {
                    Debug.getInstance().log("Failed to initialize sound system: " + SDL_GetError());
                    return null;
                }
                mixer = initMixer();
                applyMixerVolume(mixer);
            }

            return mixer;
        }
        
        private static MIX_Mixer* initMixer()
        {
            var defaultPlayback = SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK;
            var spec = getSpec();
            var mixer = SDL3_mixer.MIX_CreateMixerDevice(defaultPlayback, &spec);
            return mixer;
        }

        public override unsafe void pan(MIX_Track* track, float left, float right)
        {
            var gains = new MIX_StereoGains { left = left, right = right };
            SDL3_mixer.MIX_SetTrackStereo(track, &gains);
        }

        public override unsafe MIX_Track* playSound(string file, bool loop = false, float left = 0, float right = 0)
        {
   
            file = Bootstrap.getAssetManager().getAssetPath(file);
            if (string.IsNullOrWhiteSpace(file))
            {
                Debug.getInstance().log("Failed to play sound: asset path could not be resolved.");
                return null;
            }
            
            var mixer = getMixerInstance();
            
            if (mixer == null)
            {
                Debug.getInstance().log("Failed to create mixer: " + SDL_GetError());
                return null;
            }

            fixed (byte* pathPtr = System.Text.Encoding.UTF8.GetBytes(file + "\0"))
            {
                var track = this.playTrack(pathPtr, loop, left, right);
                Console.WriteLine("Track: " + track->ToString());
                return track;
            }
        }

        public override void setVolumePercent(int volumePercent)
        {
            masterVolumePercent = Math.Clamp(volumePercent, 0, 100);
            applyMixerVolume(getMixerInstance());
        }

        public override int getVolumePercent()
        {
            return masterVolumePercent;
        }

        private MIX_Track* playTrack(byte* pathPtr, bool loop = false, float left = 0, float right = 0)
        {
            var audio = SDL3_mixer.MIX_LoadAudio(mixer, pathPtr, false);
            if (audio == null)
            {
                Debug.getInstance().log("Failed to load audio: " + SDL_GetError());
                return null;
            }

            var track = SDL3_mixer.MIX_CreateTrack(mixer);
            if (track == null)
            {
                Debug.getInstance().log("Failed to create track: " + SDL_GetError());
                return null;
            }
            
            if (!SDL3_mixer.MIX_SetTrackAudio(track, audio))
            {
                Debug.getInstance().log("Failed to set track audio: " + SDL_GetError());
                return null;
            }

            var options = SDL_CreateProperties();
            if (loop && !SDL_SetNumberProperty(options, SDL3_mixer.MIX_PROP_PLAY_LOOPS_NUMBER, -1))
            {
                Debug.getInstance().log("Failed to set loop property: " + SDL_GetError());
                return null;
            }
            
            var gains = new MIX_StereoGains { left = left, right = right };
            SDL3_mixer.MIX_SetTrackStereo(track, &gains);
            
            if (!SDL3_mixer.MIX_PlayTrack(track,  options))
            {
                Debug.getInstance().log("Failed to play track: " + SDL_GetError());
                return null;
            }

            return track;
        }

        private static SDL_AudioSpec getSpec()
        {
            return new SDL_AudioSpec
            {
                format = SDL_AUDIO_F32,
                channels = 2,
                freq = 48000
            };
        }

        private static float toGain(int volumePercent)
        {
            var clamped = Math.Clamp(volumePercent, 0, 100);
            return clamped / 100f;
        }

        private static void applyMixerVolume(MIX_Mixer* targetMixer)
        {
            if (targetMixer == null)
            {
                return;
            }

            SDL3_mixer.MIX_SetMasterGain(targetMixer, toGain(masterVolumePercent));
        }

    }
}
