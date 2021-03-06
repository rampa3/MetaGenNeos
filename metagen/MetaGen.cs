﻿/* 
 * This is like the Engine of MetaGen, which has the main event loop, and pointers to some of the other subsystems
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.UIX;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using BaseX;
using System.Threading;
using System.IO;
using UnityEngine;
using CodeX;
using metagen;
using System.Runtime.InteropServices;
using FrooxEngine.CommonAvatar;
using NeosAnimationToolset;


namespace metagen
{
    public class MetaGen : FrooxEngine.Component
    {
        public bool playing = false;
        public OutputState playing_state = OutputState.Stopped;
        public bool recording = false;
        public bool record_local_user = false;
        public OutputState recording_state = OutputState.Stopped;
        private DateTime utcNow;
        private DateTime recordingBeginTime;
        public DataManager dataManager;

        public bool recording_hearing = false;
        public bool play_hearing = false;
        public User recording_hearing_user;

        public bool recording_voice = false;
        public bool play_voice = true;
        private VoiceRecorder voiceRecorder;

        public bool recording_vision = false;
        public bool play_vision = false;
        public int2 camera_resolution = new int2(512,512);
        private VisionRecorder visionRecorder;

        public bool recording_streams = false;
        public bool play_streams = true;
        private PoseStreamRecorder streamRecorder;
        private UnifiedPayer streamPlayer;

        public RecordingTool animationRecorder;
        public bool recording_animation = false;
        public UnityNeos.AudioRecorderNeos hearingRecorder;
        public BotLogic botComponent;

        public MetaDataManager metaDataManager;
        public DataBase dataBase;
        int frame_index = 0;
        float MAX_CHUNK_LEN_MIN = 10f;
        public event Action<User> OnUserLeftCallback;
        public event Action<User> OnUserJoinedCallback;

        public bool is_loaded = false;

        //Metadata refers to the per-recording data about the user
        public Dictionary<User, UserMetadata> userMetaData
        {
            get
            {
                return metaDataManager.userMetaData;
            }
        }
        //the user data refers to the data about the user gotten from the database
        public Dictionary<User, MetaGenUser> users
        {
            get
            {
                return metaDataManager.users;
            }
        }

        public override void OnUserJoined(User user)
        {
            if (is_loaded && metaDataManager != null)
            {
                metaDataManager.GetUsers();
                metaDataManager.AddUserMetaData(user);
                botComponent.AddOverride(user);
                OnUserJoinedCallback.Invoke(user);
                UniLog.Log("USER JOINED " + user.UserID);
            }
        }

        public override void OnUserLeft(User user)
        {
            if (is_loaded && metaDataManager != null)
            {
                metaDataManager.GetUsers();
                metaDataManager.RemoveUserMetaData(user);
                botComponent.RemoveOverride(user);
                OnUserLeftCallback.Invoke(user);
                UniLog.Log("USER LEFT " + user.UserID);
            }
        }

        public float recording_time
        {
            get
            {
                return (float)(recording ? (DateTime.UtcNow - recordingBeginTime).TotalMilliseconds : 0f);
            }
        }

        protected override void OnAttach()
        {
            base.OnAttach();
        }
        public void Initialize()
        {
            recording_streams = true;
            recording_animation = true;
            recording_voice = true;
            recording_hearing = true;
            recording_vision = false;
            utcNow = DateTime.UtcNow;
            recordingBeginTime = DateTime.UtcNow;
            dataManager = this.Slot.AttachComponent<DataManager>();
            dataManager.metagen_comp = this;
            streamRecorder = new PoseStreamRecorder(this);
            voiceRecorder = new VoiceRecorder(this);
            visionRecorder = new VisionRecorder(camera_resolution, this);
            streamPlayer = new UnifiedPayer(dataManager, this);
            animationRecorder = Slot.AttachComponent<RecordingTool>();
            animationRecorder.metagen_comp = this;
            metaDataManager = new MetaDataManager(this);
            metaDataManager.GetUserMetaData();
        }
        //protected override void OnDispose()
        //{
        //    base.OnDispose();
        //    StopRecording();
        //}
        protected override void OnCommonUpdate()
        {
            base.OnCommonUpdate();
            World currentWorld = this.World;
            //UniLog.Log("HI from " + currentWorld.CorrespondingWorldId);

            //Start/Stop recording
            //if (this.Input.GetKeyDown(Key.R))
            //{
            //    ToggleRecording();
            //}

            //Start a new chunk, if we have been recording for 30 minutes, or start a new section, if a new user has left or joined
            if (recording && ((recording_time > MAX_CHUNK_LEN_MIN * 60 * 1000) || dataManager.ShouldStartNewSection()))
            {
                StopRecording();
                StartRecording();
            }

            //TODO: improve the playback
            //TODO: make playback not fail when file ends (Maybe can save length of stream files in its header, like for wav!)
            //TODO: make playback work on normal sessions (can't use the FingerPlayerSource. Can I create a new stream and feed that to a normal FingerPoseStreamManager?)
            //TODO: make voice playback
            //TODO: add Locomotion to playback
            //TODO: controller stream playback?

            //Start/Stop playing
            //if (this.Input.GetKeyDown(Key.P))
            //{
            //    TogglePlaying();
            //}

            //TODO: make cameras for vision recording local to not affect the performance of others
            //TODO: record eye and mouth tracking data, haptics, and biometric data via some standard dynamic variables and things?
            //TODO: Save controller streams

            //RECORD ONE FRAME
            //Record one frame of video and streams (audio is handled on the audioRecorder itself via a function tied to the audio system of Neos)
            //We condition on deltaT to be as close to 30fps as possible
            float deltaT = (float)(DateTime.UtcNow - utcNow).TotalMilliseconds;
            int new_frame = (int)Math.Floor(recording_time / 33.33333f);
            if (new_frame > frame_index)
            {
                bool streams_ok = (streamRecorder == null ? false : streamRecorder.isRecording) || !recording_streams;
                bool vision_ok = (visionRecorder == null ? false : visionRecorder.isRecording) || !recording_vision;
                bool hearing_ok = (hearingRecorder == null ? false : hearingRecorder.isRecording) || !recording_hearing;
                bool voice_ok = (voiceRecorder == null ? false : (voiceRecorder.isRecording && voiceRecorder.audio_sources_ready)) || !recording_voice;
                bool all_ready = voice_ok && streams_ok && vision_ok && hearing_ok;
                //bool all_ready = hearing_ok;
                if (recording && all_ready && streamRecorder==null? false : streamRecorder.isRecording)
                {
                    //UniLog.Log("recording streams");
                    streamRecorder.RecordStreams(deltaT);
                }

                if (recording && all_ready && visionRecorder==null? false : visionRecorder.isRecording)
                {
                    //UniLog.Log("recording vision");
                    //if (frame_index == 30)
                    //    hearingRecorder.videoStartedRecording = true;
                    visionRecorder.RecordVision();
                }

                if (recording && all_ready && animationRecorder==null? false : animationRecorder.isRecording)
                {
                    animationRecorder.RecordFrame();
                }

                //if (recording && all_ready && recording_hearing_user != null && hearingRecorder==null? false : hearingRecorder.isRecording)
                //{
                //}
                frame_index += 1;

                utcNow = DateTime.UtcNow;
                if (playing)
                {
                    //UniLog.Log("playing streams");
                    streamPlayer.PlayStreams();
                }
            }
            Slot slot = recording_hearing_user.Root.HeadSlot;
            hearingRecorder.UpdateTransform(slot.GlobalPosition, slot.GlobalRotation);

        }
        protected override void OnAudioUpdate()
        {
            try
            {
                base.OnAudioUpdate();
                if (recording && voiceRecorder == null ? false : voiceRecorder.isRecording)
                {
                    //UniLog.Log("recording voice");
                    voiceRecorder.RecordAudio();
                }
            } catch (Exception e)
            {
                //UniLog.Log("Hecc, exception in metagen.OnAudioUpdate:" + e.Message);
            }

        }

        public void StartRecording()
        {
            UniLog.Log("Start recording");
            if (!dataManager.have_started_recording_session)
                dataManager.StartRecordingSession();
            if (!recording)
                dataManager.StartSection();
            recording = true;
            recording_state = OutputState.Starting;
            frame_index = 0;
            //Set the recordings time to now
            utcNow = DateTime.UtcNow;
            recordingBeginTime = DateTime.UtcNow;
            //UniLog.Log(streamRecorder.isRecording.ToString());
            //UniLog.Log(recording_streams.ToString());
            //metaDataManager.GetUserMetaData();
            
            //STREAMS
            if (recording_streams && !streamRecorder.isRecording)
            {
                streamRecorder.StartRecording();
                //Record the first frame
                streamRecorder.RecordStreams(0f);
            }

            //ANIMATION
            if (recording_animation && !animationRecorder.isRecording)
            {
                animationRecorder = Slot.AttachComponent<RecordingTool>();
                animationRecorder.metagen_comp = this;
                animationRecorder.StartRecording();
                //Record the first frame
                animationRecorder.RecordFrame();
            }

            //AUDIO
            if (recording_voice && !voiceRecorder.isRecording)
            {
                voiceRecorder.StartRecording();
            }

            //HEARING
            if (recording_hearing && !hearingRecorder.isRecording)
            {
                hearingRecorder.StartRecording();
            }

            //VIDEO
            if (recording_vision && !visionRecorder.isRecording)
            {
                visionRecorder.StartRecording();
                //Record the first frame
                visionRecorder.RecordVision();
            }
            recording_state = OutputState.Started;
        }
        public void StopRecording()
        {
            UniLog.Log("Stop recording");
            metaDataManager.WriteUserMetaData();

            if (recording)
            {
                foreach (var item in userMetaData)
                {
                    User user = item.Key;
                    UserMetadata metadata = item.Value;
                    if (metadata.isRecording)
                        dataBase.UpdateRecordedTime(user.UserID, recording_time/1000, metadata.isPublic); //in seconds
                }
            }

            recording = false;
            recording_state = OutputState.Stopping;
            bool wait_streams = false;
            bool wait_voices = false;
            bool wait_hearing = false;
            bool wait_vision = false;
            bool wait_anim = false;

            //STREAMS
            if (streamRecorder.isRecording)
            {
                streamRecorder.StopRecording();
                wait_streams = true;
            }

            //VOICES
            if (voiceRecorder.isRecording)
            {
                voiceRecorder.StopRecording();
                wait_voices = true;
            }

            //HEARING
            if (hearingRecorder.isRecording)
            {
                hearingRecorder.StopRecording();
                wait_hearing = true;
            }

            //VISION
            if (visionRecorder.isRecording)
            {
                visionRecorder.StopRecording();
                wait_vision = true;
            }

            try
            {
                if (animationRecorder.isRecording)
                {
                    animationRecorder.PreStopRecording();
                    wait_anim = true;
                }
            } catch (Exception e)
            {
                UniLog.Log(">w< animation stopping failed");
            }


            Task task = Task.Run(() =>
            {
                try
                {
                    //STREAMS
                    if (wait_streams)
                    {
                        streamRecorder.WaitForFinish();
                        wait_streams = false;
                    }

                    //VOICES
                    if (wait_voices)
                    {
                        voiceRecorder.WaitForFinish();
                        wait_voices = false;
                    }

                    //HEARING
                    if (wait_hearing)
                    {
                        hearingRecorder.WaitForFinish();
                        wait_hearing = false;
                    }

                    //VISION
                    if (wait_vision)
                    {
                        visionRecorder.WaitForFinish();
                        wait_vision = false;
                    }

                    metagen.Util.MediaConverter.WaitForFinish();

                    //ANIMATION
                    if (wait_anim)
                    {
                        World.RunSynchronously(() =>
                        {
                            animationRecorder.StopRecording();
                            animationRecorder.WaitForFinish();
                            Slot.RemoveComponent(animationRecorder);
                        });
                        wait_anim = false;
                    }
                } catch (Exception e)
                {
                    UniLog.Log("OwO error in waiting task when stopped recording: " + e.Message);
                    UniLog.Log(e.StackTrace);
                } finally
                {
                    UniLog.Log("FINISHED STOPPING RECORDING");
                    this.recording_state = OutputState.Stopped;
                    dataManager.StopSection();
                }
            });
            //task.ContinueWith((Task t) =>
            //{
            //    UniLog.Log("FINISHED STOPPING RECORDING");
            //    this.recording_state = OutputState.Stopped;
            //    dataManager.StopSection();
            //});
        }

        public void ToggleRecording()
        {
            //UniLog.Log("Start/Stop recording");
            if (recording) StopRecording();
            else StartRecording();
        }
        public void StartPlaying(int recording_index = 0, Slot avatar_template = null)
        {
            UniLog.Log("Start playing");
            playing = true;
            playing_state = OutputState.Started;
            frame_index = 0;
            //Set the recordings time to now
            utcNow = DateTime.UtcNow;
            recordingBeginTime = DateTime.UtcNow;
            if (!streamPlayer.isPlaying)
                streamPlayer.StartPlaying(recording_index, avatar_template);
        }
        public void StopPlaying()
        {
            UniLog.Log("Stop playing");
            playing = false;
            if (streamPlayer.isPlaying)
                streamPlayer.StopPlaying();
            playing_state = OutputState.Stopped;
        }
        public void TogglePlaying()
        {
            if (playing)
            {
                StopPlaying();
            } else
            {
                StartPlaying();
            }

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            botComponent.Destroy();
        }

    }
}
