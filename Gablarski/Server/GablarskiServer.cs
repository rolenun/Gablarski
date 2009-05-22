﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gablarski.Client;
using Gablarski.Messages;
using System.Diagnostics;
using Gablarski.Media.Sources;

namespace Gablarski.Server
{
	public partial class GablarskiServer
	{
		public static readonly Version MinimumAPIVersion = new Version (0,2,0,0);

		public GablarskiServer (ServerInfo serverInfo, IUserProvider userProvider, IPermissionsProvider permissionProvider)
			: this()
		{
			this.serverInfo = serverInfo;
			this.userProvider = userProvider;
		}

		#region Public Methods
		public IEnumerable<IConnectionProvider> ConnectionProviders
		{
			get
			{
				lock (availableConnectionLock)
				{
					return this.availableConnections.ToArray ();
				}
			}

			set
			{
				this.availableConnections = value.ToList();
			}
		}

		public void AddConnectionProvider (IConnectionProvider connection)
		{
			Trace.WriteLine ("[Server] " + connection.GetType().Name + " added.");

			// MUST provide a gaurantee of persona
			connection.ConnectionMade += OnConnectionMade;
			connection.StartListening ();

			lock (availableConnectionLock)
			{
				this.availableConnections.Add (connection);
			}
		}

		public void RemoveConnectionProvider (IConnectionProvider connection)
		{
			Trace.WriteLine ("[Server] " + connection.GetType ().Name + " removed.");

			connection.StopListening ();
			connection.ConnectionMade -= this.OnConnectionMade;

			lock (availableConnectionLock)
			{
				this.availableConnections.Remove (connection);
			}
		}

		public void Disconnect (IConnection connection, string reason)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");

			connection.Disconnect ();
			connection.MessageReceived -= this.OnMessageReceived;
		}

		public void Shutdown ()
		{
			lock (this.availableConnectionLock)
			{
				for (int i = 0; i < this.availableConnections.Count;)
				{
					var provider = this.availableConnections[i];

					provider.StopListening ();
					provider.ConnectionMade -= OnConnectionMade;
					this.RemoveConnectionProvider (provider);
				}
			}
		}
		#endregion

		private readonly ServerInfo serverInfo = new ServerInfo();

		private readonly object availableConnectionLock = new object();
		private List<IConnectionProvider> availableConnections = new List<IConnectionProvider> ();

		private readonly IPermissionsProvider permissionProvider;
		private readonly IUserProvider userProvider;

		private object sourceLock = new object ();
		private readonly Dictionary<IConnection, List<IMediaSource>> sources = new Dictionary<IConnection, List<IMediaSource>> ();

		private readonly ConnectionCollection connections = new ConnectionCollection();

		private IEnumerable<MediaSourceInfo> GetSourceInfoList ()
		{
			IEnumerable<MediaSourceInfo> agrSources = Enumerable.Empty<MediaSourceInfo> ();
			lock (this.sourceLock)
			{
				foreach (var kvp in this.sources)
				{
					IConnection connection = kvp.Key;
					agrSources = agrSources.Concat (
							kvp.Value.Select (s => new MediaSourceInfo (s) { PlayerId = this.connections.GetPlayerId (connection) }));
				}

				agrSources = agrSources.ToList ();
			}

			return agrSources;
		}

		protected virtual void OnMessageReceived (object sender, MessageReceivedEventArgs e)
		{
			var msg = (e.Message as ClientMessage);
			if (msg == null)
			{
				Disconnect (e.Connection, "Invalid message.");
				return;
			}

			Trace.WriteLine ("[Server] Message Received: " + msg.MessageType);

			this.Handlers[msg.MessageType] (e);
		}

		protected void AudioDataReceived (MessageReceivedEventArgs e)
		{
			var msg = (SendAudioDataMessage) e.Message;

			this.connections.Send (new AudioDataReceivedMessage (msg.SourceId, msg.Data), (IConnection c) => c != e.Connection);
		}

		protected void UserLoginAttempt (MessageReceivedEventArgs e)
		{
			var login = (LoginMessage)e.Message;

			LoginResult result = this.userProvider.Login (login.Username, login.Password);
			PlayerInfo info = null;

			if (result.Succeeded)
			{
				info = new PlayerInfo (login.Nickname, result.PlayerId);

				if (!this.connections.PlayerLoggedIn (login.Nickname))
					this.connections.Add (e.Connection, info);
				else
				{
					result.ResultState = LoginResultState.FailedNicknameInUse;
				}
			}
			
			var msg = new LoginResultMessage (result, info);

			if (result.Succeeded)
				this.connections.Send (msg);
			else
				e.Connection.Send (msg);

			Trace.WriteLine ("[Server]" + login.Username + " Login: " + result.ResultState);
		}

		protected void ClientDisconnected (MessageReceivedEventArgs e)
		{
			OnClientDisconnected (this, new ConnectionEventArgs (e.Connection));
		}

		private void OnClientDisconnected (object sender, ConnectionEventArgs e)
		{
			Trace.WriteLine ("[Server] Client disconnected");

			e.Connection.Disconnect();

			long playerId;
			if (this.connections.Remove (e.Connection, out playerId))
				this.connections.Send (new PlayerDisconnectedMessage (playerId));

			lock (sourceLock)
			{
				this.sources.Remove (e.Connection);
			}
		}
		
		private void OnConnectionMade (object sender, ConnectionEventArgs e)
		{
			Trace.WriteLine ("[Server] Connection Made");

			this.connections.Add (e.Connection);

			e.Connection.MessageReceived += this.OnMessageReceived;
			e.Connection.Disconnected += this.OnClientDisconnected;
		}
	}
}