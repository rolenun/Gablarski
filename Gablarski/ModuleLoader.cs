﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using Mono.Rocks;

namespace Gablarski
{
	[Flags]
	public enum ModuleLoaderOptions
	{
		/// <summary>
		/// Searches the assembly that <see cref="ModuleLoader{T}"/> is defined in.
		/// </summary>
		SearchCurrent = 1,

		/// <summary>
		/// Search the currently executing assembly for implementing types.
		/// </summary>
		SearchExecuting = 2,

		/// <summary>
		/// Search the paths supplied recursively.
		/// </summary>
		SearchRecursively = 4,

		/// <summary>
		/// Searches everywhere.
		/// </summary>
		SearchAll = SearchCurrent | SearchExecuting | SearchRecursively
	}

	/// <summary>
	/// Finds and loads implementing types of a contract.
	/// </summary>
	/// <typeparam name="T">The contract to find implementers for.</typeparam>
	public class ModuleLoader<T>
		where T : class
	{
		public ModuleLoader ()
		{
			Type t = typeof (T);
			if (!t.IsInterface && !t.IsAbstract)
				throw new ArgumentException ("Must be a contract (interface or abstract class)", "T");

			this.contract = t;
			this.Options = ModuleLoaderOptions.SearchCurrent | ModuleLoaderOptions.SearchExecuting;
		}

		public ModuleLoader (ModuleLoaderOptions options, IEnumerable<string> paths)
			: this()
		{
			if (paths == null)
				throw new ArgumentNullException("paths");

			this.Options = options;
			this.paths = paths.Select (s => new DirectoryInfo (s));
		}

		public ModuleLoader (ModuleLoaderOptions options, params string[] paths)
			: this (options, (IEnumerable<string>)paths)
		{
		}

		public ModuleLoaderOptions Options
		{
			get;
			private set;
		}

		public IEnumerable<Type> GetImplementers ()
		{
			IEnumerable<Type> implementers = Enumerable.Empty<Type>();

			if ((Options & ModuleLoaderOptions.SearchCurrent) == ModuleLoaderOptions.SearchCurrent)
				implementers = implementers.Concat (SearchAssembly (Assembly.GetAssembly (typeof (ModuleLoader<T>)), contract));

			if ((Options & ModuleLoaderOptions.SearchExecuting) == ModuleLoaderOptions.SearchExecuting)
				implementers = implementers.Concat (SearchAssembly (Assembly.GetExecutingAssembly(), contract));

			if (this.paths != null)
			{
				foreach (var path in this.paths)
				{
					implementers = implementers.Concat (SearchPath (path, this.contract,
						                                 ((Options & ModuleLoaderOptions.SearchRecursively) ==
						                                  ModuleLoaderOptions.SearchRecursively)));
				}
			}

			return from i in implementers.Distinct()
				   let a = i.GetCustomAttribute<ModuleSelectableAttribute>()
				   where a == null || a.Selectable
				   select i;
		}

		private readonly Type contract;
		private readonly IEnumerable<DirectoryInfo> paths;

		private static readonly HashSet<string> badPaths = new HashSet<string> ();
		internal static IEnumerable<Type> SearchPath (DirectoryInfo dir, Type contract, bool recursive)
		{
			IEnumerable<Type> implementers = Enumerable.Empty<Type>();

			try
			{
				if (recursive)
				{
					DirectoryInfo[] dirs = dir.GetDirectories ();
					for (int i = 0; i < dirs.Length; ++i)
						implementers = implementers.Concat (SearchPath (dirs[i], contract, true));
				}

				FileInfo[] files = dir.GetFiles ("*.dll");
				for (int i = 0; i < files.Length; ++i)
				{
					FileInfo file = files[i];

					lock (badPaths)
					{
						if (badPaths.Contains (file.FullName))
							continue;
					}

					try
					{
						var asm = Assembly.LoadFile (file.FullName);
						implementers = implementers.Concat (SearchAssembly (asm, contract));
					}
					// Ignoring non-assemblies or invalid assemblies.
					catch (BadImageFormatException)
					{
						lock (badPaths)
						{
							badPaths.Add (file.FullName);
						}
					}
					catch (FileLoadException)
					{
						lock (badPaths)
						{
							badPaths.Add (file.FullName);
						}
					}
					catch (AccessViolationException)
					{
						lock (badPaths)
						{
							badPaths.Add (file.FullName);
						}
					}
					catch (UnauthorizedAccessException)
					{
						lock (badPaths)
						{
							badPaths.Add (file.FullName);
						}
					}
					catch (ReflectionTypeLoadException)
					{
					}
				}
			}
			catch (AccessViolationException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}

			return implementers;
		}

		internal static IEnumerable<Type> SearchAssembly (Assembly asm, Type contract)
		{
			return asm.GetTypes().Where (t => t.IsPublic && contract.IsAssignableFrom (t) && !t.IsInterface && !t.IsAbstract);
		}
	}
}
