﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gablarski.Audio;
using Gablarski.Client;
using Gablarski.Tests.Mocks.Audio;
using NUnit.Framework;

namespace Gablarski.Tests
{
	[TestFixture]
	public class AudioEngineTests
	{
		private IClientContext context;
		private IAudioReceiver receiver;
		private IAudioSender sender;
		private ICaptureProvider provider;
		private AudioSource source;

		[SetUp]
		public void Setup()
		{
			this.provider = new MockCaptureProvider();
			this.source = new AudioSource ("mockSource", 1, 1, 1, 64000, 44100, 256, 10, false);

			var client = new GablarskiClient (new MockClientConnection (new MockConnectionProvider().EstablishConnection()));
			this.context = client;
			this.sender = client.Sources;
			this.receiver = client.Sources;
		}

		[TearDown]
		public void TearDown()
		{
			this.provider = null;
			this.source = null;
			this.receiver = null;
			this.sender = null;
			this.context = null;
		}

		[Test]
		public void InvalidAttach()
		{
			var engine = new AudioEngine();

			Assert.Throws<ArgumentNullException> (() => engine.Attach (null, AudioFormat.Mono16Bit, this.source, new AudioEngineCaptureOptions()));
			Assert.Throws<ArgumentNullException> (() => engine.Attach (this.provider, AudioFormat.Mono16Bit, null, new AudioEngineCaptureOptions()));
			Assert.Throws<ArgumentNullException> (() => engine.Attach (this.provider, AudioFormat.Mono16Bit, this.source,  null));
		}

		[Test]
		public void InvalidDetatch()
		{
			var engine = new AudioEngine();
			Assert.Throws<ArgumentNullException> (() => engine.Detach ((ICaptureProvider)null));
			Assert.Throws<ArgumentNullException> (() => engine.Detach ((AudioSource)null));
		}

		[Test]
		public void StartWithoutReceiver()
		{
			var engine = new AudioEngine();
			engine.AudioSender = new ClientSourceManager (context);
			engine.Context = context;
			Assert.Throws<InvalidOperationException> (engine.Start);
		}

		[Test]
		public void StartWithoutSender()
		{
			var engine = new AudioEngine();
			engine.AudioReceiver = receiver;
			engine.Context = context;
			Assert.Throws<InvalidOperationException> (engine.Start);
		}

		[Test]
		public void StartWithoutContext()
		{
			var engine = new AudioEngine();
			engine.AudioReceiver = receiver;
			engine.AudioSender = sender;
			Assert.Throws<InvalidOperationException> (engine.Start);
		}

		[Test]
		public void StartRunning()
		{
			var engine = new AudioEngine();
			engine.AudioReceiver = receiver;
			engine.AudioSender = sender;
			engine.Context = context;

			engine.Start();
			Assert.DoesNotThrow (engine.Start);
		}

		[Test]
		public void Start()
		{
			var engine = new AudioEngine();
			engine.AudioReceiver = receiver;
			engine.AudioSender = sender;
			engine.Context = context;
			engine.Start();

			Assert.IsTrue (engine.IsRunning);

			engine.Stop();
		}

		[Test]
		public void Stop()
		{
			var engine = new AudioEngine();
			engine.AudioReceiver = receiver;
			engine.AudioSender = sender;
			engine.Context = context;
			engine.Start();

			engine.Stop();

			Assert.IsFalse (engine.IsRunning);
		}

		[Test]
		public void AttachDetatchSource()
		{
		    var engine = new AudioEngine();

		    engine.Attach (this.provider, AudioFormat.Mono16Bit, this.source, new AudioEngineCaptureOptions());
		    Assert.IsTrue (engine.Detach (this.source));
		}

		[Test]
		public void AttachDetatchProvider()
		{
		    var engine = new AudioEngine();

		    engine.Attach (this.provider, AudioFormat.Mono16Bit, this.source, new AudioEngineCaptureOptions());
		    Assert.IsTrue (engine.Detach (this.provider));
		}

		[Test]
		public void InvalidMute()
		{
			var engine = new AudioEngine();
			Assert.Throws<ArgumentNullException> (() => engine.Mute ((IPlaybackProvider)null));
			Assert.Throws<ArgumentNullException> (() => engine.Mute ((ICaptureProvider)null));
		}

		[Test]
		public void InvalidUnmute()
		{
			var engine = new AudioEngine();
			Assert.Throws<ArgumentNullException> (() => engine.Unmute ((IPlaybackProvider)null));
			Assert.Throws<ArgumentNullException> (() => engine.Unmute ((ICaptureProvider)null));
		}

		//[Test]
		//public void InvalidBeginCapture()
		//{
		//    var engine = new AudioEngine();
		//    engine.Attach (this.provider, AudioFormat.Mono16Bit, this.source, new AudioEngineCaptureOptions());
		//    Assert.Throws<ArgumentNullException> (() => engine.BeginCapture (null));
		//}

		//[Test]
		//public void InvalidEndCapture()
		//{
		//    var engine = new AudioEngine();
		//    engine.Attach (this.provider, AudioFormat.Mono16Bit, this.source, new AudioEngineCaptureOptions());
		//    Assert.Throws<ArgumentNullException> (() => engine.EndCapture (null));
		//}
	}
}