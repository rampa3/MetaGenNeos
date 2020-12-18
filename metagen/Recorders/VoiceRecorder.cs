﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.CommonAvatar;
using BaseX;
using CodeX;
using FrooxEngine.LogiX;
using System.IO;

namespace metagen
{
    class VoiceRecorder
    {
        public Dictionary<RefID, AudioOutput> audio_outputs = new Dictionary<RefID, AudioOutput>();
        private Dictionary<RefID, AudioRecorder> audio_recorders = new Dictionary<RefID, AudioRecorder>();
        private List<string> current_users_ids = new List<string>();
        public bool isRecording = false;
        public string saving_folder;
        private MetaGen metagen_comp;
        public bool audio_sources_ready = false;

        public VoiceRecorder(MetaGen component)
        {
            metagen_comp = component;
        }

        //Record one chunk from the voice audio of each user
        public void RecordAudio()
        {
            //Debug.Log("Recording audio");
            foreach (var item in audio_outputs)
            {
                AudioOutput audio_output = item.Value;
                RefID user_id = item.Key;
                if (audio_output != null)
                {
                    float[] buffer = new float[metagen_comp.Engine.AudioSystem.BufferSize];
                    buffer.EnsureSize<float>(metagen_comp.Engine.AudioSystem.BufferSize, false);
                    if (audio_output.Source.Target != null)
                    {
                        audio_sources_ready = true;
                        audio_output.Source.Target.Read(buffer.AsMonoBuffer());
                        audio_recorders[user_id].ConvertAndWrite(buffer);
                    } else
                    {
                        UniLog.Log("Restarting audio recording coz audio output source target was null!");
                        StopRecording();
                        StartRecording();
                    }
                }
            }
        }

        public void StartRecording()
        {
            Dictionary<RefID, User>.ValueCollection users = metagen_comp.World.AllUsers;
            foreach (User user in users)
            {
                RefID user_id = user.ReferenceID;
                current_users_ids.Add(user_id.ToString());
                AvatarAudioOutputManager comp = user.Root.Slot.GetComponentInChildren<AvatarAudioOutputManager>();
                AudioOutput audio_output = comp.AudioOutput.Target;
                audio_outputs[user_id] = audio_output;
                if (audio_outputs[user_id] == null)
                {
                    UniLog.Log("OwO: Audio output for user " + user_id.ToString() + " is null!");
                }
                else
                {
                    UniLog.Log("Sample rate");
                    UniLog.Log(metagen_comp.Engine.AudioSystem.Connector.SampleRate.ToString());
                    audio_recorders[user_id] = new AudioRecorder(saving_folder + "/" + user_id.ToString() + "_voice_tmp", metagen_comp.Engine.AudioSystem.BufferSize, 1, metagen_comp.Engine.AudioSystem.SampleRate, 1);
                    audio_recorders[user_id].StartWriting();
                }
            }
            isRecording = true;
        }
        public void StopRecording()
        {
            foreach (var item in audio_recorders)
            {
                item.Value.WriteHeader();
            }
            Task task = Task.Run(() =>
            {
                foreach (string user_id in current_users_ids)
                {
                    File.Move(saving_folder + "/" + user_id.ToString() + "_voice_tmp.wav", saving_folder + "/" + user_id.ToString() + "_voice.wav");
                }
                current_users_ids = new List<string>();
            });
            task.Wait();

            audio_outputs = new Dictionary<RefID, AudioOutput>();
            audio_recorders = new Dictionary<RefID, AudioRecorder>();
            isRecording = false;
        }
    }
}
