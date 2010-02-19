﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Cadenza;
using Gablarski.Audio;
using Gablarski.Client;
using Gablarski.Clients.Windows.Properties;

namespace Gablarski.Clients.Windows
{
	public class UserTreeView
		: TreeView
	{
		public UserTreeView()
		{
			this.AllowDrop = true;

			this.ImageList = new ImageList
			{
				TransparentColor = Color.Transparent,
				ImageSize = new Size (16, 16),
				ColorDepth = ColorDepth.Depth24Bit
			};

			this.ImageList.Images.Add ("server",	Resources.ServerImage);
			this.ImageList.Images.Add ("channel",	Resources.ChannelImage);
			this.ImageList.Images.Add ("silent",	Resources.SoundNoneImage);
			this.ImageList.Images.Add ("talking",	Resources.SoundImage);
			this.ImageList.Images.Add ("music",		Resources.MusicImage);
			this.ImageList.Images.Add ("muted",		Resources.SoundMuteImage);
			this.ImageList.Images.Add ("mutedmic",	Resources.CaptureMuteImage);
		}

		[Browsable (false)]
		[DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public GablarskiClient Client
		{
			get { return this.client; }
			set
			{
				if (this.client != null)
				{
					this.client.CurrentUser.PermissionsChanged -= OnPermissionsChanged;
					this.client.Users.UserMuted -= OnUserMuted;
					this.client.Sources.AudioSourceMuted -= OnSourceMuted;
				}

				this.client = value;
				this.client.CurrentUser.PermissionsChanged += OnPermissionsChanged;
				this.client.Users.UserMuted += OnUserMuted;
				this.client.Sources.AudioSourceMuted += OnSourceMuted;
			}
		}

		private void OnUserMuted (object sender, UserEventArgs e)
		{
			MarkMuted (e.User);
			SetupUserContext (userNodes[e.User]);
		}

		private void OnSourceMuted (object sender, AudioSourceMutedEventArgs e)
		{
			MarkMuted (e.Source);
			SetupUserContext (userNodes[this.client.Users[e.Source.OwnerId]]);
		}

		public void SetServerNode (TreeNode node)
		{
			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<TreeNode>)SetServerNode, node);
				return;
			}

			this.BeginUpdate();

			this.Nodes.Clear();
			this.serverNode = node;
			this.serverNode.ImageKey = "server";
			this.serverNode.SelectedImageKey = "server";
			this.Nodes.Add (node);

			this.EndUpdate();
		}

		public void AddChannel (ChannelInfo channel)
		{
			if (channel == null)
				return;

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<ChannelInfo>)this.AddChannel, channel);
				return;
			}

			var parent = this.serverNode.Nodes;
			if (channel.ParentChannelId != 0)
			{
				var pair = channelNodes.FirstOrDefault (kvp => kvp.Key.ChannelId == channel.ParentChannelId);
				if (!pair.Equals (default(KeyValuePair<ChannelInfo, TreeNode>)))
					parent = pair.Value.Nodes;
			}

			var node = parent.Add (channel.ChannelId.ToString(), channel.Name);
			node.Tag = channel;
			node.ImageKey = "channel";
			node.SelectedImageKey = "channel";

			SetupChannelContext (node);

			this.channelNodes.Add (channel, node);
		}

		public void AddUser (UserInfo user, IEnumerable<AudioSource> sources)
		{
			if (user == null)
				return;

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<UserInfo, IEnumerable<AudioSource>>)this.AddUser, user, sources);
				return;
			}

			var channelPair = this.channelNodes.FirstOrDefault (c => c.Key.ChannelId == user.CurrentChannelId);
			if (channelPair.Equals (default(KeyValuePair<ChannelInfo,TreeNode>)))
				return;

			string displayName = user.Nickname;
			if (!user.Comment.IsNullOrWhitespace())
				displayName += " (" + user.Comment + ")";

			var node = channelPair.Value.Nodes.Add (displayName);
			node.Tag = user;

			string imageKey = "silent";
			if (user.IsMuted)
				imageKey = "muted";
			else if ((user.Status & UserStatus.MutedSound) == UserStatus.MutedSound)
				imageKey = "muted";
			else if ((user.Status & UserStatus.MutedMicrophone) == UserStatus.MutedMicrophone)
				imageKey = "mutedmic";

			node.ImageKey = node.SelectedImageKey = imageKey;

			SetupUserContext (node);

			this.userNodes[user] = node;

			if (Settings.DisplaySources)
			{
				foreach (var source in sources)
					AddSource (source);
			}

			node.Parent.Expand();
		}

		public void AddSource (AudioSource source)
		{
			if (source == null)
				return;

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<AudioSource>)this.AddSource, source);
				return;
			}

			var user = Client.Users[source.Id];
			if (user == null)
				return;

			TreeNode userNode;
			if (!this.userNodes.TryGetValue (user, out userNode))
				return;

			var snode = userNode.Nodes.Add (source.Name);
			snode.Tag = source;
			snode.SelectedImageKey = snode.ImageKey = (!source.IsMuted) ? "silent" : "muted";

			SetupSourceContext (snode);

			this.sourceNodes[source] = snode;

			userNode.Expand();
		}

		public void RemoveUser (UserInfo user)
		{
			if (user == null)
				return;

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<UserInfo>)this.RemoveUser, user);
				return;
			}

			if (!this.userNodes.ContainsKey (user))
				return;

			var node = this.userNodes[user];
			node.Remove();
			this.userNodes.Remove (user);

			foreach (var kvp in this.sourceNodes.Where (kvp => kvp.Key.OwnerId == user.UserId).ToList())
				sourceNodes.Remove (kvp.Key);
		}

		public void MarkTalking (UserInfo user)
		{
			if (user == null)
				return;

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<UserInfo>)this.MarkTalking, user);
				return;
			}

			TreeNode node;
			if (!userNodes.TryGetValue (user, out node) || node.ImageKey == "talking")
				return;

			node.ImageKey = "talking";
			node.SelectedImageKey = "talking";
			SetupUserContext (node);
		}

		public void MarkTalking (AudioSource source)
		{
			if (source == null)
				return;

			if (this.InvokeRequired)
			{
				BeginInvoke ((Action<AudioSource>)MarkTalking, source);
				return;
			}

			TreeNode node;
			if (Settings.DisplaySources && sourceNodes.TryGetValue (source, out node))
				SetupUserContext (node.Parent);
			else if (userNodes.TryGetValue (client.Users[source.OwnerId], out node))
				SetupUserContext (node);

			if (node != null)
			{
				node.ImageKey = "talking";
				node.SelectedImageKey = "talking";
			}
		}

		public void MarkMusic (UserInfo user)
		{
			if (user == null)
				throw new ArgumentNullException ("user");

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<UserInfo>)this.MarkTalking, user);
				return;
			}

			if (!userNodes.ContainsKey (user))
				return;

			var node = userNodes[user];
			userNodes[user].ImageKey = node.SelectedImageKey = "music";
			SetupUserContext (node);
		}

		public void MarkMuted (AudioSource source)
		{
			if (source == null)
				return;

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<AudioSource>)this.MarkMuted, source);
				return;
			}

			TreeNode node;
			if (!sourceNodes.TryGetValue (source, out node))
				return;

			node.SelectedImageKey = node.ImageKey = "muted";
			SetupUserContext (node.Parent);
		}

		public void MarkMuted (UserInfo user)
		{
			if (user == null)
				return;

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<UserInfo>)this.MarkMuted, user);
				return;
			}

			TreeNode node;
			if (!userNodes.TryGetValue (user, out node))
				return;

			node.SelectedImageKey = node.ImageKey = "muted";
			SetupUserContext (node);
		}

		public void MarkSilent (AudioSource source)
		{
			if (source == null)
				return;

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<AudioSource>)this.MarkSilent, source);
				return;
			}

			TreeNode node;
			if (source.IsMuted)
				return;
			else if (Settings.DisplaySources && sourceNodes.TryGetValue (source, out node))
				SetupUserContext (node.Parent);
			else if (userNodes.TryGetValue (client.Users[source.OwnerId], out node))
				SetupUserContext (node);

			if (node != null)
				node.ImageKey = node.SelectedImageKey = "silent";
		}

		public void MarkSilent (UserInfo user)
		{
			if (user == null)
				return;

			if (this.InvokeRequired)
			{
				this.BeginInvoke ((Action<UserInfo>)this.MarkSilent, user);
				return;
			}

			TreeNode node;
			if (!userNodes.TryGetValue (user, out node) || user.IsMuted)
				return;

			node.ImageKey = node.SelectedImageKey = "silent";
			SetupUserContext (node);
		}

		public void Update (IEnumerable<ChannelInfo> channels, IEnumerable<UserInfo> users, IEnumerable<AudioSource> sources)
		{
			if (InvokeRequired)
			{
				BeginInvoke ((Action<IEnumerable<ChannelInfo>, IEnumerable<UserInfo>, IEnumerable<AudioSource>>)Update, channels, users, sources);
				return;
			}

			this.channelNodes.Clear();
			this.userNodes.Clear();
			this.sourceNodes.Clear();

			BeginUpdate();
			this.Nodes.Clear();
			if (this.serverNode == null)
				return;
			
			this.serverNode.Nodes.Clear();
			this.Nodes.Add (this.serverNode);

			foreach (var channel in channels.Where (c => c.ParentChannelId == 0))
			{
				AddChannel (channel);
				AddChannels (channels, channel);
			}

			this.serverNode.Expand();

			foreach (var user in users)
				AddUser (user, sources.Where (s => s.OwnerId == user.UserId));

			UpdateContextMenus (false);

			EndUpdate();
		}

		private TreeNode serverNode;
		private readonly Dictionary<ChannelInfo, TreeNode> channelNodes = new Dictionary<ChannelInfo, TreeNode>();
		private readonly Dictionary<UserInfo, TreeNode> userNodes = new Dictionary<UserInfo, TreeNode>();
		private readonly Dictionary<AudioSource, TreeNode> sourceNodes = new Dictionary<AudioSource, TreeNode>();

		protected override void DefWndProc (ref Message m)
		{
			if (m.Msg != 515)
				base.DefWndProc (ref m);
		}

		protected override void OnNodeMouseClick (TreeNodeMouseClickEventArgs e)
		{
			this.SelectedNode = e.Node;

			base.OnNodeMouseClick (e);
		}

		protected override void OnItemDrag (ItemDragEventArgs e)
		{
			var node = ((TreeNode)e.Item);
			if (node.Tag is UserInfo)
				DoDragDrop (node, DragDropEffects.Move);

			base.OnItemDrag (e);
		}

		protected override void OnDragOver (DragEventArgs e)
		{
			e.Effect = DragDropEffects.Move;
			base.OnDragOver (e);
		}

		protected override void OnDragDrop (DragEventArgs e)
		{
			if (e.Data.GetDataPresent ("System.Windows.Forms.TreeNode", false))
			{
				TreeNode destinationNode = this.GetNodeAt (this.PointToClient (new Point (e.X, e.Y)));
				TreeNode movedNode = (TreeNode)e.Data.GetData ("System.Windows.Forms.TreeNode");

				if (destinationNode != null)
				{
					var channel = destinationNode.Tag as ChannelInfo;
					var user = movedNode.Tag as UserInfo;
					if (channel != null && user != null)
					{
						if (user.CurrentChannelId.Equals (channel.ChannelId))
							return;

						Client.Users.Move (user, channel);
					}
				}
			}

			base.OnDragDrop (e);
		}

		protected override void OnNodeMouseDoubleClick (TreeNodeMouseClickEventArgs e)
		{
			ChannelInfo channel = this.SelectedNode.Tag as ChannelInfo;
			if (e.Button != MouseButtons.Left || channel == null)
			{
				base.OnNodeMouseDoubleClick (e);
				return;
			}

			Client.Users.Move (Client.CurrentUser, channel);
		}

		private void AddChannels (IEnumerable<ChannelInfo> channels, ChannelInfo parent)
		{
			foreach (var c in channels.Where (c => c.ParentChannelId == parent.ChannelId))
			{
				this.AddChannel (c);
				this.AddChannels (channels, c);
			}
		}

		private void ContextDeleteChannelClick (object sender, EventArgs e)
		{
			Client.Channels.Delete (this.SelectedNode.Tag as ChannelInfo);
		}

		private void ContextEditChannelClick (object sender, EventArgs e)
		{
			ChannelForm editChannel = new ChannelForm (this.SelectedNode.Tag as ChannelInfo);
			if (editChannel.ShowDialog() == DialogResult.OK)
				Client.Channels.Update (editChannel.Channel);
		}

		private void ContextAddChannelClick (object sender, EventArgs e)
		{
			ChannelForm addChannel = new ChannelForm ();
			if (addChannel.ShowDialog() != DialogResult.OK)
				return;

			if (this.SelectedNode != null)
			{
				var parent = (this.SelectedNode.Tag as ChannelInfo);
				if (parent != null)
					addChannel.Channel.ParentChannelId = parent.ChannelId;
			}

			this.Client.Channels.Create (addChannel.Channel);
		}

		private void ContextIgnoreUserClick (object sender, EventArgs e)
		{
			var u = (UserInfo)this.SelectedNode.Tag;
			if (Client.Users.ToggleIgnore (u))
				MarkMuted (u);
			else if (!u.IsMuted)
				MarkSilent (u);

			SetupUserContext (userNodes[u]);
		}

		private void ContextIgnoreSourceClick (object sender, EventArgs e)
		{
			var s = ((AudioSource)((ToolStripItem)sender).Tag);
			if (Client.Sources.ToggleIgnore (s))
				MarkMuted (s);
			else if (!s.IsMuted)
				MarkSilent (s);

			SetupUserContext (userNodes[userNodes.Keys.FirstOrDefault (u => u.UserId == s.OwnerId)]);
			if (sourceNodes.ContainsKey (s))
				SetupSourceContext (sourceNodes[s]);
		}

		private void ContextMuteUserClick (object sender, EventArgs e)
		{
			Client.Users.ToggleMute ((UserInfo)this.SelectedNode.Tag);
		}

		private void ContextMuteSourceClick (object sender, EventArgs e)
		{
			Client.Sources.ToggleMute (((AudioSource)((ToolStripMenuItem)sender).Tag));
		}

		private GablarskiClient client;
		private void OnPermissionsChanged (object sender, EventArgs e)
		{
			UpdateContextMenus (true);
		}

		private void SetupSourceContext (TreeNode snode)
		{
			snode.ContextMenuStrip = new ContextMenuStrip();

			var target = (AudioSource)snode.Tag;

			if (target.OwnerId != Client.CurrentUser.UserId)
			{
				AddSourceContext (snode.ContextMenuStrip.Items, target);
			}
		}

		private void AddSourceContext (ToolStripItemCollection items, AudioSource source)
		{
			if (!Client.Sources.GetIsIgnored (source))
			{
				var ignore = new ToolStripMenuItem ("Ignore '" + source.Name + "'", Resources.SoundMuteImage);
				ignore.ToolTipText = "Ignores the audio source";
				ignore.Click += ContextIgnoreSourceClick;
				ignore.Tag = source;

				items.Add (ignore);
			}
			else
			{
				var ignore = new ToolStripMenuItem ("Unignore '" + source.Name + "'", Resources.SoundImage);
				ignore.ToolTipText = "Unignores the audio source";
				ignore.Click += ContextIgnoreSourceClick;
				ignore.Tag = source;

				items.Add (ignore);
			}

			if (Client.CurrentUser.Permissions.CheckPermission (PermissionName.MuteAudioSource))
			{
				if (!source.IsMuted)
				{
					var mute = new ToolStripMenuItem ("Mute '" + source.Name + "'", Resources.SoundMuteImage);
					mute.ToolTipText = "Mutes the audio source for everyone";
					mute.Click += ContextMuteSourceClick;
					mute.Tag = source;

					items.Add (mute);
				}
				else
				{
					var mute = new ToolStripMenuItem ("Unmute '" + source.Name + "'", Resources.SoundImage);
					mute.ToolTipText = "Unmutes the audio source for everyone";
					mute.Click += ContextMuteSourceClick;
					mute.Tag = source;

					items.Add (mute);
				}
			}
		}

		private void SetupUserContext (TreeNode un)
		{
			if (this.InvokeRequired)
			{
				Invoke ((Action<TreeNode>)SetupUserContext, un);
				return;
			}

			un.ContextMenuStrip = new ContextMenuStrip();
			
			var target = (UserInfo)un.Tag;

			if (!target.Comment.IsNullOrWhitespace())
			{
				var copy = new ToolStripMenuItem ("Copy comment", Resources.CopyCommentImage);
				copy.ToolTipText = "Copys the user's comment";
				copy.Click += ContextCopyUserCommentClick;
				un.ContextMenuStrip.Items.Add (copy);

				string comment = target.Comment.Trim();
				if (comment.StartsWith ("http://") || comment.StartsWith ("www."))
				{
					var gotourl = new ToolStripMenuItem ("Open web page", Resources.LinkImage);
					gotourl.ToolTipText = "Opens the URL in your web browser";
					gotourl.Click += ContextGotoUrlUserCommentClick;
					un.ContextMenuStrip.Items.Add (gotourl);
				}

				un.ContextMenuStrip.Items.Add (new ToolStripSeparator());
			}

			if (!Client.Users.GetIsIgnored (target))
			{
				var ignore = new ToolStripMenuItem ("Ignore user", Resources.SoundMuteImage);
				ignore.ToolTipText = "Ignores the user";
				ignore.Click += ContextIgnoreUserClick;

				un.ContextMenuStrip.Items.Add (ignore);
			}
			else
			{
				var ignore = new ToolStripMenuItem ("Unignore user", Resources.SoundImage);
				ignore.ToolTipText = "Unignores the user";
				ignore.Click += ContextIgnoreUserClick;

				un.ContextMenuStrip.Items.Add (ignore);
			}

			if (this.Client.CurrentUser.Permissions.CheckPermission (PermissionName.MuteUser))
			{
				if (!target.IsMuted)
				{
					var mute = new ToolStripMenuItem ("Mute user", Resources.SoundMuteImage);
					mute.ToolTipText = "Mutes the user for everyone";
					mute.Click += ContextMuteUserClick;

					un.ContextMenuStrip.Items.Add (mute);
				}
				else
				{
					var mute = new ToolStripMenuItem ("Unmute user", Resources.SoundImage);
					mute.ToolTipText = "Unmutes the user for everyone";
					mute.Click += ContextMuteUserClick;

					un.ContextMenuStrip.Items.Add (mute);
				}
			}

			ToolStripMenuItem menu = null;
			foreach (var source in Client.Sources[target])
			{
				if (menu == null)
				{
					menu = new ToolStripMenuItem ("Sources");
					un.ContextMenuStrip.Items.Add (menu);
				}

				AddSourceContext (menu.DropDown.Items, source);
			}
		}

		private void ContextGotoUrlUserCommentClick(object sender, EventArgs e)
		{
			string url = ((UserInfo) this.SelectedNode.Tag).Comment.Trim();

			if (url.StartsWith ("www."))
				url = "http://" + url;

			Process.Start (url);
		}

		private void ContextCopyUserCommentClick (object sender, EventArgs e)
		{
			Clipboard.SetText (((UserInfo)this.SelectedNode.Tag).Comment);
		}
		
		private void SetupChannelContext (TreeNode cn)
		{
			var channel = (ChannelInfo)cn.Tag;
			cn.ContextMenuStrip = new ContextMenuStrip();

			if (this.client.CurrentUser.Permissions.CheckPermission (PermissionName.AddChannel))
			{
				var add = new ToolStripMenuItem ("Add Channel", Resources.ChannelAddImage);
				add.Click += ContextAddChannelClick;

				cn.ContextMenuStrip.Items.Add (add);
			}

			if (!channel.ReadOnly && this.Client.CurrentUser.Permissions.CheckPermission (PermissionName.EditChannel))
			{
				var edit = new ToolStripMenuItem ("Edit Channel", Resources.ChannelEditImage);
				edit.Click += ContextEditChannelClick;

				cn.ContextMenuStrip.Items.Add (edit);
			}

			if (!channel.ReadOnly && this.Client.CurrentUser.Permissions.CheckPermission (PermissionName.DeleteChannel))
			{
				var delete = new ToolStripMenuItem ("Delete Channel", Resources.ChannelDeleteImage);
				delete.Click += ContextDeleteChannelClick;

				cn.ContextMenuStrip.Items.Add (delete);
			}
		}

		private void UpdateContextMenus (bool full)
		{
			this.ContextMenuStrip = new ContextMenuStrip();
			
			if (this.client.CurrentUser.Permissions.CheckPermission (PermissionName.AddChannel))
			{
				var add = new ToolStripMenuItem ("Add Channel", Resources.ChannelAddImage);
				add.Click += ContextAddChannelClick;

				this.ContextMenuStrip.Items.Add (add);
			}

			if (full)
			{
				foreach (var cn in this.channelNodes.Values)
					SetupChannelContext (cn);

				foreach (var un in this.userNodes.Values)
					SetupUserContext (un);
			}
		}
	}
}