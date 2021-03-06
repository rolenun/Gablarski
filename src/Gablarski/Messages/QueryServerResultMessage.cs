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
using System.Collections.Generic;
using System.Linq;
using Tempest;

namespace Gablarski.Messages
{
	public class QueryServerResultMessage
		: GablarskiMessage
	{
		public QueryServerResultMessage()
			: base (GablarskiMessageType.QueryServerResult)
		{
		}

		public override bool MustBeReliable
		{
			get { return false; }
		}

		public override bool PreferReliable
		{
			get { return false; }
		}

		public IEnumerable<IUserInfo> Users
		{
			get { return this.users.Cast<IUserInfo>(); }
			set { this.users = value.Select (u => new UserInfo (u)).ToList(); }
		}

		public IEnumerable<IChannelInfo> Channels
		{
			get { return this.channels.Cast<IChannelInfo>(); }
			set { this.channels = value.Select (c => new ChannelInfo (c)).ToList(); }
		}

		public ServerInfo ServerInfo
		{
			get; set;
		}

		public override void WritePayload (ISerializationContext context, IValueWriter writer)
		{
			if (this.Users != null)
			{
				writer.WriteInt32 (this.users.Count);
				foreach (var u in this.users)
					u.Serialize (context, writer);
			}
			else
				writer.WriteInt32 (0);

			if (this.Channels != null)
			{
				writer.WriteInt32 (this.Channels.Count());
				foreach (var c in this.channels)
					c.Serialize (context, writer);
			}
			else
				writer.WriteInt32 (0);

			this.ServerInfo.Serialize (context, writer);
		}

		public override void ReadPayload (ISerializationContext context, IValueReader reader)
		{
			int nusers = reader.ReadInt32();
			this.users = new List<UserInfo> (nusers);
			for (int i = 0; i < nusers; ++i)
				this.users.Add (new UserInfo (context, reader));
			
			int nchannels = reader.ReadInt32();
			this.channels = new List<ChannelInfo> (nchannels);
			for (int i = 0; i < nchannels; ++i)
				this.channels.Add (new ChannelInfo (context, reader));

			this.ServerInfo = new ServerInfo (context, reader);
		}

		private List<ChannelInfo> channels;
		private List<UserInfo> users;
	}
}