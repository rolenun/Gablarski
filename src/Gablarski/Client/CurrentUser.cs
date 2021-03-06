// Copyright (c) 2011, Eric Maupin
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
using Gablarski.Messages;
using Cadenza;
using Tempest;

namespace Gablarski.Client
{
	public class CurrentUser
		: UserInfo, ICurrentUserHandler
	{
		public CurrentUser (IGablarskiClientContext context)
		{
			if (context == null)
				throw new ArgumentNullException ("context");

			this.context = context;

			this.context.RegisterMessageHandler<UserChangedChannelMessage> (OnUserChangedChannelMessage);
			this.context.RegisterMessageHandler<UserUpdatedMessage> (OnUserUpdatedMessage);
			this.context.RegisterMessageHandler<LoginResultMessage> (OnLoginResultMessage);
			this.context.RegisterMessageHandler<JoinResultMessage> (OnJoinResultMessage);
			this.context.RegisterMessageHandler<PermissionsMessage> (OnPermissionsMessage);
			this.context.RegisterMessageHandler<UserKickedMessage> (OnUserKickedMessage);
			this.context.RegisterMessageHandler<RegisterResultMessage> (OnRegisterResultMessage);
		}

		internal CurrentUser (IGablarskiClientContext context, int userId, string nickname, int currentChannelId)
			: this (context)
		{
			if (userId == 0)
				throw new ArgumentException ("userId");
			if (nickname.IsNullOrWhitespace())
				throw new ArgumentNullException ("nickname", "nickname is null or empty.");
			if (currentChannelId < 0)
				throw new ArgumentOutOfRangeException ("currentChannelId");

			this.UserId = userId;
			this.Nickname = nickname;
			this.CurrentChannelId = currentChannelId;
		}

		#region Events
		/// <summary>
		/// A login result has been received.
		/// </summary>
		public event EventHandler<ReceivedLoginResultEventArgs> ReceivedLoginResult;

		public event EventHandler<ReceivedJoinResultEventArgs> ReceivedJoinResult;

		public event EventHandler<ReceivedRegisterResultEventArgs> ReceivedRegisterResult;

		public event EventHandler PermissionsChanged;

		public event EventHandler Kicked;
		#endregion

		public IEnumerable<Permission> Permissions
		{
			get
			{
				if (this.permissions == null)
					return Enumerable.Empty<Permission>();

				lock (permissionLock)
				{
					if (this.permissions == null)
						return Enumerable.Empty<Permission>();

					return this.permissions.ToList();
				}
			}
		}

		public void Login (string username, string password)
		{
			if (username.IsNullOrWhitespace())
				throw new ArgumentNullException("username");
			if (password == null)
				throw new ArgumentNullException("password");

			this.context.Connection.SendAsync (new LoginMessage
			{
				Username = username,
				Password = password
			});
		}
		
		public void Join (string nickname, string serverPassword)
		{
			Join (nickname, null, serverPassword);
		}

		public void Join (string nickname, string phonetic, string serverPassword)
		{
			if (nickname.IsNullOrWhitespace())
				throw new ArgumentNullException ("nickname");

			if (phonetic.IsNullOrWhitespace())
				phonetic = nickname;

			this.context.Connection.SendAsync (new JoinMessage (nickname, phonetic, serverPassword));
		}

		public void Register (string username, string password)
		{
			this.context.Connection.SendAsync (new RegisterMessage (username, password));
		}

		/// <summary>
		/// Sets the current user's comment.
		/// </summary>
		/// <param name="comment">The comment to set. <c>null</c> is valid to clear.</param>
		public void SetComment (string comment)
		{
			if (comment == this.Comment)
				return;

			Comment = comment;
			this.context.Connection.SendAsync (new SetCommentMessage (comment));
		}

		/// <summary>
		/// Sets the current user's status.
		/// </summary>
		/// <param name="status">The status to set.</param>
		public void SetStatus (UserStatus status)
		{
			if (status == this.Status)
				return;

			Status = status;
			this.context.Connection.SendAsync (new SetStatusMessage (status));
		}

		/// <summary>
		/// Mutes all playback and sets the user's status accordingly.
		/// </summary>
		public void MutePlayback()
		{
			context.Audio.MutePlayback();

			SetStatus (Status | UserStatus.MutedSound);
		}

		/// <summary>
		/// Unmutes all playback and sets the user's status accordingly.
		/// </summary>
		public void UnmutePlayback()
		{
			context.Audio.UnmutePlayback();

			SetStatus (Status ^ UserStatus.MutedSound);
		}

		/// <summary>
		/// Mutes all capture and sets the user's status accordingly.
		/// </summary>
		public void MuteCapture()
		{
			context.Audio.MuteCapture();

			SetStatus (Status | UserStatus.MutedMicrophone);
		}

		/// <summary>
		/// Unmutes all capture and sets the user's status accordingly.
		/// </summary>
		public void UnmuteCapture()
		{
			context.Audio.UnmuteCapture();

			SetStatus (Status ^ UserStatus.MutedMicrophone);
		}

		private readonly IGablarskiClientContext context;
		private readonly object permissionLock = new object();
		private IEnumerable<Permission> permissions;

		protected virtual void OnLoginResult (ReceivedLoginResultEventArgs e)
		{
			var result = this.ReceivedLoginResult;
			if (result != null)
				result (this, e);
		}

		protected virtual void OnPermissionsChanged (EventArgs e)
		{
			var changed = this.PermissionsChanged;
			if (changed != null)
				changed (this, e);
		}

		protected virtual void OnJoinResult (ReceivedJoinResultEventArgs e)
		{
			var join = this.ReceivedJoinResult;
			if (join != null)
				join (this, e);
		}

		protected virtual void OnRegisterResult (ReceivedRegisterResultEventArgs e)
		{
			var result = this.ReceivedRegisterResult;
			if (result != null)
				result (this, e);
		}

		protected virtual void OnKicked (EventArgs e)
		{
			var kicked = this.Kicked;
			if (kicked != null)
				kicked (this, e);
		}

		internal void OnUserKickedMessage (MessageEventArgs<UserKickedMessage> e)
		{
			var msg = (UserKickedMessage)e.Message;

			if (msg.UserId != UserId)
				return;

			OnKicked (EventArgs.Empty);
		}

		internal void OnUserChangedChannelMessage (MessageEventArgs<UserChangedChannelMessage> e)
		{
			var msg = (UserChangedChannelMessage) e.Message;

			var channel = this.context.Channels[msg.ChangeInfo.TargetChannelId];
			if (channel == null)
				return;

			var user = this.context.Users[msg.ChangeInfo.TargetUserId];
			if (user == null || !user.Equals (this))
				return;

			this.CurrentChannelId = msg.ChangeInfo.TargetChannelId;
		}

		internal void OnUserUpdatedMessage (MessageEventArgs<UserUpdatedMessage> e)
		{
			var msg = (UserUpdatedMessage) e.Message;

			if (!msg.User.Equals (this))
				return;

			this.Comment = msg.User.Comment;
			this.Status = msg.User.Status;
		}

		internal void OnLoginResultMessage (MessageEventArgs<LoginResultMessage> e)
		{
		    var msg = (LoginResultMessage)e.Message;
			if (msg.Result.ResultState == LoginResultState.Success)
				this.UserId = msg.Result.UserId;

			var args = new ReceivedLoginResultEventArgs (msg.Result);
			OnLoginResult (args);
		}

		internal void OnJoinResultMessage (MessageEventArgs<JoinResultMessage> e)
		{
			var msg = (JoinResultMessage)e.Message;
			if (msg.Result == LoginResultState.Success)
			{
				this.UserId = msg.UserInfo.UserId;
				this.Username = msg.UserInfo.Username;
				this.Nickname = msg.UserInfo.Nickname;
				this.CurrentChannelId = msg.UserInfo.CurrentChannelId;
			}

			var args = new ReceivedJoinResultEventArgs(msg.Result);
			OnJoinResult (args);
		}

		internal void OnRegisterResultMessage (MessageEventArgs<RegisterResultMessage> e)
		{
			OnRegisterResult (new ReceivedRegisterResultEventArgs (((RegisterResultMessage)e.Message).Result));
		}

		internal void OnPermissionsMessage (MessageEventArgs<PermissionsMessage> e)
		{
			var msg = (PermissionsMessage)e.Message;
			if (msg.OwnerId != this.UserId)
				return;

			this.permissions = msg.Permissions;
			OnPermissionsChanged (EventArgs.Empty);
		}
	}
}