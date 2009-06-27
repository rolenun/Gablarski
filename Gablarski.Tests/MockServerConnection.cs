﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gablarski.Messages;
using System.Threading;
using NUnit.Framework;

namespace Gablarski.Tests
{
	public class MockServerConnection
		: MockConnectionBase
	{
		public MockClientConnection Client
		{
			get
			{
				if (this.client == null)
				{
					this.client = new MockClientConnection (this);
					this.client.IdentifyingTypes = this.IdentifyingTypes;
				}

				return this.client;
			}
		}

		public override void Send (MessageBase message)
		{
			if (this.client == null)
			{
				this.client = new MockClientConnection (this);
				this.client.IdentifyingTypes = this.IdentifyingTypes;
			}

			this.client.Receive (message);
		}
		
		private MockClientConnection client;
	}
}
