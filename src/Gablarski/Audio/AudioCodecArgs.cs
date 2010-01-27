// Copyright (c) 2010, Eric Maupin
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

namespace Gablarski.Audio
{
	public class AudioCodecArgs
	{
		public AudioCodecArgs()
			: this (1, 48000, 44100, 512, 10)
		{
		}

		public AudioCodecArgs (AudioCodecArgs args)
		{
			if (args == null)
				throw new ArgumentNullException ("args");

			Channels = args.Channels;
			Bitrate = args.Bitrate;
			Frequency = args.Frequency;
			FrameSize = args.FrameSize;
			Complexity = args.Complexity;
		}

		public AudioCodecArgs (byte channels, int bitrate, int frequency, short frameSize, byte complexity)
		{
			Channels = channels;
			Bitrate = bitrate;
			Frequency = frequency;
			FrameSize = frameSize;
			Complexity = complexity;
		}

		internal AudioCodecArgs (IValueReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException ("reader");

			Deserialize (reader);
		}

		/// <summary>
		/// The bitrate of the media data.
		/// </summary>
		public int Bitrate
		{
			get { return this.bitrate; }

			protected internal set
			{
				if (IsInvalidBitrate (value))
					throw new ArgumentOutOfRangeException ("value");

				this.bitrate = value;
			}
		}

		/// <summary>
		/// Gets the complexity of the audio encoding.
		/// </summary>
		public byte Complexity
		{
			get { return this.complexity; }

			protected internal set
			{
				if (IsInvalidComplexity (value))
					throw new ArgumentOutOfRangeException ("value");

				this.complexity = value;
			}
		}

		/// <summary>
		/// Gets the number of audio channels in this source.
		/// </summary>
		public byte Channels
		{
			get { return this.channels; }

			protected internal set
			{
				if (IsInvalidChannels(value))
					throw new ArgumentOutOfRangeException ("value");

				this.channels = value;
			}
		}

		/// <summary>
		/// Gets the frequency of the audio.
		/// </summary>
		public int Frequency
		{
			get { return this.frequency; }

			protected internal set
			{
				if (IsInvalidFrequency (value))
					throw new ArgumentOutOfRangeException ("value");

				this.frequency = value;
			}
		}

		/// <summary>
		/// Gets the frame size for the encoded packets.
		/// </summary>
		public short FrameSize
		{
			get { return this.frameSize; }

			protected internal set
			{
				if (IsInvalidFrameSize (value))
					throw new ArgumentOutOfRangeException ("value");

				this.frameSize = value;
			}
		}

		public static bool IsInvalidFrameSize(short value)
		{
			return value != 0 && (value < 64 || value > 1024 || (value % 64) != 0);
		}

		private short frameSize;
		private byte channels;
		private byte complexity;
		private int bitrate;
		private int frequency;

		protected internal virtual void Serialize (IValueWriter writer)
		{
			writer.WriteInt32 (this.Bitrate);
			writer.WriteByte (this.Channels);
			writer.WriteInt32 (this.Frequency);
			writer.WriteInt16 (this.FrameSize);
			writer.WriteByte (this.Complexity);
		}

		protected internal virtual void Deserialize (IValueReader reader)
		{
			this.Bitrate = reader.ReadInt32();
			this.Channels = reader.ReadByte();
			this.Frequency = reader.ReadInt32();
			this.FrameSize = reader.ReadInt16();
			this.Complexity = reader.ReadByte();
		}

		public static bool IsInvalidFrequency (int value)
		{
			return value < 20000 || value > 96000;
		}

		public static bool IsInvalidChannels (byte value)
		{
			return value <= 0 || value > 2;
		}

		public static bool IsInvalidComplexity (byte value)
		{
			return value < 1 || value > 10;
		}

		public static bool IsInvalidBitrate (int value)
		{
			return value < 0 || value >= 320000;
		}
	}
}