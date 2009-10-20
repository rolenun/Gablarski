﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gablarski.Clients.Input;
using Gablarski.Clients.Windows.Properties;
using Mono.Rocks;

namespace Gablarski.Clients.Windows
{
	public partial class SettingsForm
		: Form
	{
		public SettingsForm ()
		{
			this.Icon = Resources.SettingsImage.ToIcon();
			InitializeComponent ();
		}

		private void SettingsForm_Load (object sender, EventArgs e)
		{
			this.inDisplaySources.Checked = Settings.DisplaySources;
			this.inConnectOnStart.Checked = Settings.ShowConnectOnStart;

			this.ptt.Checked = Settings.UsePushToTalk;
			this.inInputProvider.DisplayMember = "Name";
			this.inInputProvider.DataSource = Modules.Input.ToList();
			this.inInputProvider.SelectedText = Settings.InputProvider;

			this.voiceSelector.ProviderSource = Modules.Capture;
			this.voiceSelector.SetProvider (Settings.VoiceProvider);
			this.voiceSelector.SetDevice (Settings.VoiceDevice);
			
			this.inputSettings = Settings.InputSettings;
			this.dispInput.Text = currentInputProvider.GetNiceInputName (this.inputSettings);
			this.threshold.Value = Settings.VoiceActivationContinueThreshold / 100;
			this.vadSensitivity.Value = Settings.VoiceActivationLevel;

			this.talkingVolume.Value = Settings.TalkingMusicVolume;
			this.normalVolume.Value = Settings.NormalMusicVolume;
			foreach (Type player in Modules.MediaPlayers)
			{
				this.musicPlayers.Items.Add (player.Name.Remove ("Integration", "Provider"), Settings.EnabledMediaPlayerIntegrations.Any (s => s.Contains (player.FullName)));
			}
			this.musicIgnoreYou.Checked = Settings.MediaVolumeControlIgnoresYou;
		}

		private void btnOk_Click (object sender, EventArgs e)
		{
			Settings.ShowConnectOnStart = this.inConnectOnStart.Checked;
			Settings.DisplaySources = this.inDisplaySources.Checked;

			DisableInput();
			Settings.InputProvider = this.inInputProvider.SelectedItem.ToString();
			Settings.InputSettings = this.inputSettings;
			Settings.UsePushToTalk = this.ptt.Checked;

			Settings.VoiceProvider = this.voiceSelector.Provider.AssemblyQualifiedName;
			Settings.VoiceDevice = this.voiceSelector.Device.Name;
			Settings.VoiceActivationContinueThreshold = this.threshold.Value * 100;
			Settings.VoiceActivationLevel = this.vadSensitivity.Value;

			Settings.TalkingMusicVolume = this.talkingVolume.Value;
			Settings.NormalMusicVolume = this.normalVolume.Value;
			//Settings.EnabledMediaPlayerIntegrations = this.musicPlayers.CheckedItems.Cast<string>()
			Settings.MediaVolumeControlIgnoresYou = this.musicIgnoreYou.Checked;

			Settings.SaveSettings();

			this.Close();
		}

		private void btnCancel_Click (object sender, EventArgs e)
		{
			DisableInput();
		}

		private string inputSettings;
		private IInputProvider currentInputProvider;

		void OnInputStateChanged (object sender, InputStateChangedEventArgs e)
		{
			if (e.State == InputState.Off)
				return;

			string nice;
			this.inputSettings = this.currentInputProvider.EndRecord (out nice);

			BeginInvoke ((Action<string>)(s =>
			{
				this.dispInput.Text = s;
				this.linkSet.Enabled = true;
				this.linkSet.Text = "Set";
			}), nice);
		}

		private void inInputProvider_SelectedIndexChanged (object sender, EventArgs e)
		{
			DisableInput();

			if (this.inInputProvider.SelectedItem == null)
				return;

			currentInputProvider = (IInputProvider)Activator.CreateInstance ((Type)this.inInputProvider.SelectedItem);
			currentInputProvider.InputStateChanged += OnInputStateChanged;
			currentInputProvider.Attach (this.Handle, null);
		}

		private void DisableInput()
		{
			if (currentInputProvider == null)
				return;

			currentInputProvider.Detach();
			currentInputProvider.InputStateChanged -= OnInputStateChanged;
			currentInputProvider.Dispose();
			currentInputProvider = null;
		}

		private void linkSet_LinkClicked (object sender, LinkLabelLinkClickedEventArgs e)
		{
			if (this.currentInputProvider == null)
				return;

			if (this.linkSet.Text == "Set")
			{
				this.dispInput.Text = "Press a key";
				this.linkSet.Text = "Cancel";
				this.currentInputProvider.BeginRecord();
			}
			else
			{
				this.dispInput.Text = currentInputProvider.GetNiceInputName (this.inputSettings);
				this.linkSet.Text = "Set";
				this.currentInputProvider.EndRecord();
			}
		}

		private void linkClear_LinkClicked (object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.dispInput.Text = String.Empty;
			this.inputSettings = null;
		}

		private void threshold_Scroll (object sender, EventArgs e)
		{
			this.dispThreshold.Text = String.Format ("{0:N1}s", (double)this.threshold.Value / 10);
		}
	}
}