﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gablarski.Client
{
	public interface IDevice
		: IDisposable
	{
		string Name { get; }
	}
}