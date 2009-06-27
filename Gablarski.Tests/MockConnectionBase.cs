﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Gablarski.Messages;
using NUnit.Framework;

namespace Gablarski.Tests
{
	public abstract class MockConnectionBase
		: IConnection
	{
		protected MockConnectionBase ()
		{
			this.readStream = new MemoryStream (this.buffer, false);
			this.writeStream = new MemoryStream (this.buffer, true);

			this.reader = new StreamValueReader (this.readStream);
			this.writer = new StreamValueWriter (this.writeStream);
		}

		public void Receive (MessageBase message)
		{
			lock (this.buffer)
			{
				writer.WriteByte (42);
				writer.WriteUInt16 (message.MessageTypeCode);
				message.WritePayload (this.writer, this.IdentifyingTypes);
				Interlocked.Increment (ref this.waiting);
			}

			var received = this.MessageReceived;
			if (received != null)
				received (this, new MessageReceivedEventArgs (this, message));
		}

		public MessageBase DequeueMessage ()
		{
			while (this.waiting == 0)
				Thread.Sleep (1);

			MessageBase msg;
			lock (this.buffer)
			{
				byte sanity = this.reader.ReadByte();
				if (sanity != 0x2A)
					Assert.Fail ("Header does not begin with 42.");

				ushort type = this.reader.ReadUInt16();
				if (!MessageBase.MessageTypes.ContainsKey (type))
					Assert.Fail ("Type " + type + " does not exist.");

				msg = MessageBase.MessageTypes[type]();
				msg.ReadPayload (this.reader, this.IdentifyingTypes);

				Interlocked.Decrement (ref this.waiting);
			}

			return msg;
		}

		public T DequeueAndAssertMessage<T> ()
			where T : MessageBase
		{
			var message = this.DequeueMessage ();
			Assert.IsInstanceOf<T> (message);

			return (T)message;
		}

		#region IConnection Members
		public event EventHandler<MessageReceivedEventArgs> MessageReceived;
		public event EventHandler<ConnectionEventArgs> Disconnected;

		public bool IsConnected
		{
			get { return this.connected; }
		}

		public IdentifyingTypes IdentifyingTypes
		{
			get;
			set;
		}

		public abstract void Send (MessageBase message);

		public void Disconnect ()
		{
			this.connected = false;
			var dced = this.Disconnected;
			if (dced != null)
				dced (this, new ConnectionEventArgs (this));
		}

		#endregion

		private bool connected;

		private readonly StreamValueWriter writer;
		private readonly StreamValueReader reader;
		private readonly byte[] buffer = new byte[20480];
		private readonly MemoryStream readStream;
		private readonly MemoryStream writeStream;
		private int waiting = 0;
	}
}