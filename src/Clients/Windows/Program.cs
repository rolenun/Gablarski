﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using Gablarski.Client;
using Gablarski.Clients.Input;
using Gablarski.Clients.Persistence;
using System.Linq;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Collections.Generic;
using Microsoft.WindowsAPICodePack.Shell;
using Tempest;

namespace Gablarski.Clients.Windows
{
	static class Program
	{
		public static Task<RSAAsymmetricKey> Key;

		public static void EnableGablarskiURIs()
		{
			try
			{
				Process p = new Process
				{
					StartInfo = new ProcessStartInfo ("cmd.exe", "/C ftype gablarski=\"" + Path.Combine (Environment.CurrentDirectory, Process.GetCurrentProcess ().ProcessName) + ".exe\" \"%1\"")
					{
						Verb = "runas",
					}
				};
				p.Start ();
				p.WaitForExit ();
			}
			catch (Win32Exception)
			{
			}
		}

		public static void DisableGablarskiURIs()
		{
			try
			{
				Process p = new Process
				{
					StartInfo = new ProcessStartInfo ("cmd.exe", "/C ftype gablarski=")
					{
						Verb = "runas",
					}
				};
				p.Start();
				p.WaitForExit();
			}
			catch (Win32Exception)
			{
			}
		}

		public static void UpdateTaskbarServers()
		{
			if (!TaskbarManager.IsPlatformSupported)
				return;

			var jl = JumpList.CreateJumpList ();
			var serverCategory = new JumpListCustomCategory ("Servers");

			string exe = Process.GetCurrentProcess ().MainModule.FileName;

			IList<ServerEntry> servers = Servers.GetEntries ().ToList ();
			JumpListLink[] links = new JumpListLink[servers.Count];
			for (int i = 0; i < servers.Count; ++i)
			{
				var s = servers[i];
				links[i] = new JumpListLink (exe, s.Name)
				{
					Arguments = s.Id.ToString(),
					IconReference = new IconReference (exe, 1)
				};
			}
			
			serverCategory.AddJumpListItems (links);

			jl.AddCustomCategories (serverCategory);

			try
			{
				jl.Refresh ();
			}
			catch (UnauthorizedAccessException) // Jumplists disabled
			{
			}
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main (string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				MessageBox.Show ("Unexpected error" + Environment.NewLine + (e.ExceptionObject as Exception).ToDisplayString(),
				                 "Unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			};

			AutomaticErrorReporter errors = new AutomaticErrorReporter();
			errors.Add (new GablarskiErrorReporter (typeof(Program).Assembly));

			log4net.Config.XmlConfigurator.Configure ();
			log4net.LogManager.GetLogger ("Gablarski WinForms").InfoFormat ("Program Start. PID: {0}", Process.GetCurrentProcess().Id);

			FileInfo program = new FileInfo (Process.GetCurrentProcess().MainModule.FileName);
			Environment.CurrentDirectory = program.Directory.FullName;

			Key = ClientData.GetCryptoKeyAsync();

			if (Settings.FirstRun)
			{
				DialogResult result = MessageBox.Show ("Register gablarski:// urls with this client?", "Register gablarski://", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
				Settings.EnableGablarskiURLs = (result == DialogResult.OK);
				Settings.Version = typeof (Program).Assembly.GetName().Version.ToString();
				Settings.Save ();

				IInputProvider input = Modules.Input.FirstOrDefault (p => p.GetType().GetSimpleName() == Settings.InputProvider);
				if (input != null)
				{
					foreach (CommandBinding binding in input.Defaults)
						Settings.CommandBindings.Add (new CommandBinding (binding.Provider, binding.Command, binding.Input));
				}

				try
				{
					if (Settings.EnableGablarskiURLs)
						EnableGablarskiURIs();

					Settings.FirstRun = false;
					Settings.Save ();
				}
				catch
				{
				}
			}

			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);

			var m = new MainForm();
			m.Show();

			UpdateTaskbarServers();

			if (args.Length > 0)
			{
				int id;
				if (Int32.TryParse (args[0], out id))
				{
					ServerEntry server = Servers.GetEntries ().FirstOrDefault (s => s.Id == id);
					if (server == null)
					{
						if (!m.ShowConnect (true))
							return;
					}
					else
					{
						m.Connect (server);
					}
				}
				else
				{
					Uri server = new Uri (args[0]);
					m.Connect (server.Host, (server.Port != -1) ? server.Port : 42912);
				}
			}
			else if (Settings.ShowConnectOnStart)
			{
				if (!m.ShowConnect (true))
					return;
			}
			
			Application.Run (m);

			Settings.Save();
		}
	}
}