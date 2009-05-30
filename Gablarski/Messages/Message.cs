﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gablarski.Messages
{
	public enum ClientMessageType
		: ushort
	{
		Connect = 1,
		Login = 3,
		Disconnect = 5,

		RequestSourceList = 15,
		RequestSource = 7,
		AudioData = 9,

		RequestServerInfo = 11,
		RequestPlayerList = 13,
		
		RequestChannelList = 18,
		ChangeChannel = 20,
		EditChannel = 22
	}

	public enum ServerMessageType
		: ushort
	{
		ConnectionRejected = 2,
		ServerInfoReceived = 12,
		LoginResult = 4,
		Disconnect = 6,

		SourceListReceived = 16,
		SourceResult = 8,
		AudioDataReceived = 10,
	
		PlayerListReceived = 14,
		PlayerDisconnected = 17,

		ChannelListReceived = 19,
		ChangeChannelResult = 21,
	}

	public abstract class Message<TMessage>
		: MessageBase
	{
		protected Message (TMessage messageType)
		{
			this.MessageType = messageType;
		}

		public TMessage MessageType
		{
			get;
			protected set;
		}
	}

	//public abstract class DualMessage
	//    : MessageBase
	//{
	//    protected DualMessage (ClientMessage clientMessageType, ServerMessage serverMessageType)
	//    {
	//        this.ClientMessageType = clientMessageType;
	//        this.ServerMessageType = serverMessageType;
	//    }

	//    public ClientMessage ClientMessageType
	//    {
	//        get;
	//        protected set;
	//    }

	//    public ServerMessage ServerMessageType
	//    {
	//        get;
	//        protected set;
	//    }
	//}
}