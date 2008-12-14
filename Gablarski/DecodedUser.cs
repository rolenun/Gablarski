﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gablarski
{
	internal class DecodedUser
		: IUser
	{
		#region IUser Members

		public uint ID
		{
			get; set;
		}

		public string Nickname
		{
			get; set;
		}

		public string Username
		{
			get; set;
		}

		public UserState State
		{
			get; set;
		}

		public ChannelInfo Channel
		{
			get; set;
		}

		#endregion
	}
}