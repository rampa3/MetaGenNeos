﻿using BaseX;
using FrooxEngine.UIX;
//using OBSWebsocketDotNet;
//using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FrooxEngine
{
    public class MetaGenBotPanelUI : NeosSwapCanvasPanel
    {
        //protected readonly SyncRef<Checkbox> _autoMirror;
        public readonly Sync<bool> _active;
        public readonly SyncRef<Button> _playButton;
        public readonly SyncRef<Button> _recordButton;
        public readonly SyncTime _recordingStarted;
        public readonly Sync<bool> should_record;
        public readonly Sync<bool> should_play;
        public readonly SyncRef<Text> _recordingTime;
        private NeosSwapCanvasPanel panel;

        protected override void OnAttach()
        {
            base.OnAttach();
            float2 float2 = new float2(800f, 1080f);
            this.CanvasSize = float2 * 0.4f;
            this.PhysicalHeight = this.Slot.Parent.LocalScaleToGlobal(0.3f);
            this.Panel.ShowHeader.Value = false;
            this.Panel.ShowHandle.Value = false;
            Slot holder = this.Slot.Parent.AddSlot("panel holder");
            holder.LocalPosition = new float3(1.5f, 0f, 0.5f);
            this.Slot.DestroyChildren();
            panel = holder.AttachComponent<NeosSwapCanvasPanel>();
            this.OpenConnectedPanel();
            //this._container.Target.SetParent(this.World.RootSlot);
            //var t = typeof(NeosPanel);
            //t.GetProperty("_contentSlot").SetValue(engine, compatibilityHash, null);
            //this._panel.Target.
            //this.Slot.SetParent(this.World.RootSlot);
            //this.Slot.LocalPosition = new float3(1.5f, 0.5f, 0f);
            //this.Slot.RemoveComponent(this);
        }

        //private void OpenErrorPage(string error)
        //{
        //    UIBuilder uiBuilder = this.SwapPanel(NeosSwapCanvasPanel.Slide.Left, 0.5f);
        //    uiBuilder.VerticalLayout(4f, 0.0f, new Alignment?());
        //    uiBuilder.Style.PreferredHeight = 32f;
        //    uiBuilder.Style.MinHeight = 32f;
        //    uiBuilder.Style.FlexibleHeight = 100f;
        //    LocaleString text1 = (LocaleString)error;
        //    uiBuilder.Text(in text1, true, new Alignment?(), true, (string)null);
        //    uiBuilder.Style.FlexibleHeight = -1f;
        //    LocaleString text2 = "CameraControl.OBS.ReturnToConnect".AsLocaleKey((string)null, true, (Dictionary<string, IField>)null);
        //    uiBuilder.Button(in text2, new ButtonEventHandler(this.OnReturnToConnect));
        //}

        private void OpenConnectedPanel()
        {
            UIBuilder uiBuilder1 = panel.SwapPanel(NeosSwapCanvasPanel.Slide.None, 0.5f);
            uiBuilder1.VerticalLayout(4f, 0.0f, new Alignment?());
            uiBuilder1.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
            uiBuilder1.Style.PreferredHeight = 32f;
            uiBuilder1.Style.MinHeight = 32f;

            //status text
            //SyncRef<Text> status = this._status;
            //LocaleString localeString1 = (LocaleString)"";
            //ref LocaleString local1 = ref localeString1;
            //Alignment? alignment1 = new Alignment?();
            //Text text1 = uiBuilder1.Text(in local1, true, alignment1, true, (string)null);
            //status.Target = text1;

            //Description
            Text text1 = uiBuilder1.Text("MetaGen is a project to generate a public dataset of VR experiences, for use in scientific research, and development of AI technologies");

            //recording time
            SyncRef<Text> recording_time = this._recordingTime;
            LocaleString localeString2 = (LocaleString)"";
            Text text2 = uiBuilder1.Text(localeString2);
            recording_time.Target = text2;

            uiBuilder1.Style.PreferredHeight *= 2f;
            uiBuilder1.Style.MinHeight *= 2f;

            //record button
            SyncRef<Button> recordButton = this._recordButton;
            Button button1 = uiBuilder1.Button("");
            recordButton.Target = button1;
            ButtonValueSet<bool> comp1 = button1.Slot.AttachComponent<ButtonValueSet<bool>>();
            comp1.SetValue.Value = true;
            comp1.TargetValue.Target = should_record;
            //TODO: need the button to not reference the sync in the local slot!

            //play button
            SyncRef<Button> streamButton = this._playButton;
            Button button2 = uiBuilder1.Button("");
            streamButton.Target = button2;
            ButtonValueSet<bool> comp2 = button1.Slot.AttachComponent<ButtonValueSet<bool>>();
            comp2.SetValue.Value = true;
            comp2.TargetValue.Target = should_play;

            //SyncRef<Checkbox> autoMirror = this._autoMirror;
            //LocaleString localeString8 = "CameraControl.OBS.AutoMirror".AsLocaleKey((string)null, true, (Dictionary<string, IField>)null);
            //Checkbox checkbox = uiBuilder1.Checkbox(localeString8, true, true, 4f);
            //autoMirror.Target = checkbox;
            //this._autoMirror.Target.State.SyncWithSetting<bool>("InteractiveCamera.AutoMirror", SettingSync.LocalChange.UpdateSetting);
            //this._active.Value = true;
        }
    }
}