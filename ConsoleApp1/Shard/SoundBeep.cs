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
using System.Runtime.InteropServices;

namespace Shard
{
    public unsafe class SoundSDL : Sound
    {


        public override void playSound(string file)
        {
            SDL_AudioSpec spec;
            byte* audioData;
            uint audioLen;

            file = Bootstrap.getAssetManager().getAssetPath(file);

            // Load WAV file - SDL3 API
            fixed (byte* pathPtr = System.Text.Encoding.UTF8.GetBytes(file + "\0"))
            {
                if (!SDL_LoadWAV(pathPtr, &spec, &audioData, &audioLen))
                {
                    Debug.getInstance().log("Failed to load WAV: " + SDL_GetError());
                    return;
                }
            }

            // Create audio stream for playback
            SDL_AudioStream* stream = SDL_OpenAudioDeviceStream(
                SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK,
                &spec,
                null,
                (nint)0
            );

            if (stream == null)
            {
                Debug.getInstance().log("Failed to open audio stream: " + SDL_GetError());
                SDL_free(audioData);
                return;
            }

            // Put audio data into the stream
            if (!SDL_PutAudioStreamData(stream, (nint)audioData, (int)audioLen))
            {
                Debug.getInstance().log("Failed to queue audio: " + SDL_GetError());
            }

            // Resume the stream to start playback
            SDL_ResumeAudioStreamDevice(stream);

            // Free the loaded audio data
            SDL_free(audioData);

            // Note: The stream will be cleaned up when playback completes
            // For a more robust implementation, track streams and destroy them when done
        }

    }
}

