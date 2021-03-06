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
using Tempest;

namespace Gablarski.Messages
{
	public class UserListMessage
		: GablarskiMessage
	{
		public UserListMessage()
			: base (GablarskiMessageType.UserList)
		{
		}

		public UserListMessage (IEnumerable<IUser> users)
			: this()
		{
			Users = users;
		}

		public IEnumerable<IUser> Users
		{
			get; set;
		}

		public override void ReadPayload (ISerializationContext context, IValueReader reader)
		{
			int numUsers = reader.ReadInt32();
			IUser[] users = new IUser[numUsers];
			for (int i = 0; i < users.Length; ++i)
				users[i] = new User (reader.ReadInt32(), reader.ReadString());

			Users = users;
		}

		public override void WritePayload (ISerializationContext context, IValueWriter writer)
		{
			var users = Users.ToList();
			writer.WriteInt32 (users.Count);
			for (int i = 0; i < users.Count; ++i)
			{
				var u = users[i];
				writer.WriteInt32 (u.UserId);
				writer.WriteString (u.Username);
			}
		}
	}

	internal class User
		: IUser
	{
		public int UserId
		{
			get;
			private set;
		}

		public string Username
		{
			get;
			private set;
		}

		public User (int userId, string username)
		{
			if (username == null)
				throw new ArgumentNullException ("username");

			UserId = userId;
			Username = username;
		}
	}
}