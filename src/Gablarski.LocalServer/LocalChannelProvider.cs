﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Gablarski.Server;
using NHibernate;

namespace Gablarski.LocalServer
{
	public class LocalChannelProvider
		//: IChannelProvider, IDisposable
	{
		public event EventHandler ChannelsUpdatedExternally;

		public LocalChannelProvider()
		{
			//this.session = Fluently.Configure()
			//    .Database (MySQLConfiguration.Standard.ConnectionString (s => s.Server ("192.168.1.3").Username ("root").Password ("")))
			//    .Mappings (m => m.FluentMappings.AddFromAssembly (Assembly.GetExecutingAssembly()))
			//    .BuildSessionFactory().OpenSession();
		}

		private readonly ISession session;

		#region IChannelProvider Members


		public Type IdentifyingType
		{
			get { return typeof(Int32); }
		}

		public bool UpdateSupported
		{
			get { return true; }
		}

		public NativeChannel DefaultChannel
		{
			get { throw new NotImplementedException (); }
		}

		public IEnumerable<NativeChannel> GetChannels ()
		{
			return this.session.CreateCriteria (typeof (NativeChannel)).List<NativeChannel>();
		}

		public ChannelEditResult SaveChannel (ChannelInfo channel)
		{
			using (var trans = this.session.BeginTransaction())
			{
				this.session.SaveOrUpdate (channel);
			}

			return ChannelEditResult.Success;
		}

		public void DeleteChannel (ChannelInfo channel)
		{
			using (var trans = this.session.BeginTransaction())
			{
				this.session.Delete (channel);
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose ()
		{
			this.session.Dispose();
		}

		#endregion
	}
}