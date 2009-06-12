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
		public static readonly Version MinimumApiVersion = new Version (0,6,0,0);

		/// <summary>
		/// Initializes a new <c>GablarskiServer</c> instance.
		/// </summary>
		/// <param name="settings">The settings for the server, providing name, description, etc.</param>
		/// <param name="userProvider">The user authentication provider for the server to use.</param>
		/// <param name="permissionProvider">The user permissions provider for the server to use.</param>
		/// <param name="channelProvider">The channel provider for the server to use.</param>
		public GablarskiServer (ServerSettings settings, IUserProvider userProvider, IPermissionsProvider permissionProvider, IChannelProvider channelProvider)
			: this()
		{
			this.settings = settings;
			this.userProvider = userProvider;
			this.permissionProvider = permissionProvider;
			
			this.channelProvider = channelProvider;
			this.channelProvider.ChannelsUpdatedExternally += OnChannelsUpdatedExternally;
			this.UpdateChannels (false);
		}

		/// <summary>
		/// Initializes a new <c>GablarskiServer</c> instance.
		/// </summary>
		/// <param name="settings">The settings for the server, providing name, description, etc.</param>
		/// <param name="provider">The backend provider for the server to use.</param>
		public GablarskiServer (ServerSettings settings, IBackendProvider provider)
			: this()
		{
			this.settings = settings;

			this.backendProvider = provider;
			this.backendProvider.ChannelsUpdatedExternally += OnChannelsUpdatedExternally;
			this.UpdateChannels (false);
		}

		#region Public Methods
		/// <summary>
		/// Gets or sets the list of <c>IConnectionProvider</c>'s for the server to use.
		/// </summary>
		public IEnumerable<IConnectionProvider> ConnectionProviders
		{
			get
			{
				lock (this.availableConnections)
				{
					return this.availableConnections.ToArray ();
				}
			}

			set
			{
				lock (this.availableConnections)
				{
					foreach (var provider in this.availableConnections)
						RemoveConnectionProvider (provider, false);

					this.availableConnections.Clear ();

					foreach (var provider in value)
						this.AddConnectionProvider (provider);
				}
			}
		}

		/// <summary>
		/// Starts the server.
		/// </summary>
		public void Start ()
		{
			this.running = true;
			this.messageRunnerThread.Start ();
		}

		/// <summary>
		/// Adds and starts an <c>IConnectionProvider</c>.
		/// </summary>
		/// <param name="provider">The <c>IConnectionProvider</c> to add and start listening.</param>
		public void AddConnectionProvider (IConnectionProvider provider)
		{
			Trace.WriteLine ("[Server] " + provider.GetType().Name + " added.");

			// MUST provide a gaurantee of persona
			lock (this.availableConnections)
			{
				provider.ConnectionMade += OnConnectionMade;
				provider.StartListening ();
		
				this.availableConnections.Add (provider);
			}
		}

		/// <summary>
		/// Stops and removes an <c>IConnectionProvider</c>.
		/// </summary>
		/// <param name="connection"></param>
		public void RemoveConnectionProvider (IConnectionProvider connection)
		{
			RemoveConnectionProvider (connection, true);
		}

		/// <summary>
		/// Disconnections an <c>IConnection</c>.
		/// </summary>
		/// <param name="player">The player to disconnect.</param>
		public void Disconnect (UserInfo player)
		{
			if (player == null)
				throw new ArgumentNullException ("player");

			var playerConnection = this.connections[player];
			if (playerConnection != null)
				this.Disconnect (playerConnection);
		}

		/// <summary>
		/// Removes all connection providers, disconnects all users and shuts the server down.
		/// </summary>
		public void Shutdown ()
		{
			this.running = false;
			
			lock (this.availableConnections)
			{
				for (int i = 0; i < this.availableConnections.Count;)
				{
					var provider = this.availableConnections[i];

					provider.ConnectionMade -= OnConnectionMade;
					this.RemoveConnectionProvider (provider);
				}
			}

			this.messageRunnerThread.Join ();

			while (this.connections.ConnectionCount > 0)
				this.Disconnect (this.connections[0].Key);
		}
		#endregion

		private readonly ServerSettings settings;

		private List<IConnectionProvider> availableConnections = new List<IConnectionProvider> ();

		private readonly IBackendProvider backendProvider;

		private readonly IChannelProvider channelProvider;
		private readonly IPermissionsProvider permissionProvider;
		private readonly IUserProvider userProvider;
		private volatile bool running = true;

		protected IBackendProvider BackendProvider
		{
			get { return backendProvider; }
		}

		protected IChannelProvider ChannelProvider
		{
			get { return (backendProvider ?? channelProvider); }
		}

		protected IPermissionsProvider PermissionProvider
		{
			get { return (backendProvider ?? permissionProvider); }
		}

		protected IUserProvider UserProvider
		{
			get { return (backendProvider ?? userProvider); }
		}

		private readonly object sourceLock = new object ();
		private readonly Dictionary<IConnection, List<IMediaSource>> sources = new Dictionary<IConnection, List<IMediaSource>> ();

		private readonly ConnectionCollection connections = new ConnectionCollection();

		private readonly object channelLock = new object ();
		private Channel defaultChannel;
		private Dictionary<long, Channel> channels;

		private void Disconnect (IConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException ("connection");

			connection.Disconnect ();
			connection.MessageReceived -= this.OnMessageReceived;
			connection.Disconnected -= this.OnClientDisconnected;
			this.connections.Remove (connection);
		}

		private void RemoveConnectionProvider (IConnectionProvider provider, bool listRemove)
		{
			Trace.WriteLine ("[Server] " + provider.GetType ().Name + " removed.");

			provider.StopListening ();
			provider.ConnectionMade -= this.OnConnectionMade;

			if (listRemove)
			{
				lock (this.availableConnections)
				{
					this.availableConnections.Remove (provider);
				}
			}
		}

		private void UpdateChannels (bool sendUpdate)
		{
			this.channels = this.ChannelProvider.GetChannels ().ToDictionary (c => c.ChannelId);
			this.defaultChannel = this.ChannelProvider.DefaultChannel;

			if (sendUpdate)
				this.connections.Send (new ChannelListMessage (this.channels.Values));
		}

		private void OnChannelsUpdatedExternally (object sender, EventArgs e)
		{
			lock (channelLock)
			{
				this.UpdateChannels (true);
			}
		}

		private IEnumerable<MediaSourceInfo> GetSourceInfoList ()
		{
			IEnumerable<MediaSourceInfo> agrSources = Enumerable.Empty<MediaSourceInfo> ();
			lock (this.sourceLock)
			{
				foreach (var kvp in this.sources)
				{
					IConnection connection = kvp.Key;
					agrSources = agrSources.Concat (
							kvp.Value.Select (s => new MediaSourceInfo (s) { PlayerId = this.connections[connection].UserId }));
				}

				agrSources = agrSources.ToList ();
			}

			return agrSources;
		}

		protected bool GetPermission (PermissionName name, long channelId, long playerId)
		{
			if (this.BackendProvider != null)
				return this.BackendProvider.GetPermissions (channelId, playerId).GetPermission (name);
			else
				return this.PermissionProvider.GetPermissions (playerId).GetPermission (name);
		}

		protected bool GetPermission (PermissionName name, long channelId, IConnection connection)
		{
			return GetPermission (name, channelId, this.connections[connection].UserId);
		}

		protected bool GetPermission (PermissionName name, IConnection connection)
		{
			var player = this.connections[connection];

			return GetPermission (name, (player != null) ? player.CurrentChannelId : 0, (player != null) ? player.UserId : 0);
		}

		protected bool GetPermission (PermissionName name, UserInfo player)
		{
			return GetPermission (name, player.CurrentChannelId, player.UserId);
		}

		protected void ClientDisconnected (MessageReceivedEventArgs e)
		{
			OnClientDisconnected (this, new ConnectionEventArgs (e.Connection));
		}

		private void OnClientDisconnected (object sender, ConnectionEventArgs e)
		{
			Trace.WriteLine ("[Server] Client disconnected");

			e.Connection.Disconnected -= this.OnClientDisconnected;
			e.Connection.Disconnect();

			long playerId;
			if (this.connections.Remove (e.Connection, out playerId))
				this.connections.Send (new UserDisconnectedMessage (playerId));

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