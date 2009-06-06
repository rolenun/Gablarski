﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Gablarski.Server;
using Gablarski.Client;
using Gablarski.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gablarski.Tests
{
	[TestClass]
	public class ServerTests
	{
		[TestInitialize]
		public void ServerTestInitialize ()
		{
			this.server = new GablarskiServer (new ServerSettings { Name = "Test Server", Description = "Test Server" }, new GuestUserProvider (), new GuestPermissionProvider(), new LobbyChannelProvider());
			this.server.AddConnectionProvider (this.provider = new MockConnectionProvider ());
			this.server.Start ();
		}

		[TestCleanup]
		public void ServerTestCleanup ()
		{
			this.server.Shutdown ();
			this.server = null;
			this.provider = null;
		}

		private GablarskiServer server;
		private MockConnectionProvider provider;

		[TestMethod]
		public void TestOldVersionReject ()
		{
			MockServerConnection connection = provider.EstablishConnection ();

			connection.Client.Send (new ConnectMessage (new Version (0,0,0,1)));

			var msg = (connection.Client.DequeueMessage () as ConnectionRejectedMessage);
			Assert.IsNotNull (msg);
			Assert.AreEqual (ConnectionRejectedReason.IncompatibleVersion, msg.Reason);
		}

		[TestMethod]
		public void TestRequestChannelList ()
		{
			MockServerConnection connection = provider.EstablishConnection ();
			connection.Client.Send (new RequestChannelListMessage ());

			var msg = (connection.Client.DequeueMessage () as ChannelListMessage);
			Assert.IsNotNull (msg);
			Assert.IsTrue (msg.Channels.Count () > 0);
		}
	}
}
