﻿// Copyright (c) 2010, Eric Maupin
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cadenza.Collections;
using Gablarski.Audio;

namespace Gablarski.Server
{
	public class ServerSourceManager
		: ISourceManager
	{
		private readonly IServerContext context;

		public ServerSourceManager (IServerContext context)
		{
			this.context = context;
		}

		public IEnumerator<AudioSource> GetEnumerator()
		{
			lock (SyncRoot)
			{
				return sources.Values.ToList().GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public AudioSource this [int key]
		{
			get
			{
				AudioSource source;
				lock (SyncRoot)
					sources.TryGetValue (key, out source);

				return source;
			}
		}

		public IEnumerable<AudioSource> this [UserInfo user]
		{
			get
			{
				lock (SyncRoot)
					return ownedSources[user];
			}
		}

		public void Add (AudioSource source)
		{
			if (source == null)
				throw new ArgumentNullException ("source");

			UserInfo owner = context.Users[source.OwnerId];
			if (owner == null)
				return;

			lock (SyncRoot)
			{
				ownedSources.Add (owner, source);
				sources.Add (source.Id, source);
			}
		}

		public void Remove (AudioSource source)
		{
			if (source == null)
				throw new ArgumentNullException ("source");
			
			UserInfo owner = context.Users[source.OwnerId];

			lock (SyncRoot)
			{
				if (owner != null)
					ownedSources.Remove (owner, source);

				sources.Remove (source.Id);
			}
		}

		public void Remove (UserInfo user)
		{
			if (user == null)
				throw new ArgumentNullException ("user");

			lock (SyncRoot)
			{
				foreach (AudioSource source in ownedSources[user])
					sources.Remove (source.Id);

				ownedSources.Remove (user);
			}
		}

		protected readonly object SyncRoot = new object();
		private readonly MutableLookup<UserInfo, AudioSource> ownedSources = new MutableLookup<UserInfo, AudioSource>();
		private readonly Dictionary<int, AudioSource> sources = new Dictionary<int, AudioSource>();
	}
}