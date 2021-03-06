﻿// Copyright (c) 2010-2013, Eric Maupin
// All rights reserved.
//
// Redistribution and use in source and binary forms, with
// or without modification, are permitted provided that
// the following conditions are met:
//
// - Redistributions of source code must retain the above 
//   copyright notice, this list of conditions and the
//   following disclaimer.
//
// - Redistributions in binary form must reproduce the above
//   copyright notice, this list of conditions and the
//   following disclaimer in the documentation and/or other
//   materials provided with the distribution.
//
// - Neither the name of Gablarski nor the names of its
//   contributors may be used to endorse or promote products
//   or services derived from this software without specific
//   prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS
// AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE
// GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
// THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
// DAMAGE.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cadenza;
using Cadenza.Collections;
using Gablarski.Audio;
using Gablarski.Client;
using Gablarski.Clients.Input;
using Gablarski.Clients.Media;
using Gablarski.Clients.Persistence;
using Gablarski.Clients.Windows.Properties;
using Gablarski.Messages;
using log4net;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Taskbar;
using Tempest;
using DisconnectedEventArgs = Gablarski.Client.DisconnectedEventArgs;

namespace Gablarski.Clients.Windows
{
	public partial class MainForm
		: Form
	{
		public MainForm ()
		{
			this.Icon = Resources.Headphones;

			this.gablarski = new GablarskiClient (Program.Key.Result);
			this.gablarski.ConnectionRejected += this.GablarskiConnectionRejected;
			this.gablarski.Connected += this.GablarskiConnected;
			this.gablarski.Disconnected += this.GablarskiDisconnected;
			this.gablarski.Channels.ReceivedChannelList += this.ChannelsReceivedChannelList;
			this.gablarski.Users.ReceivedUserList += this.UsersReceivedUserList;
			this.gablarski.Users.UserJoined += UsersUserJoined;
			this.gablarski.Users.UserDisconnected += UsersUserDisconnected;
			this.gablarski.Users.UserChangedChannel += UsersUserChangedChannel;
			this.gablarski.Users.UserUpdated += UsersUserUpdated;
			this.gablarski.Users.UserIgnored += UsersUserIgnored;
			this.gablarski.CurrentUser.ReceivedLoginResult += this.CurrentUserReceivedLoginResult;
			this.gablarski.CurrentUser.ReceivedJoinResult += this.CurrentUserReceivedJoinResult;

			this.gablarski.Sources.ReceivedSourceList += SourcesOnReceivedSourceList;
			this.gablarski.Sources.ReceivedAudioSource += SourcesReceivedSource;
			this.gablarski.Sources.AudioSourcesRemoved += SourcesRemoved;
			this.gablarski.Sources.AudioSourceStopped += SourceStoped;
			this.gablarski.Sources.AudioSourceStarted += SourceStarted;
			
			IEnumerable<IMediaPlayer> players = Enumerable.Empty<IMediaPlayer>();
			if (Settings.EnableMediaVolumeControl)
				players = Settings.EnabledMediaPlayerIntegrations.Select (s => (IMediaPlayer)Activator.CreateInstance (Type.GetType (s))).ToList ();

			this.mediaPlayerIntegration = new MediaController (this.gablarski, players)
			{
				NormalVolume = Settings.NormalMusicVolume,
				TalkingVolume = Settings.TalkingMusicVolume,
				UserTalkingCounts = !Settings.MediaVolumeControlIgnoresYou,
				UseCurrentPlayerVolume = Settings.UseMusicCurrentVolume
			};

			Settings.SettingChanged += SettingsSettingChanged;

			this.InitializeComponent ();

			this.users.Client = this.gablarski;
		}

		private void UsersUserIgnored (object sender, UserMutedEventArgs e)
		{
			lock (this.ignores)
			{
				string un = e.User.Username.Replace (" ", String.Empty).ToLower();
				if (this.ignores.Contains (un) && e.Unmuted)
				{
					foreach (var entry in ClientData.GetIgnores().Where (ie => ie.ServerId == this.server.Id && ie.Username.Replace(" ", String.Empty).ToLower() == un))
						ClientData.Delete (entry);

					this.ignores.Remove (un);
				}
				else if (!this.ignores.Contains (un) && !e.Unmuted)
				{
					ClientData.SaveOrUpdate (new IgnoreEntry (0) { ServerId = this.server.Id, Username = un });
					this.ignores.Add (un);
				}
			}
		}

		private void UsersUserUpdated (object sender, UserEventArgs e)
		{
			this.users.RemoveUser (e.User);
			this.users.AddUser (e.User, gablarski.Sources[e.User]);
		}

		private void SetupNotifications ()
		{
			if (this.notifications != null)
			{
				this.notifications.Close();
				this.notifications = null;
			}

			this.notifications = new NotificationHandler (this.gablarski);

			var enabledSpeech = new MutableLookup<ITextToSpeech, NotificationType>();
			foreach (var enabled in Settings.EnabledNotifications) {
				Type type = Type.GetType (enabled.Key);
				if (type == null) {
					LogManager.GetLogger ("Notifications").WarnFormat ("Notifier {0} not found", enabled.Key);
					continue;
				}

				object n = Activator.CreateInstance (type);

				var notifier = n as INotifier;
				if (notifier != null)
					this.notifications.AddNotifier (notifier, enabled);
				else if (n is ITextToSpeech)
					enabledSpeech.Add ((ITextToSpeech) n, enabled);
			}

			if (enabledSpeech.Count > 0) {
				foreach (var enabled in enabledSpeech) {
					ITextToSpeech tts = enabled.Key;
					tts.Media = this.mediaPlayerIntegration;

					var format = tts.SupportedFormats.OrderByDescending (af => af.SampleRate).FirstOrDefault();
					if (format == null)
						format = new AudioFormat (WaveFormatEncoding.LPCM, 1, 16, 44100);

					var source = this.gablarski.Sources.CreateFake ("speech", format, (short)(format.SampleRate / 100));
					this.speechSources.Add (tts, source);
					tts.AudioSource = source;
					this.gablarski.Audio.Attach (this.audioPlayback, source, new AudioEnginePlaybackOptions());

					this.notifications.AddNotifier (tts, enabled);
				}
				
				this.notifications.SpeechReceiver = this.gablarski.Sources;
			}

			this.notifications.MediaController = this.mediaPlayerIntegration;
		}

		private void SourcesOnReceivedSourceList (object sender, ReceivedListEventArgs<AudioSource> args)
		{
			SetupPlayback();
		}

		public void Connect (string host, int port)
		{
			this.btnConnect.Enabled = true;
			this.btnConnect.Image = Resources.LoadingImage;
			this.btnConnect.Text = "Cancel";
			this.btnConnect.ToolTipText = "Cancel (Connecting)";

			this.gablarski.ConnectAsync (new Target (host, port));
		}

		public void Connect (ServerEntry connectTo)
		{
			if (String.IsNullOrEmpty (connectTo.UserNickname))
			{
				/*InputForm nickname = new InputForm();
				if (nickname.ShowDialog () == DialogResult.Cancel)
					return;

				string nick = nickname.Input.Text.Trim();
				connectTo.UserNickname = nick;*/

				connectTo.UserNickname = Settings.Nickname;
			}

			this.server = connectTo;
			this.users.Server = connectTo;
			Connect ();
		}

		public bool ShowConnect (bool cancelExits)
		{
			this.btnConnect.Enabled = false;
			this.btnConnect.Image = Resources.DisconnectImage;
			this.btnConnect.Text = "Connect";
			this.btnConnect.ToolTipText = "Connect (Disconnected)";

			var login = new LoginForm();
			if (login.ShowDialog(this) == DialogResult.OK)
			{
				if (String.IsNullOrEmpty (login.Entry.UserNickname))
				{
					InputForm nickname = new InputForm();
					if (nickname.ShowDialog() == DialogResult.Cancel)
						return false;

					string nick = nickname.Input.Text.Trim();
					login.Entry.UserNickname = nick;
				}

				this.server = login.Entry;
				this.users.Server = login.Entry;
				Connect();
			}
			else if (cancelExits)
			{
				this.Close();
				return false;
			}

			return true;
		}

		private const string VoiceName = "voice";
		private const string MusicName = "music";

		private IAudioPlaybackProvider audioPlayback;
		private IAudioCaptureProvider voiceCapture;
		private AudioSource voiceSource;
		private AudioSource musicSource;

		private bool playbackDisabled;
		private void SetupPlayback ()
		{
			DisablePlayback();

			if (!this.gablarski.IsConnected)
				return;

			try
			{
				if (Settings.PlaybackProvider == null)
					throw new Exception ("Playback provider is not set");

				this.audioPlayback = (IAudioPlaybackProvider) Activator.CreateInstance (Type.GetType (Settings.PlaybackProvider));

				if (Settings.PlaybackDevice.IsNullOrWhitespace() || Settings.PlaybackDevice == "Default")
					this.audioPlayback.Device = this.audioPlayback.DefaultDevice;
				else
				{
					this.audioPlayback.Device = this.audioPlayback.GetDevices().FirstOrDefault (d => d.Name == Settings.PlaybackDevice)
												?? this.audioPlayback.DefaultDevice;
				}

				this.audioPlayback.Open();
				this.audioPlayback.Gain = Settings.GlobalVolume;

				this.gablarski.Sources.Where (s => s.OwnerId != gablarski.CurrentUser.UserId)
					.ForEach (AttachSource);

				BeginInvoke ((Action)(() =>
				{
					if (btnMute.Checked && !this.playbackDisabled)
						return;

					btnMute.Image = Resources.SoundMuteImage;
					btnMute.Checked = false;
					btnMute.Enabled = true;
					btnMute.ToolTipText = "Mute Sound";

					if (this.playbackDisabled)
						btnMuteMic.Checked = false;

					this.playbackDisabled = false;
				}));
			}
			catch (Exception ex)
			{
				Program.Raygun.Send (ex, new[] { "playback-init" }, Settings.CurrentSettings);

				if (this.audioPlayback != null)
					this.audioPlayback.Dispose();

				this.audioPlayback = null;

				BeginInvoke ((Action) (() =>
				{
					this.playbackDisabled = true;
					btnMute.Image = Resources.SoundMuteImage.ToErrorIcon();
					btnMute.Checked = true;
					btnMute.Enabled = false;
					btnMute.ToolTipText = "Playback Initialization error: " + ex.Message;
				}));
			}

			if (this.audioPlayback != null && Settings.EnableNotifications)
			{
				SetupNotifications();
				this.notifications.Notify (NotificationType.Connected, "Connected");
			}
		}

		private void DisablePlayback()
		{
			if (this.audioPlayback == null)
				return;

			this.gablarski.Audio.Detach (this.audioPlayback);
			this.audioPlayback.Dispose ();
		}

		private bool captureDisabled;
		private void SetupVoiceCapture ()
		{
			DisableVoiceCapture();

			if (!this.gablarski.IsConnected)
				return;

			try
			{
				if (Settings.VoiceProvider == null)
					throw new Exception ("Capture provider is not set");

				this.voiceCapture = (IAudioCaptureProvider)Activator.CreateInstance (Type.GetType (Settings.VoiceProvider));

				if (String.IsNullOrEmpty (Settings.VoiceDevice) || Settings.VoiceDevice == "Default")
					this.voiceCapture.Device = this.voiceCapture.DefaultDevice;
				else
				{
					this.voiceCapture.Device = this.voiceCapture.GetDevices().FirstOrDefault (d => d.Name == Settings.VoiceDevice) ??
					                           this.voiceCapture.DefaultDevice;
				}

				if (this.voiceSource != null)
				{
					gablarski.Audio.Attach (this.voiceCapture, this.voiceSource, new AudioEngineCaptureOptions
					{
						StartVolume = Settings.VoiceActivationLevel,
						ContinuationVolume = Settings.VoiceActivationLevel / 2,
						ContinueThreshold = TimeSpan.FromMilliseconds (Settings.VoiceActivationContinueThreshold),
						Mode = (!Settings.UsePushToTalk) ? AudioEngineCaptureMode.Activated : AudioEngineCaptureMode.Explicit
					});
				}

				BeginInvoke ((Action)(() =>
				{
					if (btnMute.Checked && !this.captureDisabled)
						return;

					btnMuteMic.Image = Resources.CaptureMuteImage;
					btnMuteMic.Checked = false;
					btnMuteMic.Enabled = true;
					btnMuteMic.ToolTipText = "Mute Microphone";
					this.captureDisabled = false;
				}));
			}
			catch (Exception ex)
			{
				Program.Raygun.Send (ex, new[] { "capture-init" }, Settings.CurrentSettings);

				if (this.voiceCapture != null)
					this.voiceCapture.Dispose();

				this.voiceCapture = null;

				BeginInvoke ((Action) (() =>
				{
					this.captureDisabled = true;
					btnMuteMic.Image = Resources.CaptureMuteImage.ToErrorIcon();
					btnMuteMic.Checked = true;
					btnMuteMic.Enabled = false;
					btnMuteMic.ToolTipText = "Capture Initialization error: " + ex.Message;
				}));
			}
		}

		private void DisableVoiceCapture()
		{
			foreach (var source in this.gablarski.Sources.Where (s => s.OwnerId == this.gablarski.CurrentUser.UserId))
				this.gablarski.Audio.EndCapture (source);

			if (this.voiceCapture != null)
			{
				this.gablarski.Audio.Detach (this.voiceCapture);
				this.voiceCapture.Dispose ();
			}

			if (this.musicprovider != null)
			{
				this.gablarski.Audio.Detach (this.musicprovider);
				this.musicprovider.Dispose ();
			}
		}

		private void Connect ()
		{
			Connect (this.server.Host, this.server.Port);
		}

		private async void SettingsSettingChanged (object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "DisplaySources":
					this.users.Update (this.gablarski.Channels, this.gablarski.Users, this.gablarski.Sources);
					break;

				case Settings.GlobalVolumeName:
					if (this.audioPlayback != null)
						this.audioPlayback.Gain = Settings.GlobalVolume;
					break;

				case Settings.UseMusicCurrentVolumeName:
					this.mediaPlayerIntegration.UseCurrentPlayerVolume = Settings.UseMusicCurrentVolume;
					break;

				case Settings.UsePushToTalkSettingName:
					await SetupInputAsync();
					SetupVoiceCapture();
					break;

				case Settings.VoiceActivationLevelSettingName:
					SetupVoiceCapture();
					break;

				case Settings.VoiceActivationContinueThresholdSettingName:
					SetupVoiceCapture();
					break;

				case Settings.EnableMediaVolumeControlSettingName:
				case Settings.EnabledMediaPlayerIntegrationsSettingName:		
					IEnumerable<IMediaPlayer> players = Enumerable.Empty<IMediaPlayer>();
					if (Settings.EnableMediaVolumeControl)
						players = Settings.EnabledMediaPlayerIntegrations.Select (s => (IMediaPlayer)Activator.CreateInstance (Type.GetType (s))).ToList ();

					this.mediaPlayerIntegration.MediaPlayers = players;
					break;

				case Settings.TalkingMusicVolumeSettingName:
					this.mediaPlayerIntegration.TalkingVolume = Settings.TalkingMusicVolume;
					break;

				case Settings.NormalMusicVolumeSettingName:
					this.mediaPlayerIntegration.NormalVolume = Settings.NormalMusicVolume;
					break;

				case Settings.MediaVolumeControlIgnoresYouSettingName:
					this.mediaPlayerIntegration.UserTalkingCounts = !Settings.MediaVolumeControlIgnoresYou;
					break;

				case Settings.EnabledNotificationsSettingName:
				case Settings.EnableNotificationsSettingName:
					if (!Settings.EnableNotifications && this.notifications != null)
					{
						this.notifications.Close ();
						this.notifications = null;
					}
					else if (this.audioPlayback != null)
						SetupNotifications ();

					break;

				case Settings.EnableGablarskiURLsSettingName:
					if (Settings.EnableGablarskiURLs)
						Program.EnableGablarskiURIs();
					else
						Program.DisableGablarskiURIs();

					break;

				case Settings.PlaybackProviderSettingName:
					SetupPlayback ();
					break;

				case Settings.PlaybackDeviceSettingName:
					SetupPlayback ();
					break;

				case "VoiceProvider":
					SetupVoiceCapture();
					break;

				case "VoiceDevice":
					SetupVoiceCapture();
					break;

				case "InputProvider":
				case "InputSettings":
					await SetupInputAsync();
					break;
			}
		}

		private IInputProvider inputProvider;
		private void DisableInput()
		{
			if (this.inputProvider != null)
			{
				this.inputProvider.Detach();
				this.inputProvider.CommandStateChanged -= OnCommandStateChanged;
				this.inputProvider.Dispose();
				this.inputProvider = null;
			}

			if (this.speech != null)
			{
				this.speech.StopRecognizing();
				this.speech.Close();
				this.speech.CommandStateChanged -= OnCommandStateChanged;
				this.speech = null;
			}
		}

		private ISpeechRecognizer speech;
		private async Task SetupInputAsync()
		{
			if (InvokeRequired)
			{
				BeginInvoke ((Action)(async () => await SetupInputAsync()));
				return;
			}

			DisableInput();

			this.speech = Modules.SpeechRecognizers.FirstOrDefault();
			if (this.speech != null)
			{
				try
				{
					await this.speech.OpenAsync();
					this.speech.Update (this.gablarski.Channels, this.gablarski.Users);
					this.speech.CommandStateChanged += OnCommandStateChanged;
				}
				catch (Exception ex)
				{
					MessageBox.Show ("Error initializing speech recognition: " + ex, "Speech Recognition", MessageBoxButtons.OK);
					this.speech = null;
				}
			}

			Type settingType;
			if (Settings.InputProvider.IsNullOrWhitespace() || (settingType = Type.GetType (Settings.InputProvider)) == null)
				this.inputProvider = Modules.Input.FirstOrDefault();
			else
				this.inputProvider = (IInputProvider)Activator.CreateInstance (settingType);

			if (this.inputProvider == null)
				Settings.UsePushToTalk = false;
			else
			{
				this.inputProvider.CommandStateChanged += OnCommandStateChanged;
				await this.inputProvider.AttachAsync (Handle);
				this.inputProvider.SetBindings (Settings.CommandBindings.Where (b => b.Provider.GetType().GetSimpleName() == this.inputProvider.GetType().GetSimpleName()));
			}
		}

		private void OnCommandStateChanged (object sender, CommandStateChangedEventArgs e)
		{
			switch (e.Command)
			{
				case Command.ActivateRecognition:
					if (!Settings.UsePushToTalk)
					{
						if ((bool)e.State)
							this.gablarski.Audio.MuteCapture();
						else
							this.gablarski.Audio.UnmuteCapture();
					}

					if (this.speech != null)
					{
						if ((bool) e.State)
							this.speech.StartRecognizing();
						else
							this.speech.StopRecognizing();
					}

					break;

				case Command.Talk:
					if (!Settings.UsePushToTalk || this.voiceSource == null || this.voiceCapture == null)
						return;

					if ((bool)e.State)
						this.gablarski.Audio.BeginCapture (voiceSource, this.gablarski.CurrentChannel);
					else
						this.gablarski.Audio.EndCapture (voiceSource);

					break;

				case Command.SwitchChannel:
					IChannelInfo channel = e.State as IChannelInfo;
					if (channel == null)
					{
						if (e.State is int)
							channel = this.gablarski.Channels[(int) e.State];
					}

					if (channel == null)
						return;

					this.gablarski.Users.Move (this.gablarski.CurrentUser, channel);

					break;

				case Command.MuteMic:
					if ((bool)e.State)
						BeginInvoke ((Action)(() => this.btnMuteMic.Checked = !this.btnMuteMic.Checked));

					break;

				case Command.MuteAll:
					if ((bool)e.State)
						BeginInvoke ((Action)(() => this.btnMute.Checked = !this.btnMute.Checked));

					break;

				case Command.SayCurrentSong:
					if (!(bool)e.State)
						return;

					if (this.notifications != null && this.mediaPlayerIntegration != null)
					{
						var attached = this.mediaPlayerIntegration.AttachedMediaPlayers;
						if (attached.Count() == 0)
							return;
						else if (attached.Count() == 1)
							this.notifications.Notify (NotificationType.Song, attached.First().SongName + " by " + attached.First().ArtistName);
						else
						{
							foreach (var media in attached)
								this.notifications.Notify (NotificationType.Song, String.Format ("{0} is playing {1} by {2}", media.Name, media.SongName, media.ArtistName));
						}
					}

					break;
			}
		}

		void SourceStarted (object sender, AudioSourceEventArgs e)
		{
			this.users.MarkTalking (this.gablarski.Users[e.Source.OwnerId]);
			this.users.MarkTalking (e.Source);
		}

		void SourceStoped (object sender, AudioSourceEventArgs e)
		{
			this.users.MarkSilent (this.gablarski.Users[e.Source.OwnerId]);
			this.users.MarkSilent (e.Source);
		}

		void SourcesRemoved (object sender, ReceivedListEventArgs<AudioSource> e)
		{
			foreach (var s in e.Data)
				this.gablarski.Audio.Detach (s);
		}

		void SourcesReceivedSource (object sender, ReceivedAudioSourceEventArgs e)
		{
			if (e.Result == SourceResult.Succeeded)
			{
				if (e.Source.Name == VoiceName)
				{
					voiceSource = e.Source;
					SetupVoiceCapture();
				}
				else if (e.Source.Name == MusicName)
				{
					musicSource = e.Source;
					gablarski.Audio.Attach (musicprovider, musicSource,
					                        new AudioEngineCaptureOptions { Mode = AudioEngineCaptureMode.Explicit });
					gablarski.Audio.BeginCapture (musicSource, gablarski.CurrentChannel);
				}

				users.Update (gablarski.Channels, gablarski.Users, gablarski.Sources);
			}
			else if (e.Result == SourceResult.NewSource)
			{
				AttachSource (e.Source);
			}
			else
			{
				string reason;
				switch (e.Result)
				{
					case SourceResult.FailedPermissions:
						reason = "Insufficient permissions";
						break;

					case SourceResult.FailedLimit:
						reason = "You or the server are at source capacity";
						break;

					default:
						reason = e.Result.ToString();
						break;
				}

				BeginInvoke ((Action)(() => MessageBox.Show (this, "Source '" + e.SourceName + "' request failed: " + reason, "Source Request", MessageBoxButtons.OK, MessageBoxIcon.Error)));
			}
		}

		private void AttachSource (AudioSource source)
		{
			if (this.audioPlayback == null)
				return;

			float gain = 1.0f;

			var user = gablarski.Users[source.OwnerId];
			if (user != null)
			{
				users.RemoveUser (user);
				users.AddUser (user, gablarski.Sources[user]);

				VolumeEntry volume = ClientData.GetVolumes (this.server).FirstOrDefault (ve => ve.Username == user.Username);
				if (volume != null)
					gain = volume.Gain;
			}

			this.gablarski.Audio.Attach (this.audioPlayback, source, new AudioEnginePlaybackOptions (gain));
		}

		private void UsersUserChangedChannel (object sender, ChannelChangedEventArgs e)
		{
			if (e.User.Equals (gablarski.CurrentUser))
			{
				if (musicSource != null)
				{
					gablarski.Audio.EndCapture (musicSource);
					gablarski.Audio.BeginCapture (musicSource, e.TargetChannel);
				}
				else if (voiceSource != null && !Settings.UsePushToTalk)
				{
					gablarski.Audio.EndCapture (voiceSource);
					gablarski.Audio.BeginCapture (voiceSource, e.TargetChannel);
				}
			}

			this.users.RemoveUser (e.User);
			this.users.AddUser (e.User, gablarski.Sources[e.User]);
		}

		private void UsersUserDisconnected (object sender, UserEventArgs e)
		{
			this.users.RemoveUser (e.User);
		}

		private void UsersUserJoined (object sender, UserEventArgs e)
		{
			lock (this.ignores)
			{
				if (this.ignores != null && this.ignores.Contains (e.User.Username.Replace (" ", String.Empty).ToLower()) && !this.gablarski.Users.GetIsIgnored (e.User))
					this.gablarski.Users.ToggleIgnore (e.User);
			}

			this.users.AddUser (e.User, Enumerable.Empty<AudioSource>());
		}

		private void UsersReceivedUserList (object sender, ReceivedListEventArgs<IUserInfo> e)
		{
			this.users.Update (this.gablarski.Channels, e.Data, this.gablarski.Sources);
			
			if (this.speech != null)
				this.speech.Update (this.gablarski.Channels, this.gablarski.Users);

			lock (this.ignores)
			{
				if (this.ignores == null)
					return;

				var usernames = this.gablarski.Users.ToDictionary (u => u.Username.Replace (" ", String.Empty).ToLower());
				foreach (string username in this.ignores.ToList())
				{
					IUserInfo user;
					if (!usernames.TryGetValue (username, out user) || this.gablarski.Users.GetIsIgnored (user))
						continue;

					this.gablarski.Users.ToggleIgnore (user);
					usernames.Remove (username);
				}
			}
		}

		private void ChannelsReceivedChannelList (object sender, ReceivedListEventArgs<IChannelInfo> e)
		{
			this.users.Update (e.Data, this.gablarski.Users, this.gablarski.Sources);

			if (this.speech != null)
				this.speech.Update (this.gablarski.Channels, this.gablarski.Users);
		}

		private void CurrentUserReceivedLoginResult (object sender, ReceivedLoginResultEventArgs e)
		{
			if (!e.Result.Succeeded)
			{
				Action<string, string, MessageBoxButtons, MessageBoxIcon> d = (m, c, b, i) => MessageBox.Show (this, m, c, b, i);

				switch (e.Result.ResultState)
				{
					case LoginResultState.FailedBanned:
						BeginInvoke (d, "You have been banned from this server.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
						break;

					default:
						BeginInvoke (d, "Login Failed: " + e.Result.ResultState, "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
						break;
				}

				gablarski.DisconnectAsync();
			}
			else
			{
				if (LocalServer.IsRunning)
					LocalServer.Permissions.SetAdmin (e.Result.UserId);

				string serverpassword = this.server.ServerPassword;
				if (this.gablarski.ServerInfo.Passworded && serverpassword.IsNullOrWhitespace())
					serverpassword = RequireServerPassword();

				if (this.gablarski.IsConnected)
					this.gablarski.CurrentUser.Join (this.server.UserNickname, serverpassword);
			}
		}

		async void CurrentUserReceivedJoinResult (object sender, ReceivedJoinResultEventArgs e)
		{
			if (e.Result != LoginResultState.Success)
			{
				string reason = e.Result.ToString ();
				switch (e.Result)
				{
					case LoginResultState.FailedUsernameAndPassword:
						reason = "Username and password combination not found.";
						break;

					case LoginResultState.FailedUsername:
						reason = "This server requires a user login.";
						break;

					case LoginResultState.FailedServerPassword:
						reason = "The server password supplied was incorrect.";
						break;

					case LoginResultState.FailedNicknameInUse:
						reason = "The nickname supplied is already in use.";
						break;

					case LoginResultState.FailedPassword:
						reason = "The password for the username supplied was incorrect.";
						break;

					case LoginResultState.FailedBanned:
						reason = "You have been banned from this server.";
						break;
				}

				Action<Form, string> d = (f, m) => MessageBox.Show (f, m, "Joining", MessageBoxButtons.OK, MessageBoxIcon.Error);
				BeginInvoke (d, this, "Join failed: " + reason);
				await gablarski.DisconnectAsync();
			}
			else
			{
				await SetupInputAsync();

				if (!this.gablarski.CurrentUser.IsRegistered && this.gablarski.ServerInfo.RegistrationMode != UserRegistrationMode.None)
					BeginInvoke ((Action)(() => btnRegister.Visible = true ));

				this.gablarski.Sources.Request ("voice", AudioFormat.Mono16bitLPCM, 480);
			}
		}

		private void GablarskiDisconnected (object sender, DisconnectedEventArgs e)
		{
			if (this.shuttingDown)
			    return;

			if (e.Reason == DisconnectionReason.Unknown)
				this.reconnecting = true;

			bool finishedWithPlayback = true;

			if (e.Reason == DisconnectionReason.Unknown || (e.Reason == DisconnectionReason.Requested && !this.reconnecting))
			{
				KeyValuePair<ITextToSpeech, AudioSource> tts = this.speechSources.FirstOrDefault (kvp => kvp.Key.GetType().GetSimpleName() == Settings.TextToSpeech);
				if (!tts.Equals(default(KeyValuePair<ITextToSpeech, AudioSource>)) && this.audioPlayback != null)
				{
					finishedWithPlayback = false;
					this.audioPlayback.SourceFinished += (o, args) =>
					{
						if (args.Source == tts.Value)
							DisablePlayback();
					};

					this.audioPlayback.QueuePlayback (tts.Value, tts.Key.GetSpeech ("Disconnected"));
				}
			}

			ResetState();
			DisableInput();
			if (finishedWithPlayback)
				DisablePlayback();

			DisableVoiceCapture();

			if (this.IsDisposed || this.Disposing || this.shuttingDown)
				return;

			this.Invoke ((Action)delegate
			{
				if (this.IsDisposed || this.Disposing)
					return;

				if (TaskbarManager.IsPlatformSupported)
				{
					TaskbarManager.Instance.SetOverlayIcon (this.Handle, Resources.DisconnectImage.ToIcon (), "Disconnected");
				}

				this.users.Nodes.Clear();

				if (!this.reconnecting)
				{
					this.btnConnect.Image = Resources.DisconnectImage;
					this.btnConnect.Text = "Connect";
					this.btnConnect.ToolTipText = "Connect (Disconnected)";
				}
				else
				{
					this.btnConnect.Image = Resources.LoadingImage;
					this.btnConnect.Text = "Disconnect";
					this.btnConnect.ToolTipText = "Disconnect (Reconnecting)";
				}

				this.btnComment.Enabled = false;
				this.btnMute.Enabled = false;
				this.btnMuteMic.Enabled = false;
				this.btnAFK.Enabled = false;

				this.btnRegister.Visible = false;
			});
		}

		private void ResetState()
		{
			this.ignores.Clear();
			this.speechSources.Clear();
		}

		private void GablarskiConnected (object sender, EventArgs e)
		{
			if (this.IsDisposed || this.Disposing)
				return;

			this.reconnecting = false;

			this.Invoke ((Action)delegate
			{
				if (TaskbarManager.IsPlatformSupported)
					TaskbarManager.Instance.SetOverlayIcon (this.Handle, Resources.ConnectImage.ToIcon (), "Connected");

				this.btnConnect.Image = Resources.ConnectImage;
				this.btnConnect.Text = "Disconnect";
				this.btnConnect.ToolTipText = "Disconnect (Connected)";

				this.btnComment.Enabled = true;
				this.btnMute.Enabled = true;
				this.btnMuteMic.Enabled = true;
				this.btnAFK.Enabled = true;

				this.users.SetServerNode (
					new TreeNode (this.gablarski.ServerInfo.Name)
					{
						ToolTipText = this.gablarski.ServerInfo.Description
					}
				);
			});

			if (this.server == null)
			{
				this.server = new ServerEntry (0) {
					UserNickname = Settings.Nickname
				};
				this.users.Server = this.server;
			}
			
			lock (this.ignores)
			{
				ClientData.GetIgnores().Where (i => i.ServerId == this.server.Id).Select (i => i.Username.Replace (" ", String.Empty).ToLower())
					.ForEach (un => this.ignores.Add (un));
			}

			string userpassword = this.server.UserPassword;

			if (this.server.UserNickname.IsNullOrWhitespace ())
			{
				InputForm nickname = new InputForm ();
				if (nickname.ShowDialogOnFormThread (this) == DialogResult.Cancel)
					return;

				this.server.UserNickname = nickname.Input.Text.Trim ();
			}
			
			if (!this.server.UserName.IsNullOrWhitespace() && userpassword.IsNullOrWhitespace())
			{
				InputForm input = new InputForm();
				input.Input.UseSystemPasswordChar = true;
				input.Label.Text = "User Password:";
				input.Text = "Enter Password";

				if (input.ShowDialogOnFormThread (this) != DialogResult.OK)
				{
					this.gablarski.DisconnectAsync();
					return;
				}

				userpassword = input.Input.Text;
			}

			if (this.server.UserName.IsNullOrWhitespace() || this.server.UserPassword == null)
			{
				string serverpassword = this.server.ServerPassword;
				if (this.gablarski.ServerInfo.Passworded && serverpassword.IsNullOrWhitespace())
					serverpassword = RequireServerPassword();

				if (this.gablarski.IsConnected)
					this.gablarski.CurrentUser.Join (this.server.UserNickname, this.server.UserPhonetic, serverpassword);
			}
			else
				this.gablarski.CurrentUser.Login (this.server.UserName, userpassword);
		}

		private string RequireServerPassword ()
		{
			InputForm input = new InputForm();
			input.Input.UseSystemPasswordChar = true;
			input.Label.Text = "Server Password:";
			input.Text = "Enter Password";

			if (input.ShowDialogOnFormThread (this) != DialogResult.OK)
			{
				this.gablarski.DisconnectAsync();
				return null;
			}

			return input.Input.Text;
		}

		private void GablarskiConnectionRejected (object sender, RejectedConnectionEventArgs e)
		{
			e.Reconnect = false;

			switch (e.Reason)
			{
				case ConnectionRejectedReason.CouldNotConnect:
					if (this.reconnecting)
						e.Reconnect = true;
					else
					{
						Invoke ((Action)(() =>
						{
							if (MessageBox.Show (this, "Could not connect to the server", "Connecting", MessageBoxButtons.RetryCancel,MessageBoxIcon.Warning) == DialogResult.Retry)
								e.Reconnect = !this.shuttingDown;
							else
								ShowConnect (true);
						}));
					}

					break;

				case ConnectionRejectedReason.CouldNotResolve:
					BeginInvoke ((Action)(() => MessageBox.Show ("Error resolving hostname", "Connecting", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
					break;

				case ConnectionRejectedReason.IncompatibleVersion:
					BeginInvoke ((Action)(() =>
					MessageBox.Show (this, "Connecting to the server failed because it is running a newer version of Gablarski than you are.", "Connecting",
					                 MessageBoxButtons.OK, MessageBoxIcon.Warning)));
					break;

				default:
					MessageBox.Show (e.Reason.ToString());
					break;
			}
		}

		private readonly GablarskiClient gablarski;
		private ServerEntry server;
		private bool shuttingDown;
		private bool reconnecting;

		private void MainForm_FormClosing (object sender, FormClosingEventArgs e)
		{
			this.shuttingDown = true;

			DisableInput();
			DisablePlayback();
			DisableVoiceCapture();

			if (this.notifications != null)
				this.notifications.Close ();

			this.gablarski.DisconnectAsync();
			LocalServer.Shutdown();
		}

		private void btnConnect_Click (object sender, EventArgs e)
		{
			if (this.gablarski.IsConnected || this.gablarski.IsConnecting)
			{
				this.reconnecting = false;
				this.gablarski.DisconnectAsync();
				LocalServer.Shutdown();
			}
			
			this.ShowConnect (true);
		}

		private async void btnSettings_Click (object sender, EventArgs e)
		{
			DisableInput();

			SettingsWindow settingsWindow = new SettingsWindow();
			settingsWindow.ShowDialog();

			if (inputProvider == null)
				await SetupInputAsync();
		}

		private IAudioCaptureProvider musicprovider;
		private readonly MediaController mediaPlayerIntegration;
		private NotificationHandler notifications;
		private readonly Dictionary<ITextToSpeech, AudioSource> speechSources = new Dictionary<ITextToSpeech, AudioSource>();
		private readonly HashSet<string> ignores = new HashSet<string>();

		private void btnComment_Click (object sender, EventArgs e)
		{
			var changeComment = new CommentForm (this.gablarski.CurrentUser.Comment);
			if (changeComment.ShowDialog() == DialogResult.OK)
				this.gablarski.CurrentUser.SetComment (changeComment.Comment);
		}

		private void btnRegister_Click (object sender, EventArgs e)
		{
			switch (this.gablarski.ServerInfo.RegistrationMode)
			{
				case UserRegistrationMode.WebPage:
					Uri url;
					if (Uri.TryCreate (this.gablarski.ServerInfo.RegistrationContent, UriKind.Absolute, out url) && !url.IsFile)
						Process.Start (url.AbsolutePath);

					break;

				case UserRegistrationMode.Message:
					if (TaskDialog.IsPlatformSupported)
					{
						TaskDialog d = new TaskDialog();
						d.Caption = "Registration";
						d.Text = this.gablarski.ServerInfo.RegistrationContent;
						d.Icon = TaskDialogStandardIcon.Information;
						d.Show();
					}
					else
					{
						MessageBox.Show (this.gablarski.ServerInfo.RegistrationContent, "Registration", MessageBoxButtons.OK,
						                 MessageBoxIcon.Information);
					}

					break;

				case UserRegistrationMode.Approved:
				case UserRegistrationMode.Normal:
				case UserRegistrationMode.PreApproved:
					var register = new RegisterForm (this.gablarski);
					if (register.ShowDialog (this) == DialogResult.Abort)
						this.btnRegister.Visible = false;

					break;
			}
		}

		private void btnAFK_CheckedChanged (object sender, EventArgs e)
		{
			if (this.btnAFK.Checked)
				this.gablarski.CurrentUser.SetStatus (this.gablarski.CurrentUser.Status | UserStatus.AFK);
			else
				this.gablarski.CurrentUser.SetStatus (this.gablarski.CurrentUser.Status ^ UserStatus.AFK);
		}

		private void btnMute_CheckedChanged (object sender, EventArgs e)
		{
			if (gablarski == null)
				return;

			if (this.btnMute.Checked)
			{
				if (!this.btnMuteMic.Checked)
					this.btnMuteMic.PerformClick();

				this.btnMuteMic.Enabled = false;

				if (notifications != null)
					notifications.Muted = true;

				gablarski.CurrentUser.MutePlayback();
			}
			else
			{
				if (notifications != null)
					notifications.Muted = false;

				gablarski.CurrentUser.UnmutePlayback();

				this.btnMuteMic.Enabled = true;
			}
		}

		private void btnMuteMic_CheckStateChanged(object sender, EventArgs e)
		{
			if (gablarski == null)
				return;

			if (this.btnMuteMic.Checked)
				gablarski.CurrentUser.MuteCapture();
			else
				gablarski.CurrentUser.UnmuteCapture();
		}

		private void aboutButton_Click(object sender, EventArgs e)
		{
			new AboutForm().ShowDialog (this);
		}
	}
}
