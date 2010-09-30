using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Cadenza;
using Gablarski.Clients.Input;
using Gablarski.Clients.Windows.Entities;

namespace Gablarski.Clients.Windows
{
	public static class Persistance
	{
		static Persistance()
		{
			DirectoryInfo gablarskiData = new DirectoryInfo (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "Gablarski"));
			if (!gablarskiData.Exists)
				gablarskiData.Create();

			string useLocal = ConfigurationManager.AppSettings["useLocalDatabase"];

			if (useLocal != null && Boolean.Parse (useLocal))
				DbFile = new FileInfo ("gablarski.db");
			else
				DbFile = new FileInfo (Path.Combine (gablarskiData.FullName, "gablarski.db"));

			var builder = new SQLiteConnectionStringBuilder();
			builder.DataSource = DbFile.FullName;

			if (DbFile.Exists)
			{
				db = new SQLiteConnection (builder.ToString());
				db.Open();

				if (Settings.Version.IsNullOrWhitespace())
				{
					db.Close();
					DbFile.Delete();
					DbFile.Refresh();
					Settings.Clear();
				}
			}

			db = new SQLiteConnection(builder.ToString());
			db.Open();
			
			CreateDbs();
		}

		public static IEnumerable<SettingEntry> GetSettings()
		{
			using (var cmd = db.CreateCommand ("SELECT * FROM settings"))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					yield return new SettingEntry (Convert.ToInt32 (reader["settingId"]))
					{
						Name = Convert.ToString (reader["settingName"]),
						Value = Convert.ToString (reader["settingValue"])
					};
				}
			}
		}

		#region Ignores
		public static void SaveOrUpdate (IgnoreEntry ignore)
		{
			if (ignore == null)
				throw new ArgumentNullException ("ignore");

			using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = ignore.Id > 0
				                  	? "UPDATE ignores SET ignoreServerId=?,ignoreUsername=? WHERE (ignoreId=?)"
				                  	: "INSERT INTO ignores (ignoreServerId,ignoreUsername) VALUES (?,?)";

				cmd.Parameters.Add (new SQLiteParameter ("server", ignore.ServerId));
				cmd.Parameters.Add (new SQLiteParameter ("username", ignore.Username));

				if (ignore.Id > 0)
					cmd.Parameters.Add (new SQLiteParameter ("id", ignore.Id));

				cmd.ExecuteNonQuery();
			}
		}

		public static IEnumerable<IgnoreEntry> GetIgnores()
		{
			using (var cmd = db.CreateCommand ("SELECT * FROM ignores"))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					yield return new IgnoreEntry (Convert.ToInt32 (reader["ignoreId"]))
					{
						ServerId = Convert.ToInt32 (reader["ignoreServerId"]),
						Username = Convert.ToString (reader["ignoreUsername"])
					};
				}
			}
		}

		public static void Delete (IgnoreEntry ignore)
		{
			if (ignore == null)
				throw new ArgumentNullException ("ignore");
			if (ignore.Id < 1)
				throw new ArgumentException ("Can't delete a non-existant ignore", "ignore");

			using (var cmd = db.CreateCommand ("DELETE FROM ignores WHERE (ignoreId=?)"))
			{
				cmd.Parameters.Add (new SQLiteParameter ("id", ignore.Id));
				cmd.ExecuteNonQuery();
			}
		}
		#endregion

		#region Servers
		public static void SaveOrUpdate (SettingEntry setting)
		{
			if (setting == null)
				throw new ArgumentNullException ("setting");

			using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = setting.Id > 0
				                  	? "UPDATE settings SET settingName=?,settingValue=? WHERE (settingId=?)"
				                  	: "INSERT INTO settings (settingName,settingValue) VALUES (?,?)";

				cmd.Parameters.Add (new SQLiteParameter ("name", setting.Name));
				cmd.Parameters.Add (new SQLiteParameter ("value", setting.Value));

				if (setting.Id > 0)
					cmd.Parameters.Add (new SQLiteParameter ("id", setting.Id));

				cmd.ExecuteNonQuery();
			}
		}

		public static IEnumerable<ServerEntry> GetServers()
		{
			using (var cmd = db.CreateCommand ("SELECT * FROM servers"))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					yield return new ServerEntry (Convert.ToInt32 (reader[0]))
					{
						Name = Convert.ToString (reader[1]),
						Host = Convert.ToString (reader[2]),
						Port = Convert.ToInt32 (reader[3]),
						ServerPassword = Convert.ToString (reader[4]),
						UserNickname = Convert.ToString (reader[5]),
						UserPhonetic = Convert.ToString (reader[6]),
						UserName = Convert.ToString (reader[7]),
						UserPassword = Convert.ToString (reader[8])
					};
				}
			}
		}

		public static void SaveOrUpdate (ServerEntry server)
		{
			if (server == null)
				throw new ArgumentNullException ("server");
			
			using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = server.Id > 0 ? "UPDATE servers SET serverName=?,serverHost=?,serverPort=?,serverPassword=?,serverUserNickname=?,serverUserPhonetic=?,serverUserName=?,serverUserPassword=? WHERE (serverId=?)" 
					: "INSERT INTO servers (serverName,serverHost,serverPort,serverPassword,serverUserNickname,serverUserPhonetic,serverUserName,serverUserPassword) VALUES (?,?,?,?,?,?,?,?)";

				cmd.Parameters.Add (new SQLiteParameter ("name", server.Name));
				cmd.Parameters.Add (new SQLiteParameter ("host", server.Host));
				cmd.Parameters.Add (new SQLiteParameter ("port", server.Port));
				cmd.Parameters.Add (new SQLiteParameter ("password", server.ServerPassword));
				cmd.Parameters.Add (new SQLiteParameter ("nickname", server.UserNickname));
				cmd.Parameters.Add (new SQLiteParameter ("phonetic", server.UserPhonetic));
				cmd.Parameters.Add (new SQLiteParameter ("username", server.UserName));
				cmd.Parameters.Add (new SQLiteParameter ("userpassword", server.UserPassword));

				if (server.Id > 0)
					cmd.Parameters.Add (new SQLiteParameter ("id", server.Id));

				cmd.ExecuteNonQuery();
			}
		}

		public static void Delete (ServerEntry server)
		{
			if (server == null)
				throw new ArgumentNullException ("server");
			if (server.Id < 1)
				throw new ArgumentException ("Can't delete a non-existant server", "server");

			using (var cmd = db.CreateCommand ("DELETE FROM servers WHERE (serverId=?)"))
			{
				cmd.Parameters.Add (new SQLiteParameter ("id", server.Id));
				cmd.ExecuteNonQuery();
			}
		}
		#endregion

		#region Volume
		public static IEnumerable<VolumeEntry> GetVolumes (ServerEntry server)
		{
			if (server == null)
				throw new ArgumentNullException ("server");

			using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = "SELECT * FROM volumes WHERE (volumeServerId=?)";
				cmd.Parameters.Add (new SQLiteParameter ("serverId", server.Id));
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						yield return new VolumeEntry (Convert.ToInt32 (reader[0]))
						{
							ServerId = Convert.ToInt32 (reader[1]),
							Username = Convert.ToString (reader[2]),
							Gain = Convert.ToSingle (reader[3])
						};
					}
				}
			}
		}

		public static void SaveOrUpdate (VolumeEntry volumeEntry)
		{
			if (volumeEntry == null)
				throw new ArgumentNullException ("volumeEntry");

			using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = (volumeEntry.VolumeId > 0)
				                  	? "UPDATE volumes SET volumeServerId=?, volumeUsername=?, volumeGain=? WHERE volumeId=?"
				                  	: "INSERT INTO volumes (volumeServerId,volumeUsername,volumeGain) VALUES (?,?,?)";

				cmd.Parameters.Add (new SQLiteParameter ("serverId", volumeEntry.ServerId));
				cmd.Parameters.Add (new SQLiteParameter ("username", volumeEntry.Username));
				cmd.Parameters.Add (new SQLiteParameter ("gain", volumeEntry.Gain));

				if (volumeEntry.VolumeId > 0)
					cmd.Parameters.Add (new SQLiteParameter ("id", volumeEntry.VolumeId));

				cmd.ExecuteNonQuery();
			}
		}

		public static void Delete (VolumeEntry volumeEntry)
		{
			if (volumeEntry == null)
				throw new ArgumentNullException ("volumeEntry");
			if (volumeEntry.VolumeId < 1)
				throw new ArgumentException ("Can't delete a non-existent server", "volumeEntry");

			using (var cmd = db.CreateCommand ("DELETE FROM volumes WHERE (volumeId=?)"))
			{
				cmd.Parameters.Add (new SQLiteParameter ("id", volumeEntry.VolumeId));
				cmd.ExecuteNonQuery();
			}
		}
		#endregion

		#region Bindings
		public static IEnumerable<CommandBindingEntry> GetCommandBindings()
		{
			using (var cmd = db.CreateCommand ("SELECT * FROM commandbindings"))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					yield return new CommandBindingEntry
					{
						ProviderType = Convert.ToString (reader[0]),
						Command = (Command)Convert.ToInt32 (reader[1]),
						Input = Convert.ToString (reader[2])
					};
				}
			}
		}

		public static void Create (CommandBindingEntry bindingEntry)
		{
			if (bindingEntry == null)
				throw new ArgumentNullException ("bindingEntry");

			using (var cmd = db.CreateCommand())
			{
				cmd.CommandText = "INSERT INTO commandbindings (commandbindingProvider,commandbindingCommand,commandbindingInput) VALUES (?,?,?)";

				cmd.Parameters.Add (new SQLiteParameter ("provider", bindingEntry.ProviderType));
				cmd.Parameters.Add (new SQLiteParameter ("command", (int)bindingEntry.Command));
				cmd.Parameters.Add (new SQLiteParameter ("input", bindingEntry.Input));
				cmd.ExecuteNonQuery();
			}
		}

		public static void DeleteAllBindings ()
		{
			using (var cmd = db.CreateCommand ("DELETE FROM commandbindings"))
			{
				cmd.ExecuteNonQuery();
			}
		}
		#endregion

		private static readonly DbConnection db;
		private static readonly FileInfo DbFile;

		private static void CreateDbs()
		{
			using (var cmd = db.CreateCommand ("CREATE TABLE IF NOT EXISTS servers (serverId integer primary key autoincrement, serverName varchar(255), serverHost varchar(255), serverPort integer, serverPassword varchar(255), serverUserNickname varchar(255), serverUserPhonetic varchar(255), serverUserName varchar(255), serverUserPassword varchar(255))"))
				cmd.ExecuteNonQuery();

			using (var cmd = db.CreateCommand ("CREATE TABLE IF NOT EXISTS settings (settingId integer primary key autoincrement, settingName varchar(255), settingValue varchar(255))"))
				cmd.ExecuteNonQuery();

			using (var cmd = db.CreateCommand ("CREATE TABLE IF NOT EXISTS volumes (volumeId integer primary key autoincrement, volumeServerId integer, volumeUsername varchar(255), volumeGain float)"))
				cmd.ExecuteNonQuery();

			using (var cmd = db.CreateCommand ("CREATE TABLE IF NOT EXISTS ignores (ignoreId integer primary key autoincrement, ignoreServerId integer, ignoreUsername varchar(255))"))
				cmd.ExecuteNonQuery();

			using (var cmd = db.CreateCommand ("CREATE TABLE IF NOT EXISTS commandbindings (commandbindingProvider string, commandbindingCommand integer, commandbindingInput varchar(255))"))
				cmd.ExecuteNonQuery();
		}
	}
}