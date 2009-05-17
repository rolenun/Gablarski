﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gablarski.Messages
{
	public class ServerInfoMessage
		: ServerMessage
	{
		public ServerInfoMessage ()
			: base (ServerMessageType.ServerInfoReceived)
		{
		}

		public ServerInfoMessage (ServerInfo serverInfo)
			: this ()
		{
			this.ServerInfo = serverInfo;
		}

		public ServerInfo ServerInfo
		{
			get;
			set;
		}

		public override void WritePayload (IValueWriter writer)
		{
			this.ServerInfo.Serialize (writer);
		}

		public override void ReadPayload (IValueReader reader)
		{
			this.ServerInfo = new ServerInfo();
			this.ServerInfo.Deserialize (reader);
		}
	}
}