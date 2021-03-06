// Copyright (c) 2011-2014, Eric Maupin
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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Threading.Tasks;
using Gablarski.Audio;
using Gablarski.Clients;
using System.Speech.Synthesis;
using Gablarski.Clients.Media;

namespace Gablarski.SpeechNotifier
{
	[Export (typeof (ITextToSpeech))]
	public class EventSpeech
		:  ITextToSpeech
	{
		public EventSpeech()
		{
			this.formats = this.speech.Voice.SupportedAudioFormats.Select (af =>
				new AudioFormat (GetWaveEncodingFormat (af.EncodingFormat), af.ChannelCount, af.BitsPerSample, af.SamplesPerSecond)
				).ToList();
		}

		public string Name
		{
			get { return "SAPI Text to Speech"; }
		}

		public AudioSource AudioSource
		{
			get { return this.audioSource; }
			set
			{
				if (value != null) {
					AudioCodecArgs settings = value.CodecSettings;
					this.format = new SpeechAudioFormatInfo (
						settings.SampleRate,
						settings.BitsPerSample == 8 ? AudioBitsPerSample.Eight : AudioBitsPerSample.Sixteen,
						settings.Channels == 1 ? AudioChannel.Mono : AudioChannel.Stereo);
				} else
					this.format = null;

				this.audioSource = value;
			}
		}

		public IEnumerable<AudioFormat> SupportedFormats
		{
			get { return this.formats; }
		}

		public byte[] GetSpeech (string say)
		{
			if (say == null)
				throw new ArgumentNullException ("say");
			if (AudioSource == null)
				throw new InvalidOperationException ("You must set AudioSource before calling GetSpeech");

			lock (speech) {
				speech.SetOutputToAudioStream (this.voiceBuffer, this.format);
				speech.Speak (say);

				byte[] output = this.voiceBuffer.ToArray();
				this.voiceBuffer.SetLength (0);

				return output;
			}
		}

		public Task SayAsync (string say)
		{
			if (say == null)
				throw new ArgumentNullException ("say");
			
			return Task.Factory.StartNew (o => {
				lock (sync) {
					if (media != null)
						media.AddTalker();

					lock (speech) {
						speech.SetOutputToDefaultAudioDevice();
						speech.Speak ((string)o);
					}

					if (media != null)
						media.RemoveTalker();
				}
			}, say, TaskCreationOptions.HideScheduler);
		}

		public IMediaController Media
		{
			get { return media; }

			set
			{
				lock (sync)
					media = value;
			}
		}

		public override string ToString()
		{
			return Name;
		}

		public void Dispose()
		{
			speech.Dispose();
		}

		private SpeechAudioFormatInfo format;
		private AudioSource audioSource;
		private readonly object sync = new object();
		private readonly MemoryStream voiceBuffer = new MemoryStream (120000);
		private IMediaController media;
		private readonly SpeechSynthesizer speech = new SpeechSynthesizer ();
		private readonly List<AudioFormat> formats;

		private static WaveFormatEncoding GetWaveEncodingFormat (EncodingFormat encoding)
		{
			switch (encoding)
			{
				case EncodingFormat.Pcm:
					return WaveFormatEncoding.LPCM;

				default:
					return WaveFormatEncoding.Unknown;
			}
		}
	}
}