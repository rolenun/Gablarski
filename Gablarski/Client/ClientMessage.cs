﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gablarski.Client
{
	/// <summary>
	/// Client -> Server message
	/// </summary>
	public abstract class ClientMessage
		: Message<ClientMessages>
	{
		public ClientMessage (ClientMessages messageType, AuthedClient client)
			: base (messageType, client)
		{
		}

		protected override ushort MessageTypeCode
		{
			get { return (ushort)this.MessageType; }
		}

		protected override bool SendAuthHash
		{
			get { return true; }
		}
	}

	public enum ClientMessages
		: ushort
	{
		Ping = 1,
		Login = 2,
		Disconnect = 3,
		AudioData = 4
	}
}