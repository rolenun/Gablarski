﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gablarski.Audio;
using Gablarski.Clients.Input;
using Gablarski.Clients.Media;
using Gablarski.Clients.Windows.Entities;
using Gablarski.Clients.Windows.Properties;
using Cadenza;

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
			this.gablarskiURLs.Checked = Settings.EnableGablarskiURLs;

			this.bindingViewModel = new BindingListViewModel (this.Handle, new AnonymousCommand (OnRecord, CanRecord));
			this.bindingList.DataContext = this.bindingViewModel;
			this.inInputProvider.DisplayMember = "DisplayName";
			this.inInputProvider.DataSource = Modules.Input.ToList();
			this.inInputProvider.SelectedText = Settings.InputProvider;

			this.playbackSelector.ProviderSource = Modules.Playback.Cast<IAudioDeviceProvider>();
			this.playbackSelector.SetProvider (Settings.PlaybackProvider);
			this.playbackSelector.SetDevice (Settings.PlaybackDevice);

			this.voiceSelector.ProviderSource = Modules.Capture.Cast<IAudioDeviceProvider>();
			this.voiceSelector.SetProvider (Settings.VoiceProvider);
			this.voiceSelector.SetDevice (Settings.VoiceDevice);

			this.playbackVolume.Value = (int)(Settings.GlobalVolume * 100);

			this.voiceActivation.Checked = !Settings.UsePushToTalk;
			this.threshold.Value = Settings.VoiceActivationContinueThreshold / 100;
			this.dispThreshold.Text = String.Format ("{0:N1}s", (double)this.threshold.Value / 10);
			this.vadSensitivity.Value = Settings.VoiceActivationLevel;

			this.inUseCurrentVolume.Checked = Settings.UseMusicCurrentVolume;
			this.volumeControl.Checked = Settings.EnableMediaVolumeControl;
			this.musicIgnoreYou.Checked = Settings.MediaVolumeControlIgnoresYou;
			this.talkingVolume.Value = Settings.TalkingMusicVolume;
			this.normalVolume.Value = Settings.NormalMusicVolume;

			foreach (IMediaPlayer player in Modules.MediaPlayers)
			{
				this.musicPlayers.Items.Add (player.GetType().Name.Remove ("Integration", "Provider"), Settings.EnabledMediaPlayerIntegrations.Any (s => s.Contains (player.GetType().FullName)));
			}

			this.enableNotifications.Checked = Settings.EnableNotifications;
			foreach (var n in Modules.Notifiers.Cast<INamedComponent>().Concat (Modules.TextToSpeech.Cast<INamedComponent>()))
			{
				try
				{
					bool enabled = false;
					foreach (var s in Settings.EnabledNotifiers)
					{
						if (!n.GetType().AssemblyQualifiedName.Contains (s))
							continue;

						enabled = true;
					}

					this.notifiers.Items.Add (new ListViewItem (n.Name)
					{
						Tag = n,
						Checked = enabled
					});
				}
				catch
				{
				}
			}
		}

		private void btnOk_Click (object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Settings.ShowConnectOnStart = this.inConnectOnStart.Checked;
			Settings.DisplaySources = this.inDisplaySources.Checked;
			Settings.EnableGablarskiURLs = this.gablarskiURLs.Checked;

			DisableInput();

			var inputProvider = this.inInputProvider.SelectedItem as IInputProvider;
			if (inputProvider != null)
			{
				Settings.InputProvider = inputProvider.ToString();
				Settings.CommandBindings.Clear();

				foreach (var b in this.bindingViewModel.Bindings)
					Settings.CommandBindings.Add (new CommandBinding (inputProvider, b.Command, b.Input));
			}
			else
			{
				Settings.InputProvider = String.Empty;
			}

			Settings.UsePushToTalk = !this.voiceActivation.Checked;

			if (this.playbackSelector.Provider != null)
			{
				Settings.PlaybackProvider = this.playbackSelector.Provider.GetType().AssemblyQualifiedName;
				Settings.PlaybackDevice = this.playbackSelector.Device.Name;
			}
			else
			{
				Settings.PlaybackProvider = null;
				Settings.PlaybackDevice = null;
			}

			if (this.voiceSelector.Provider != null)
			{
				Settings.VoiceProvider = this.voiceSelector.Provider.GetType().AssemblyQualifiedName;
				Settings.VoiceDevice = this.voiceSelector.Device.Name;
			}
			else
			{
				Settings.VoiceProvider = null;
				Settings.VoiceDevice = null;
			}

			Settings.GlobalVolume = this.playbackVolume.Value / (float)100;

			Settings.VoiceActivationContinueThreshold = this.threshold.Value * 100;
			Settings.VoiceActivationLevel = this.vadSensitivity.Value;

			Settings.EnableMediaVolumeControl = this.volumeControl.Checked;
			Settings.UseMusicCurrentVolume = this.inUseCurrentVolume.Checked;
			Settings.TalkingMusicVolume = this.talkingVolume.Value;
			Settings.NormalMusicVolume = this.normalVolume.Value;
			Settings.MediaVolumeControlIgnoresYou = this.musicIgnoreYou.Checked;

			List<string> enabledPlayers = new List<string> ();
			foreach (Type playerSupport in Modules.MediaPlayers.Select (m => m.GetType()))
			{
				foreach (string enabled in this.musicPlayers.CheckedItems.Cast<string> ())
				{
					if (!playerSupport.Name.Contains (enabled))
						continue;
					
					enabledPlayers.Add (playerSupport.FullName + ", " + playerSupport.Assembly.GetName().Name);
				}
			}
			Settings.EnabledMediaPlayerIntegrations = enabledPlayers;

			Settings.EnableNotifications = this.enableNotifications.Checked;
			List<string> enabledNotifiers = new List<string>();
			foreach (ListViewItem li in this.notifiers.CheckedItems.Cast<ListViewItem>().Where (li => li.Checked))
			{
				var t = li.Tag.GetType();
				enabledNotifiers.Add (t.FullName + ", " + t.Assembly.GetName().Name);
			}
			Settings.EnabledNotifiers = enabledNotifiers;

			Settings.SaveSettings();

			Close();
		}

		private string inputSettings;
		private BindingListViewModel bindingViewModel;
		private CommandBindingSettingEntry recordingEntry;
		private readonly object inputSync = new object();

		private void inInputProvider_SelectedValueChanged(object sender, EventArgs e)
		{
			if (this.inInputProvider.SelectedItem == null)
			{
				this.addBinding.Enabled = false;
				return;
			}

			this.addBinding.Enabled = true;
			this.bindingViewModel.InputProvider = (IInputProvider)this.inInputProvider.SelectedItem;
		}

		private void DisableInput()
		{
			if (this.bindingViewModel != null)
				this.bindingViewModel.InputProvider = null;
		}

		private void threshold_Scroll (object sender, EventArgs e)
		{
			this.dispThreshold.Text = String.Format ("{0:N1}s", (double)this.threshold.Value / 10);
		}

		private void SettingsForm_FormClosing (object sender, FormClosingEventArgs e)
		{
			if (DialogResult != DialogResult.OK)
				DisableInput();
		}

		private void inUseCurrentVolume_CheckedChanged(object sender, EventArgs e)
		{
			this.normalVolume.Enabled = !this.inUseCurrentVolume.Checked;
		}

		private void OnRecord (object obj)
		{
			var entry = (obj as CommandBindingSettingEntry);
			if (entry == null)
				return;

			this.recordingEntry = entry;
			this.recordingEntry.Recording = true;
			((AnonymousCommand)this.bindingViewModel.RecordCommand).ChangeCanExecute();

			this.addBinding.Enabled = false;
			this.bindingViewModel.InputProvider.NewRecording += OnNewRecording;
			this.bindingViewModel.InputProvider.BeginRecord();
		}

		private bool CanRecord (object arg)
		{
			var binding = (arg as CommandBindingSettingEntry);
			if (binding == null)
				return false;

			return !binding.Recording;
		}

		private void addBinding_LinkClicked (object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.bindingViewModel.Bindings.Add (new CommandBindingSettingEntry (this.bindingViewModel.InputProvider));
		}

		private void OnNewRecording (object sender, RecordingEventArgs e)
		{
			if (this.recordingEntry == null)
				return;

			CommandBindingSettingEntry entry;
			lock (this.inputSync)
			{
				if (this.recordingEntry == null)
					return;

				e.Provider.NewRecording -= OnNewRecording;
				e.Provider.EndRecord();

				entry = this.recordingEntry;
				this.recordingEntry = null;

				entry.Input = e.RecordedInput;
				entry.ProviderType = e.Provider.GetType().Name;
				entry.Recording = false;
			}

			BeginInvoke ((Action<CommandBindingSettingEntry>)(be =>
			{
				((AnonymousCommand)this.bindingViewModel.RecordCommand).ChangeCanExecute();
				this.addBinding.Enabled = true;
			}), entry);
		}
	}
}