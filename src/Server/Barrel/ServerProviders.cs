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
using Gablarski.Server;
using Tempest;

namespace Gablarski.Barrel
{
	public class ServerProviders
	{
		public ServerProviders (IChannelProvider channels, IUserProvider auth, IPermissionsProvider permissions, IEnumerable<IConnectionProvider> cproviders)
			: this (cproviders)
		{
			if (channels == null)
				throw new ArgumentNullException ("channels");
			if (auth == null)
				throw new ArgumentNullException ("auth");
			if (permissions == null)
				throw new ArgumentNullException ("permissions");

			Channels = channels;
			Users = auth;
			Permissions = permissions;
		}

		private ServerProviders (IEnumerable<IConnectionProvider> cproviders)
		{
			if (cproviders == null)
				throw new ArgumentNullException ("cproviders");

			ConnectionProviders = cproviders;
		}

		public IEnumerable<IConnectionProvider> ConnectionProviders
		{
			get;
			private set;
		}

		public IChannelProvider Channels
		{
			get;
			private set;
		}

		public IUserProvider Users
		{
			get;
			private set;
		}

		public IPermissionsProvider Permissions
		{
			get;
			private set;
		}
	}
}