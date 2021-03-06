﻿// Copyright (c) 2011, Eric Maupin
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

namespace Gablarski.Server
{
	/// <summary>
	/// Contract for settings providers.
	/// </summary>
	/// <remarks>
	/// This is used for a server's basic settings, but also for various providers allowing
	/// them to specify settings (such as to allow guests or not in the user provider).
	/// </remarks>
	public interface ISettingsProvider
	{
		/// <summary>
		/// Gets whether these settings can be updated externally or not.
		/// </summary>
		bool UpdateSupported { get; }

		/// <summary>
		/// Raised when a setting changes from <see cref="SaveSetting"/> or from outside causes.
		/// </summary>
		event EventHandler<SettingsChangedEventArgs> SettingsChanged;

		/// <summary>
		/// Gets a listing of settings and their values.
		/// </summary>
		IEnumerable<ISetting> GetSettings();

		/// <summary>
		/// Saves a setting.
		/// </summary>
		/// <param name="setting">The setting name and value to save.</param>
		/// <returns><c>true</c> if the save succeeded, <c>false</c> otherwise.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="setting"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException"><paramref name="setting"/>'s name is unrecognized.</exception>
		/// <exception cref="NotSupportedException"><see cref="UpdateSupported"/> is <c>false</c>.</exception>
		bool SaveSetting (ISetting setting);
	}

	public class SettingsChangedEventArgs
		: EventArgs
	{
		public SettingsChangedEventArgs (ISetting setting)
		{
			Setting = setting;
		}

		public ISetting Setting
		{
			get;
			private set;
		}
	}
}