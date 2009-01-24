﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gablarski
{
	/// <summary>
	/// Represents a media source from another user
	/// </summary>
	public interface IMediaSource
	{
		/// <summary>
		/// Gets the ID of the source
		/// </summary>
		int ID { get; }

		/// <summary>
		/// Gets the type of the source
		/// </summary>
		MediaSourceType Type { get; }

		/// <summary>
		/// Gets an implementation of the audio codec used for the source
		/// </summary>
		IMediaCodec Codec { get; }
	}

	/// <summary>
	/// IMediaSource types
	/// </summary>
	[Flags]
	public enum MediaSourceType
	{
		/// <summary>
		/// Voice (Players talking)
		/// </summary>
		Voice = 1,

		/// <summary>
		/// Music
		/// </summary>
		Music = 2,

		/// <summary>
		/// All types
		/// </summary>
		All = Voice | Music
	}
}