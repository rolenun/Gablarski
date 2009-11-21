// Copyright (c) 2009, Eric Maupin
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
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using Gablarski.OpenAL;

namespace Gablarski.OpenAL
{
	[SuppressUnmanagedCodeSecurity]
	public class Source
		: IDisposable
	{
		internal Source (uint sourceID)
		{
			this.sourceID = sourceID;
		}

		public SourceState State
		{
			get
			{
				int state;
				alGetSourcei (this.sourceID, IntSourceProperty.AL_SOURCE_STATE, out state);
				Gablarski.OpenAL.OpenAL.ErrorCheck ();

				return (SourceState)state;
			}
		}

		public bool IsPlaying
		{
			get { return (this.State == SourceState.Playing); }
		}

		public bool IsPaused
		{
			get { return (this.State == SourceState.Paused); }
		}

		public bool IsStopped
		{
			get { return (this.State == SourceState.Stopped); }
		}

		public int ProcessedBuffers
		{
			get
			{
				int buffers;
				alGetSourcei (this.sourceID, IntSourceProperty.AL_BUFFERS_PROCESSED, out buffers);
				Gablarski.OpenAL.OpenAL.ErrorCheck();

				return buffers;
			}
		}

		public float Pitch
		{
			get { return GetPropertyF (this.sourceID, FloatSourceProperty.AL_PITCH); }
			set { SetPropertyF (this.sourceID, FloatSourceProperty.AL_PITCH, value); }
		}

		/// <summary>
		/// Gets or sets the minimum gain for this source.
		/// </summary>
		public float MinimumGain
		{
			get { return GetPropertyF (this.sourceID, FloatSourceProperty.AL_MIN_GAIN); }
			set { SetPropertyF (this.sourceID, FloatSourceProperty.AL_MIN_GAIN, value); }
		}

		/// <summary>
		/// Gets or sets the source's gain.
		/// </summary>
		public float Gain
		{
			get { return GetPropertyF (this.sourceID, FloatSourceProperty.AL_GAIN); }
			set { SetPropertyF (this.sourceID, FloatSourceProperty.AL_GAIN, value); }
		}

		/// <summary>
		/// Gets or sets the maximum gain for this source.
		/// </summary>
		public float MaximumGain
		{
			get { return GetPropertyF (this.sourceID, FloatSourceProperty.AL_MAX_GAIN); }
			set { SetPropertyF (this.sourceID, FloatSourceProperty.AL_MAX_GAIN, value); }
		}

		public void Queue (SourceBuffer buffer)
		{
			Queue (new [] { buffer });
		}

		public void QueueAndPlay (SourceBuffer buffer)
		{
			this.Queue (buffer);
			this.Play ();
		}

		public void Queue (IEnumerable<SourceBuffer> buffers)
		{
			Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Enqueuing buffers {0} to source {1}", buffers, this.sourceID);
			
			uint[] bufferIDs = buffers.Select (b => b.bufferID).ToArray ();
			alSourceQueueBuffers (this.sourceID, bufferIDs.Length, bufferIDs);
			Gablarski.OpenAL.OpenAL.ErrorCheck ();
		}

		public SourceBuffer[] Dequeue ()
		{
			Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Dequeing all processed buffers for source {0}", this.sourceID);
			return Dequeue (this.ProcessedBuffers);
		}

		public SourceBuffer[] Dequeue (int buffers)
		{
			Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Dequeing {0} buffers for source {1}", buffers, this.sourceID);
			
			uint[] bufferIDs = new uint[buffers];
			alSourceUnqueueBuffers (this.sourceID, buffers, bufferIDs);
			Gablarski.OpenAL.OpenAL.ErrorCheck ();

			SourceBuffer[] dequeued = new SourceBuffer[bufferIDs.Length];
			for (int i = 0; i < bufferIDs.Length; ++i)
			{
				Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Dequeued source buffer {0} for source {1}", bufferIDs[i], this.sourceID);
				dequeued[i] = SourceBuffer.GetBuffer(bufferIDs[i]);
			}

			return dequeued;
		}

		public void Play ()
		{
			PlayCore (true);
		}

		public void Replay ()
		{
			PlayCore (false);
		}

		public void Pause ()
		{
			Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Pausing source {0}", this.sourceID);
			alSourcePause (this.sourceID);
			Gablarski.OpenAL.OpenAL.ErrorCheck ();
		}

		public void Stop ()
		{
			Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Stopping source {0}", this.sourceID);
			alSourceStop (this.sourceID);
			Gablarski.OpenAL.OpenAL.ErrorCheck ();
		}

		private readonly uint sourceID;

		protected void PlayCore (bool check)
		{
			if (check && this.IsPlaying)
				return;

			Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Playing source {0}", this.sourceID);
			alSourcePlay (this.sourceID);
			Gablarski.OpenAL.OpenAL.ErrorCheck ();
		}

		#region IDisposable Members

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		private bool disposed;

		protected void Dispose (bool disposing)
		{
			if (this.disposed)
				return;

			uint[] id = new[] { this.sourceID };
			alDeleteSources (1, id);
			Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Deleting source {0}", id);
			
			this.disposed = true;
		}

		~Source ()
		{
			Dispose (false);
		}

		#endregion

		public static Source Generate ()
		{
			return Generate (1)[0];
		}

		public static Source[] Generate (int count)
		{
			Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Generating {0} sources", count);
			if (count > MaxSources)
			{
				Gablarski.OpenAL.OpenAL.Log.ErrorFormat ("Requested {0} sources which is more than maximum {1}", count, MaxSources);
				throw new InvalidOperationException();
			}

			Source[] sources = new Source[count];

			uint[] sourceIDs = new uint[count];
			alGenSources (count, sourceIDs);
			Gablarski.OpenAL.OpenAL.ErrorCheck ();

			for (int i = 0; i < count; ++i)
			{
				Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Generated source {0}", sourceIDs[i]);
				sources[i] = new Source (sourceIDs[i]);
			}

			return sources;
		}
		// ReSharper disable InconsistentNaming
		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alGetSourcei (uint sourceID, IntSourceProperty property, out int value);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alSourcePlay (uint sourceID);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alSourcePause (uint sourceID);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alSourceStop (uint sourceID);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alSourceQueueBuffers (uint sourceID, int number, uint[] bufferIDs);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alSourceUnqueueBuffers (uint sourceID, int buffers, uint[] buffersDequeued);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alGenSources (int count, uint[] sources);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alDeleteSources (int count, uint[] sources);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alGetSourcef (uint sourceID, FloatSourceProperty property, out float value);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern void alSourcef (uint sourceID, FloatSourceProperty property, float value);
		// ReSharper restore InconsistentNaming

		internal static float GetPropertyF (uint sourceID, FloatSourceProperty property)
		{
			float value;
			alGetSourcef (sourceID, property, out value);
			Gablarski.OpenAL.OpenAL.ErrorCheck ();

			return value;
		}

		internal static void SetPropertyF (uint sourceID, FloatSourceProperty property, float value)
		{
			Gablarski.OpenAL.OpenAL.Log.DebugFormat ("Setting source {0} property {1} to {2}", sourceID, property, value);
			alSourcef (sourceID, property, value);
			Gablarski.OpenAL.OpenAL.ErrorCheck ();
		}

		internal static int MaxSources
		{
			get { return 32; }
		}
	}

	public enum SourceState
	{
		Initial = 0x1011,
		Playing = 0x1012,
		Paused = 0x1013,
		Stopped = 0x1014
	}

	// ReSharper disable InconsistentNaming
	internal enum IntSourceProperty
	{
		AL_SOURCE_STATE = 0x1010,
		AL_BUFFERS_QUEUED = 0x1015,
		AL_BUFFERS_PROCESSED = 0x1016
	}

	internal enum FloatSourceProperty
	{
		AL_PITCH				= 0x1003,
		AL_GAIN					= 0x100A,
		AL_MIN_GAIN				= 0x100D,
		AL_MAX_GAIN				= 0x100E,
		AL_MAX_DISTANCE			= 0x1023,
		AL_ROLLOFF_FACTOR		= 0x1021,
		AL_CONE_OUTER_GAIN		= 0x1022,
		AL_CONE_INNER_ANGLE		= 0x1001,
		AL_CONE_OUTER_ANGLE		= 0x1002,
		AL_REFERENCE_DISTANCE	= 0x1020
	}
	// ReSharper restore InconsistentNaming
}