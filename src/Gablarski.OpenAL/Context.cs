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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Gablarski.OpenAL
{
	public class Context
		: IDisposable
	{
		internal Context (IntPtr handle, PlaybackDevice device)
		{
			this.Handle = handle;
			this.Device = device;
		}

		public PlaybackDevice Device
		{
			get;
			private set;
		}

		public void Activate ()
		{
			OpenAL.DebugFormat ("Activating context for device {0}", this.Device.Name);
			
			alcMakeContextCurrent (this.Handle);
			OpenAL.ErrorCheck (this.Device);
		}

		internal readonly IntPtr Handle;

		#region IDisposable Members
		private bool disposed;
		public void Dispose ()
		{
			this.Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (this.disposed)
				return;

			if (this.Handle != IntPtr.Zero)
				alcDestroyContext (this.Handle);
			
			lock (lck)
			{
				contexts.Remove (this.Handle);
			}
			
			OpenAL.DebugFormat ("Destroying context for {0}", Device.Name);
			
			this.disposed = true;
		}

		~Context()
		{
			this.Dispose (false);
		}

		#endregion

		public static Context CurrentContext
		{
			get
			{
				IntPtr handle = alcGetCurrentContext();

				Context c = null;
				
				if (handle != IntPtr.Zero)
					contexts.TryGetValue (handle, out c);

				return c;
			}
		}

		// ReSharper disable InconsistentNaming
		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private extern static IntPtr alcCreateContext (IntPtr device, IntPtr attrlist);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private extern static void alcMakeContextCurrent (IntPtr context);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private extern static IntPtr alcGetContextsDevice (IntPtr context);

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private extern static IntPtr alcGetCurrentContext ();

		[DllImport ("OpenAL32.dll", CallingConvention = CallingConvention.Cdecl)]
		private extern static void alcDestroyContext (IntPtr context);
		// ReSharper restore InconsistentNaming

		private static Dictionary<IntPtr, Context> contexts;
		private static readonly object lck = new object ();

		public static Context Create (PlaybackDevice device)
		{
			OpenAL.DebugFormat ("Creating context for {0}", device.Name);
			
			Context c = null;
			lock (lck)
			{
				if (contexts == null)
					contexts = new Dictionary<IntPtr, Context> ();

				c = new Context (alcCreateContext (device.Handle, IntPtr.Zero), device);
				OpenAL.ErrorCheck(device);

				if (!contexts.ContainsKey (c.Handle))
					contexts.Add (c.Handle, c);
			}

			OpenAL.ErrorCheck (device);
			return c;
		}

		public static Context CreateAndActivate (PlaybackDevice device)
		{
			Context c = Create (device);
			c.Activate ();
			return c;
		}
	}

	internal enum ContextAttribute
	{
		ALC_FREQUENCY = 0x1007,
		ALC_REFRESH = 0x1008,
		ALC_SYNC = 0x1009,
		ALC_MONO_SOURCES = 0x1010,
		ALC_STEREO_SOURCES = 0x1011
	}
}