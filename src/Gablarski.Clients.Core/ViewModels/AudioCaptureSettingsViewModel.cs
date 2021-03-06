﻿// Copyright (c) 2013, Eric Maupin
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
using System.Linq;
using Gablarski.Audio;

namespace Gablarski.Clients.ViewModels
{
	public class AudioCaptureSettingsViewModel
		: ViewModelBase
	{
		public AudioCaptureSettingsViewModel()
		{
			CaptureProviders = Modules.Capture;

			IAudioCaptureProvider savedCaptureProvider = Modules.Capture.FirstOrDefault (p => p.GetType().GetSimpleName() == Settings.VoiceProvider);
			if (savedCaptureProvider == null)
				CurrentCaptureProvider = Modules.Capture.FirstOrDefault();
			else {
				CurrentCaptureProvider = savedCaptureProvider;

				if (Settings.VoiceDevice != null)
				{
					var setDevice = CaptureDevices.FirstOrDefault (d => d.Device.Name == Settings.VoiceDevice);
					if (setDevice != null)
						CurrentCaptureDevice = setDevice;
				}
			}

			VoiceActivationThreshold = Settings.VoiceActivationLevel;
			VoiceActivationSilenceThreshold = Settings.VoiceActivationContinueThreshold / 100;
			UseVoiceActivation = !Settings.UsePushToTalk;
		}

		private const int FrameSize = 480;
		private void UpdateActivation()
		{
			IsActivating = false;
			ActivationLevel = 0;
			this.activation = new VoiceActivation (AudioFormat.Mono16bitLPCM, FrameSize, VoiceActivationThreshold, VoiceActivationThreshold / 2, VoiceActivationSilenceTime);
		}

		private void OnSamplesAvailable (object sender, SamplesAvailableEventArgs e)
		{
			if (this.activation == null)
				return;
			if (e.Available < FrameSize)
				return;

			byte[] samples = e.Provider.ReadSamples (FrameSize);
			ActivationLevel = this.activation.GetLevel (samples);
			IsActivating = this.activation.IsTalking (samples);
		}

		public int ActivationLevel
		{
			get { return this.activationLevel; }
			set
			{
				if (this.activationLevel == value)
					return;

				this.activationLevel = value; 
				OnPropertyChanged();
			}
		}

		public bool IsActivating
		{
			get { return this.isActivating; }
			set
			{
				if (this.isActivating == value)
					return;

				this.isActivating = value; 
				OnPropertyChanged();
			}
		}

		public IEnumerable<IAudioCaptureProvider> CaptureProviders
		{
			get;
			private set;
		}

		private IAudioCaptureProvider currentCaptureProvider;
		public IAudioCaptureProvider CurrentCaptureProvider
		{
			get { return this.currentCaptureProvider; }
			set
			{
				if (this.currentCaptureProvider == value)
					return;

				if (this.currentCaptureProvider != null)
					this.currentCaptureProvider.SamplesAvailable -= OnSamplesAvailable;

				this.currentCaptureProvider = value; 
				OnPropertyChanged();

				if (value != null) {
					value.SamplesAvailable += OnSamplesAvailable;

					CaptureDevices = new[] { new DefaultDevice() }
						.Concat (value.GetDevices())
						.Select (d => new DeviceViewModel (value, d))
						.ToArray();

					CurrentCaptureDevice = CaptureDevices.First();
				} else {
					CaptureDevices = Enumerable.Empty<DeviceViewModel>();
					CurrentCaptureDevice = null;
				}
			}
		}

		private IEnumerable<DeviceViewModel> captureDevices;
		public IEnumerable<DeviceViewModel> CaptureDevices
		{
			get { return this.captureDevices; }
			set
			{
				if (this.captureDevices == value)
					return;

				this.captureDevices = value;
				OnPropertyChanged();
			}
		}

		private DeviceViewModel currentCaptureDevice;
		public DeviceViewModel CurrentCaptureDevice
		{
			get { return this.currentCaptureDevice; }
			set
			{
				if (value == null && CurrentCaptureProvider != null)
					value = CaptureDevices.FirstOrDefault();
				if (this.currentCaptureDevice == value)
					return;

				if (UseVoiceActivation && CurrentCaptureProvider != null) {
					if (CurrentCaptureProvider.IsCapturing)
						CurrentCaptureProvider.EndCapture();

					UpdateActivation();

					if (value != null)
					{
						var actualDevice = value.Device;
						if (actualDevice is DefaultDevice)
							actualDevice = CaptureDevices.First (d => d.Device.Equals (CurrentCaptureProvider.DefaultDevice)).Device;

						CurrentCaptureProvider.Device = actualDevice;
						CurrentCaptureProvider.BeginCapture (AudioFormat.Mono16bitLPCM, FrameSize);
					}
				}

				this.currentCaptureDevice = value;
				OnPropertyChanged();
			}
		}

		private bool useVoiceActivation;
		public bool UseVoiceActivation
		{
			get { return this.useVoiceActivation; }
			set
			{
				if (this.useVoiceActivation == value)
					return;

				if (CurrentCaptureProvider != null) {
					if (!value && CurrentCaptureProvider.IsCapturing) {
						CurrentCaptureProvider.EndCapture();
						UpdateActivation();
					} else if (value && !CurrentCaptureProvider.IsCapturing) {
						UpdateActivation();
						if (CurrentCaptureProvider.Device == null) {
							CurrentCaptureProvider.Device = (CurrentCaptureDevice.Device is DefaultDevice)
								? CurrentCaptureProvider.DefaultDevice
								: CurrentCaptureDevice.Device;
						}

						CurrentCaptureProvider.BeginCapture (AudioFormat.Mono16bitLPCM, FrameSize);
					}
				}

				this.useVoiceActivation = value;
				OnPropertyChanged();
			}
		}

		private int voiceActivationThreshold;
		public int VoiceActivationThreshold
		{
			get { return this.voiceActivationThreshold; }
			set
			{
				if (this.voiceActivationThreshold == value)
					return;

				this.voiceActivationThreshold = value;
				OnPropertyChanged();

				UpdateActivation();
			}
		}

		public TimeSpan VoiceActivationSilenceTime
		{
			get { return TimeSpan.FromMilliseconds (VoiceActivationSilenceThreshold * 100); }
		}

		private int voiceActivationSilenceThreshold;
		public int VoiceActivationSilenceThreshold
		{
			get { return this.voiceActivationSilenceThreshold; }
			set
			{
				if (this.voiceActivationSilenceThreshold == value)
					return;

				this.voiceActivationSilenceThreshold = value;
				OnPropertyChanged();
				OnPropertyChanged ("VoiceActivationSilenceTime");

				UpdateActivation();
			}
		}

		public void UpdateSettings()
		{
			Settings.VoiceProvider = (CurrentCaptureProvider != null)
				? CurrentCaptureProvider.GetType().GetSimpleName()
				: null;

			Settings.VoiceDevice = (CurrentCaptureDevice != null)
				? CurrentCaptureDevice.Device.Name
				: null;

			Settings.VoiceActivationLevel = VoiceActivationThreshold;
			Settings.VoiceActivationContinueThreshold = VoiceActivationSilenceThreshold * 100;
			Settings.UsePushToTalk = !UseVoiceActivation;
		}

		public void Close()
		{
			if (CurrentCaptureProvider == null)
				return;
			
			CurrentCaptureProvider.SamplesAvailable -= OnSamplesAvailable;
			if (CurrentCaptureProvider.IsCapturing)
				CurrentCaptureProvider.EndCapture();
		}

		private VoiceActivation activation;
		private bool isActivating;
		private int activationLevel;
	}
}
