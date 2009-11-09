﻿// Copyright (c) 2009, Eric Maupin
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

namespace Gablarski.Server
{
	public interface IAuthenticationProvider
	{
		/// <summary>
		/// Gets whether or not users can be updated.
		/// </summary>
		bool UpdateSupported { get; }

		/// <summary>
		/// Gets whether a user exists or not.
		/// </summary>
		/// <param name="username">The username to check.</param>
		/// <returns><c>true</c> if the username exists, <c>false</c> otherwise</returns>
		bool UserExists (string username);

		/// <summary>
		/// Gets the type used to identify users.
		/// </summary>
		Type IdentifyingType { get; }

		/// <summary>
		/// Attempts to login a user using the supplied <paramref name="username"/> and <paramref name="password"/>.
		/// </summary>
		/// <param name="username">The username to login with.</param>
		/// <param name="password">The password to login to the username with.</param>
		LoginResult Login (string username, string password);

		/// <summary>
		/// Registers a new user account with <paramref name="username"/> and <paramref name="password"/>.
		/// </summary>
		/// <param name="username">The username to register.</param>
		/// <param name="password">The password to register <paramref name="username"/> with.</param>
		LoginResult Register (string username, string password);
	}
}