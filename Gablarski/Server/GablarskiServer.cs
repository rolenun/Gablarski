﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Gablarski.Client;
using Lidgren.Network;

namespace Gablarski.Server
{
	public class GablarskiServer
	{
		public GablarskiServer (IAuthProvider authProvider)
		{
			this.auth = authProvider;
		}

		public event EventHandler<ConnectionEventArgs> ClientConnected;
		public event EventHandler<ReasonEventArgs> ClientDisconnected;
		public event EventHandler<ConnectionEventArgs> UserLogin;
		public event EventHandler<VoiceEventArgs> VoiceReceived;

		public bool IsRunning
		{
			get;
			private set;
		}

		public int Port
		{ 
			get { return this.port; }
			set { this.port = value; }
		}

		public int MaxConnections
		{
			get { return this.maxConnections; }
			set { this.maxConnections = value; }
		}
				
		public IEnumerable<UserConnection> Users
		{
			get
			{
				userRWL.EnterReadLock ();
				IEnumerable<UserConnection> users = this.users.Values.ToList ();
				userRWL.ExitReadLock ();

				return users;
			}
		}

		public void Start ()
		{
			if (this.IsRunning)
				throw new InvalidOperationException ("Server already running.");

			this.IsRunning = true;

			this.ServerThread = new Thread (this.ServerRunner);
			this.ServerThread.Name = "Gablarski Server THread";
			this.ServerThread.Start ();
		}

		public void Shutdown ()
		{
			this.IsRunning = false;
			Server.Connections.ForEach (nc => nc.Disconnect ("Server shutting down.", 0.0f));
			this.ServerThread.Join();

			Server.Dispose();
		}

		private IAuthProvider auth;

		private int port = 6112;
		private int maxConnections = 128;
		private NetServer Server;
		private Thread ServerThread;

		private readonly ReaderWriterLockSlim userRWL = new ReaderWriterLockSlim();
		private readonly Dictionary<int, DateTime> pendingLogins = new Dictionary<int, DateTime>();
		private readonly Dictionary<int, UserConnection> users = new Dictionary<int, UserConnection> ();

		private void ServerRunner ()
		{
			NetConfiguration config = new NetConfiguration("Gablarski");
			config.MaxConnections = this.MaxConnections;
			config.Port = this.Port;

			Trace.WriteLine ("Starting listener...");
			
			Server = new NetServer(config);
			Server.Start();

			NetBuffer buffer = Server.CreateBuffer();

			while (this.IsRunning)
			{
				NetConnection sender;
				NetMessageType type;
				while (Server.ReadMessage (buffer, out type, out sender))
				{
					UserConnection connection = users.Values.FirstOrDefault (uc => uc.Connection == sender) ?? new UserConnection (Server, sender);
					ConnectionEventArgs e = new ConnectionEventArgs (connection, buffer);

					switch (type)
					{
						case NetMessageType.StatusChanged:
							if (sender.Status == NetConnectionStatus.Connected)
							{
								e.UserConnection.AuthHash = GenerateHash ();
								this.OnClientConnected (e);
							}
							else if (sender.Status == NetConnectionStatus.Disconnected)
								this.OnClientDisconnected (new ReasonEventArgs(e, buffer.ReadString()));

							break;

						case NetMessageType.Data:
							byte sanity = buffer.ReadByte();
							if (sanity != Message<ServerMessages>.FirstByte)
								continue;

							ClientMessages messageType = (ClientMessages)buffer.ReadVariableUInt32();

							int hash = e.Buffer.ReadVariableInt32 ();
							e.UserConnection.AuthHash = hash;

							userRWL.EnterReadLock ();
							if (!this.users.ContainsKey (hash) && !this.pendingLogins.ContainsKey (hash))
							{
								userRWL.ExitReadLock ();
								this.ClientDisconnect (new ReasonEventArgs (e, "Auth failure"), false);
							}
							userRWL.ExitReadLock ();

							switch (messageType)
							{
								case ClientMessages.Login:
									this.OnClientLogin (e);
									break;

								case ClientMessages.Disconnect:
									this.ClientDisconnect (new ReasonEventArgs (e, "Client requested"), true);
									break;

								case ClientMessages.VoiceData:
									Trace.WriteLine ("Received voice data from: " + e.UserConnection.User.Username);

									int voiceLen = buffer.ReadVariableInt32();
									if (voiceLen <= 0)
										continue;

									ServerMessage msg = new ServerMessage (ServerMessages.VoiceData, this.users.Values/*.Where (uc => uc != e.UserConnection)*/);
									var msgbuffer = msg.GetBuffer ();
									msgbuffer.WriteVariableUInt32 (e.UserConnection.User.ID);
									msgbuffer.WriteVariableInt32 (voiceLen);
									msgbuffer.Write (buffer.ReadBytes (voiceLen));
									msg.Send (this.Server, NetChannel.Unreliable);

									break;
							}

							break;
					}
				}

				Thread.Sleep (1);
			}
		}

		private void ClientDisconnect (ReasonEventArgs e, bool commanded)
		{
			userRWL.EnterUpgradeableReadLock ();
			if (this.pendingLogins.ContainsKey (e.UserConnection.AuthHash) || this.users.ContainsKey (e.UserConnection.AuthHash))
			{
				userRWL.EnterWriteLock ();
				this.pendingLogins.Remove (e.UserConnection.AuthHash);
				this.users.Remove (e.UserConnection.AuthHash);
				userRWL.ExitWriteLock ();
			}
			userRWL.ExitUpgradeableReadLock ();

			//if (!commanded)
				e.Connection.Disconnect (String.Empty, 0.0f);

			if (e.UserConnection.User != null)
				Trace.WriteLine (e.UserConnection.User.Username + " disconnected: " + e.Reason);
			else
				Trace.WriteLine ("Unknown disconnected: " + e.Reason);

			if (e.UserConnection.User != null)
			{
				ServerMessage msg = new ServerMessage (ServerMessages.UserDisconnected, this.users.Values.Where (uc => uc.Connection.Status == NetConnectionStatus.Connected));
				e.UserConnection.User.Encode (msg.GetBuffer ());
				msg.Send (this.Server, NetChannel.ReliableUnordered);
			}

			var disconnected = this.ClientDisconnected;
			if (disconnected != null)
				disconnected (this, e);
		}

		protected virtual void OnClientDisconnected (ReasonEventArgs e)
		{
			this.ClientDisconnect (e, false);
		}

		protected virtual void OnClientConnected (ConnectionEventArgs e)
		{
			userRWL.EnterUpgradeableReadLock ();
			if (this.pendingLogins.ContainsKey (e.UserConnection.AuthHash))
			{
				userRWL.ExitUpgradeableReadLock ();
				return;
			}

			userRWL.EnterWriteLock();
			this.pendingLogins.Add (e.UserConnection.AuthHash, DateTime.Now);
			userRWL.ExitWriteLock();
			userRWL.ExitUpgradeableReadLock ();

			ServerMessage msg = new ServerMessage (ServerMessages.Connected, e.UserConnection);
			msg.Send (this.Server, NetChannel.ReliableInOrder1);

			var connected = this.ClientConnected;
			if (connected != null)
				connected (this, e);
		}

		protected virtual void OnClientLogin (ConnectionEventArgs e)
		{
			string nickname = e.Buffer.ReadString();
			string username = e.Buffer.ReadString();
			string password = e.Buffer.ReadString();

			Trace.WriteLine ("Login attempt: " + nickname);

			LoginResult result = null;

			if (String.IsNullOrEmpty (username))
			{
				if (this.auth.CheckUserExists (nickname))
				{
					this.DisconnectUser (e.UserConnection, "Registered user owns this login already.", e.Connection);
					return;
				}

				userRWL.EnterUpgradeableReadLock();

				if (this.users.Values.Where (uc => uc.User.Nickname == nickname).Any())
				{
					this.DisconnectUser (e.UserConnection, "User already logged in.", e.Connection);
					userRWL.ExitUpgradeableReadLock ();
					return;
				}

				userRWL.ExitUpgradeableReadLock();

				result = this.auth.Login (nickname);
			}
			else
			{
				if (!this.auth.CheckUserExists(nickname))
				{
					this.DisconnectUser (e.UserConnection, "User does not exist.", e.Connection);
					return;
				}

				userRWL.EnterUpgradeableReadLock();

				if (this.users.Values.Where (uc => uc.User.Username == username).Any())
				{
					this.DisconnectUser (e.UserConnection, "User already logged in.", e.Connection);
					userRWL.ExitUpgradeableReadLock ();
					return;
				}

				userRWL.ExitUpgradeableReadLock ();

				result = this.auth.Login (username, password);
			}
			
			if (!result.Succeeded)
			{
				this.DisconnectUser (e.UserConnection, result.FailureReason, e.Connection);
				return;
			}

			Trace.WriteLine (result.User.Username + " logged in.");

			e.UserConnection.User = result.User;

			userRWL.EnterWriteLock();
			this.pendingLogins.Remove (e.UserConnection.AuthHash);
			this.users.Add (e.UserConnection.AuthHash, e.UserConnection);
			userRWL.ExitWriteLock();

			ServerMessage msg = new ServerMessage (ServerMessages.LoggedIn, e.UserConnection);
			e.UserConnection.User.Encode (msg.GetBuffer ());
			msg.Send (this.Server, NetChannel.ReliableInOrder1);

			msg = new ServerMessage (ServerMessages.UserConnected, this.users.Values.Where (uc => uc.Connection.Status == NetConnectionStatus.Connected));
			e.UserConnection.User.Encode (msg.GetBuffer ());
			msg.Send (this.Server, NetChannel.ReliableUnordered);

			var login = this.UserLogin;
			if (login != null)
				login (this, e);
		}

		protected void DisconnectUser (UserConnection userc, string reason, NetConnection netc)
		{
			netc.Disconnect (reason, 0);

			userRWL.EnterWriteLock();
			this.pendingLogins.Remove (userc.AuthHash);
			this.users.Remove (userc.AuthHash);
			userRWL.ExitWriteLock();
		}

		static int GenerateHash()
		{
			return DateTime.Now.Millisecond + DateTime.Now.Second + 42;
		}
	}
}
