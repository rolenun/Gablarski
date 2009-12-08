using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Gablarski.Tests
{
	[TestFixture]
	public class UserInfoTests
	{
		private const string Username = "Bar";
		private const string Nickname = "Foo";
		private const string Phonetic = "Phoo";
		private const int UserId = 1;
		private const int ChanId = 2;
		private const bool Muted = true;

		[Test]
		public void InvalidCtor()
		{
			Assert.Throws<ArgumentNullException> (() => new UserInfo ((UserInfo)null));
			Assert.Throws<ArgumentNullException> (() => new UserInfo ((StreamValueReader)null));

			Assert.Throws<ArgumentNullException> (() => new UserInfo (null, UserId, ChanId, Muted));
			Assert.Throws<ArgumentException> (() => new UserInfo (Username, 0, ChanId, Muted));
			Assert.Throws<ArgumentOutOfRangeException> (() => new UserInfo (Username, UserId, -1, Muted));

			Assert.Throws<ArgumentNullException> (() => new UserInfo (null, Phonetic, Username, UserId, ChanId, Muted));
			Assert.Throws<ArgumentNullException> (() => new UserInfo (Nickname, Phonetic, null, UserId, ChanId, Muted));
			Assert.DoesNotThrow (() => new UserInfo (Nickname, null, Username, UserId, ChanId, Muted));
			Assert.Throws<ArgumentException> (() => new UserInfo (Nickname, Phonetic, Username, 0, ChanId, Muted));
			Assert.Throws<ArgumentOutOfRangeException> (() => new UserInfo (Nickname, Phonetic, Username, UserId, -1, Muted));
		}

		[Test]
		public void Ctor()
		{
			var info = new UserInfo (Username, UserId, ChanId, Muted);

			Assert.AreEqual (UserId, info.UserId);
			Assert.AreEqual (ChanId, info.CurrentChannelId);
			Assert.AreEqual (Username, info.Username);
			Assert.AreEqual (Muted, info.IsMuted);

			info = new UserInfo (Nickname, Phonetic, Username, UserId, ChanId, Muted);

			Assert.AreEqual (UserId, info.UserId);
			Assert.AreEqual (ChanId, info.CurrentChannelId);
			Assert.AreEqual (Nickname, info.Nickname);
			Assert.AreEqual (Phonetic, info.Phonetic);
			Assert.AreEqual (Username, info.Username);
			Assert.AreEqual (Muted, info.IsMuted);
		}

		[Test]
		public void SerializeDeserialize()
		{
			var stream = new MemoryStream(new byte[20480], true);
			var writer = new StreamValueWriter (stream);
			var reader = new StreamValueReader (stream);

			var info = new UserInfo (Nickname, Phonetic, Username, UserId, ChanId, Muted);
			info.Serialize (writer);
			long length = stream.Position;
			stream.Position = 0;

			info = new UserInfo (reader);
			Assert.AreEqual (length, stream.Position);
			Assert.AreEqual (UserId, info.UserId);
			Assert.AreEqual (ChanId, info.CurrentChannelId);
			Assert.AreEqual (Nickname, info.Nickname);

			if (Gablarski.Client.GablarskiClient.ProtocolVersion > 3) // HACK
				Assert.AreEqual (Phonetic, info.Phonetic);

			Assert.AreEqual (Muted, info.IsMuted);
		}

		[Test]
		public void Equals()
		{
			UserInfo foo = new UserInfo (Nickname, Phonetic, Username, 1, 2, Muted);
			UserInfo bar = new UserInfo (Nickname, Phonetic, Username, 1, 2, Muted);

			Assert.AreEqual (foo, bar);
		}

		[Test]
		public void HashCode()
		{
			UserInfo foo = new UserInfo (Nickname, Phonetic, Username, 1, 2, Muted);
			UserInfo bar = new UserInfo (Nickname, Phonetic, Username, 1, 2, Muted);

			Assert.AreEqual (foo.GetHashCode(), bar.GetHashCode());
		}
	}
}