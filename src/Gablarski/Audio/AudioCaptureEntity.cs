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
using System.Linq;
//using Gablarski.Audio.Speex;
using Gablarski.Messages;

namespace Gablarski.Audio
{
	internal class AudioCaptureEntity
		: IDisposable
	{
		public AudioCaptureEntity (IAudioCaptureProvider audioCapture, AudioSource source, AudioEngineCaptureOptions options)
		{
			this.audioCapture = audioCapture;
			this.source = source;
			this.options = options;

			this.frameLength = (this.source.CodecSettings.FrameSize / source.CodecSettings.SampleRate) * 1000;

			if (options.Mode == AudioEngineCaptureMode.Activated)
			{
				activation = new VoiceActivation (source.CodecSettings, source.CodecSettings.FrameSize, options.StartVolume, options.ContinuationVolume, options.ContinueThreshold);
				//preprocessor = new SpeexPreprocessor (this.source.FrameSize, this.source.Frequency);
			}
		}

		public bool Talking
		{
			get;
			set;
		}

		/// <summary>
		/// Gets the frame length in milliseconds
		/// </summary>
		public int FrameLength
		{
			get { return frameLength; }
		}

		public IAudioCaptureProvider AudioCapture
		{
			get { return this.audioCapture; }
		}

		public AudioSource Source
		{
			get { return this.source; }
		}

		public AudioEngineCaptureOptions Options
		{
			get { return this.options; }
		}

		//public SpeexPreprocessor Preprocessor
		//{
		//    get { return this.preprocessor; }
		//}

		public VoiceActivation VoiceActivation
		{
			get { return this.activation; }
		}

		public bool Muted
		{
			get;
			set;
		}

		private bool isDisposed;
		private readonly VoiceActivation activation;
		private readonly int frameLength;
		//private readonly SpeexPreprocessor preprocessor;
		private readonly IAudioCaptureProvider audioCapture;
		private readonly AudioSource source;
		private readonly AudioEngineCaptureOptions options;

		internal int[] CurrentTargets
		{
			get;
			set;
		}

		internal TargetType TargetType
		{
			get;
			set;
		}

		public void Dispose()
		{
			if (this.isDisposed)
				return;

			//if (this.preprocessor != null)
			//    preprocessor.Dispose();
			if (audioCapture != null)
				audioCapture.Dispose();

			this.isDisposed = true;
		}
	}
}