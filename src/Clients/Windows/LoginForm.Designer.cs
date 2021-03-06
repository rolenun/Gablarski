﻿namespace Gablarski.Clients.Windows
{
	partial class LoginForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose (bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent ()
		{
			this.servers = new System.Windows.Forms.ListView();
			this.pnlModServer = new System.Windows.Forms.Panel();
			this.serverStatus = new System.Windows.Forms.PictureBox();
			this.playPhonetic = new System.Windows.Forms.Button();
			this.lblPhonetic = new System.Windows.Forms.Label();
			this.inPhonetic = new System.Windows.Forms.TextBox();
			this.inServerPassword = new System.Windows.Forms.TextBox();
			this.lblServerPassword = new System.Windows.Forms.Label();
			this.labelPassword = new System.Windows.Forms.Label();
			this.labelUsername = new System.Windows.Forms.Label();
			this.inPassword = new System.Windows.Forms.TextBox();
			this.inPort = new Gablarski.Clients.Windows.NumericRequiredTextBox();
			this.labelNickname = new System.Windows.Forms.Label();
			this.labelServer = new System.Windows.Forms.Label();
			this.labelName = new System.Windows.Forms.Label();
			this.inUsername = new System.Windows.Forms.TextBox();
			this.inNickname = new Gablarski.Clients.Windows.RequiredTextBox();
			this.inServer = new Gablarski.Clients.Windows.RequiredTextBox();
			this.inName = new Gablarski.Clients.Windows.RequiredTextBox();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnConnect = new System.Windows.Forms.Button();
			this.settingsButton = new System.Windows.Forms.Button();
			this.startLocal = new System.Windows.Forms.Button();
			this.btnSaveServer = new System.Windows.Forms.Button();
			this.btnEditServer = new System.Windows.Forms.Button();
			this.btnAddServer = new System.Windows.Forms.Button();
			this.pnlModServer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.serverStatus)).BeginInit();
			this.SuspendLayout();
			// 
			// servers
			// 
			this.servers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.servers.Location = new System.Drawing.Point(12, 35);
			this.servers.Name = "servers";
			this.servers.Size = new System.Drawing.Size(282, 246);
			this.servers.TabIndex = 0;
			this.servers.UseCompatibleStateImageBehavior = false;
			this.servers.ItemActivate += new System.EventHandler(this.servers_ItemActivate);
			this.servers.SelectedIndexChanged += new System.EventHandler(this.servers_SelectedIndexChanged);
			this.servers.KeyUp += new System.Windows.Forms.KeyEventHandler(this.servers_KeyUp);
			// 
			// pnlModServer
			// 
			this.pnlModServer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.pnlModServer.Controls.Add(this.serverStatus);
			this.pnlModServer.Controls.Add(this.playPhonetic);
			this.pnlModServer.Controls.Add(this.lblPhonetic);
			this.pnlModServer.Controls.Add(this.inPhonetic);
			this.pnlModServer.Controls.Add(this.inServerPassword);
			this.pnlModServer.Controls.Add(this.lblServerPassword);
			this.pnlModServer.Controls.Add(this.labelPassword);
			this.pnlModServer.Controls.Add(this.labelUsername);
			this.pnlModServer.Controls.Add(this.inPassword);
			this.pnlModServer.Controls.Add(this.inPort);
			this.pnlModServer.Controls.Add(this.labelNickname);
			this.pnlModServer.Controls.Add(this.labelServer);
			this.pnlModServer.Controls.Add(this.labelName);
			this.pnlModServer.Controls.Add(this.inUsername);
			this.pnlModServer.Controls.Add(this.inNickname);
			this.pnlModServer.Controls.Add(this.inServer);
			this.pnlModServer.Controls.Add(this.inName);
			this.pnlModServer.Location = new System.Drawing.Point(12, 35);
			this.pnlModServer.Name = "pnlModServer";
			this.pnlModServer.Size = new System.Drawing.Size(282, 246);
			this.pnlModServer.TabIndex = 3;
			this.pnlModServer.Visible = false;
			// 
			// serverStatus
			// 
			this.serverStatus.Location = new System.Drawing.Point(76, 7);
			this.serverStatus.Name = "serverStatus";
			this.serverStatus.Size = new System.Drawing.Size(20, 20);
			this.serverStatus.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.serverStatus.TabIndex = 13;
			this.serverStatus.TabStop = false;
			// 
			// playPhonetic
			// 
			this.playPhonetic.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.playPhonetic.Enabled = false;
			this.playPhonetic.Image = global::Gablarski.Clients.Windows.Properties.Resources.control_play_blue;
			this.playPhonetic.Location = new System.Drawing.Point(253, 149);
			this.playPhonetic.Name = "playPhonetic";
			this.playPhonetic.Size = new System.Drawing.Size(26, 23);
			this.playPhonetic.TabIndex = 15;
			this.playPhonetic.UseVisualStyleBackColor = true;
			this.playPhonetic.Click += new System.EventHandler(this.playPhonetic_Click);
			// 
			// lblPhonetic
			// 
			this.lblPhonetic.AutoSize = true;
			this.lblPhonetic.Location = new System.Drawing.Point(6, 154);
			this.lblPhonetic.Name = "lblPhonetic";
			this.lblPhonetic.Size = new System.Drawing.Size(52, 13);
			this.lblPhonetic.TabIndex = 14;
			this.lblPhonetic.Text = "Phonetic:";
			// 
			// inPhonetic
			// 
			this.inPhonetic.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.inPhonetic.Location = new System.Drawing.Point(102, 151);
			this.inPhonetic.MaxLength = 255;
			this.inPhonetic.Name = "inPhonetic";
			this.inPhonetic.Size = new System.Drawing.Size(145, 20);
			this.inPhonetic.TabIndex = 13;
			// 
			// inServerPassword
			// 
			this.inServerPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.inServerPassword.Location = new System.Drawing.Point(102, 79);
			this.inServerPassword.MaxLength = 255;
			this.inServerPassword.Name = "inServerPassword";
			this.inServerPassword.Size = new System.Drawing.Size(177, 20);
			this.inServerPassword.TabIndex = 3;
			// 
			// lblServerPassword
			// 
			this.lblServerPassword.AutoSize = true;
			this.lblServerPassword.Location = new System.Drawing.Point(6, 82);
			this.lblServerPassword.Name = "lblServerPassword";
			this.lblServerPassword.Size = new System.Drawing.Size(90, 13);
			this.lblServerPassword.TabIndex = 12;
			this.lblServerPassword.Text = "Server Password:";
			// 
			// labelPassword
			// 
			this.labelPassword.AutoSize = true;
			this.labelPassword.Location = new System.Drawing.Point(6, 226);
			this.labelPassword.Name = "labelPassword";
			this.labelPassword.Size = new System.Drawing.Size(56, 13);
			this.labelPassword.TabIndex = 11;
			this.labelPassword.Text = "Password:";
			// 
			// labelUsername
			// 
			this.labelUsername.AutoSize = true;
			this.labelUsername.Location = new System.Drawing.Point(6, 190);
			this.labelUsername.Name = "labelUsername";
			this.labelUsername.Size = new System.Drawing.Size(58, 13);
			this.labelUsername.TabIndex = 10;
			this.labelUsername.Text = "Username:";
			// 
			// inPassword
			// 
			this.inPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.inPassword.Location = new System.Drawing.Point(102, 223);
			this.inPassword.MaxLength = 255;
			this.inPassword.Name = "inPassword";
			this.inPassword.Size = new System.Drawing.Size(177, 20);
			this.inPassword.TabIndex = 6;
			this.inPassword.UseSystemPasswordChar = true;
			// 
			// inPort
			// 
			this.inPort.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.inPort.Location = new System.Drawing.Point(238, 7);
			this.inPort.Name = "inPort";
			this.inPort.Size = new System.Drawing.Size(41, 20);
			this.inPort.TabIndex = 1;
			this.inPort.Text = "42912";
			this.inPort.Validated += new System.EventHandler(this.inPort_Validated);
			// 
			// labelNickname
			// 
			this.labelNickname.AutoSize = true;
			this.labelNickname.Location = new System.Drawing.Point(6, 118);
			this.labelNickname.Name = "labelNickname";
			this.labelNickname.Size = new System.Drawing.Size(58, 13);
			this.labelNickname.TabIndex = 7;
			this.labelNickname.Text = "Nickname:";
			// 
			// labelServer
			// 
			this.labelServer.AutoSize = true;
			this.labelServer.Location = new System.Drawing.Point(6, 10);
			this.labelServer.Name = "labelServer";
			this.labelServer.Size = new System.Drawing.Size(41, 13);
			this.labelServer.TabIndex = 6;
			this.labelServer.Text = "Server:";
			// 
			// labelName
			// 
			this.labelName.AutoSize = true;
			this.labelName.Location = new System.Drawing.Point(6, 46);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(88, 13);
			this.labelName.TabIndex = 4;
			this.labelName.Text = "Name the server:";
			// 
			// inUsername
			// 
			this.inUsername.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.inUsername.Location = new System.Drawing.Point(102, 187);
			this.inUsername.MaxLength = 255;
			this.inUsername.Name = "inUsername";
			this.inUsername.Size = new System.Drawing.Size(177, 20);
			this.inUsername.TabIndex = 5;
			// 
			// inNickname
			// 
			this.inNickname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.inNickname.Location = new System.Drawing.Point(102, 115);
			this.inNickname.MaxLength = 255;
			this.inNickname.Name = "inNickname";
			this.inNickname.Size = new System.Drawing.Size(177, 20);
			this.inNickname.TabIndex = 4;
			// 
			// inServer
			// 
			this.inServer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.inServer.Location = new System.Drawing.Point(102, 7);
			this.inServer.MaxLength = 255;
			this.inServer.Name = "inServer";
			this.inServer.Size = new System.Drawing.Size(130, 20);
			this.inServer.TabIndex = 0;
			this.inServer.Validated += new System.EventHandler(this.inServer_Validated);
			// 
			// inName
			// 
			this.inName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.inName.Location = new System.Drawing.Point(102, 43);
			this.inName.MaxLength = 255;
			this.inName.Name = "inName";
			this.inName.Size = new System.Drawing.Size(177, 20);
			this.inName.TabIndex = 2;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(225, 287);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(66, 24);
			this.btnCancel.TabIndex = 8;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnConnect
			// 
			this.btnConnect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnConnect.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnConnect.Enabled = false;
			this.btnConnect.Image = global::Gablarski.Clients.Windows.Properties.Resources.ServerConnectImage;
			this.btnConnect.Location = new System.Drawing.Point(12, 287);
			this.btnConnect.Name = "btnConnect";
			this.btnConnect.Size = new System.Drawing.Size(75, 24);
			this.btnConnect.TabIndex = 1;
			this.btnConnect.Text = "Connect";
			this.btnConnect.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnConnect.UseVisualStyleBackColor = true;
			// 
			// settingsButton
			// 
			this.settingsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.settingsButton.Image = global::Gablarski.Clients.Windows.Properties.Resources.SettingsImage;
			this.settingsButton.Location = new System.Drawing.Point(219, 6);
			this.settingsButton.Name = "settingsButton";
			this.settingsButton.Size = new System.Drawing.Size(75, 24);
			this.settingsButton.TabIndex = 12;
			this.settingsButton.Text = "Settings";
			this.settingsButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.settingsButton.UseVisualStyleBackColor = true;
			this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
			// 
			// startLocal
			// 
			this.startLocal.Image = global::Gablarski.Clients.Windows.Properties.Resources.ServerImage;
			this.startLocal.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.startLocal.Location = new System.Drawing.Point(12, 6);
			this.startLocal.Name = "startLocal";
			this.startLocal.Size = new System.Drawing.Size(117, 24);
			this.startLocal.TabIndex = 11;
			this.startLocal.Text = "Start Local Server";
			this.startLocal.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.startLocal.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.startLocal.UseVisualStyleBackColor = true;
			this.startLocal.Click += new System.EventHandler(this.startLocal_Click);
			// 
			// btnSaveServer
			// 
			this.btnSaveServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSaveServer.Image = global::Gablarski.Clients.Windows.Properties.Resources.SaveImage;
			this.btnSaveServer.Location = new System.Drawing.Point(126, 287);
			this.btnSaveServer.Name = "btnSaveServer";
			this.btnSaveServer.Size = new System.Drawing.Size(93, 24);
			this.btnSaveServer.TabIndex = 7;
			this.btnSaveServer.Text = "Save Server";
			this.btnSaveServer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.btnSaveServer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnSaveServer.UseVisualStyleBackColor = true;
			this.btnSaveServer.Visible = false;
			this.btnSaveServer.Click += new System.EventHandler(this.btnSaveServer_Click);
			// 
			// btnEditServer
			// 
			this.btnEditServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnEditServer.Image = global::Gablarski.Clients.Windows.Properties.Resources.ServerEditImage;
			this.btnEditServer.Location = new System.Drawing.Point(126, 287);
			this.btnEditServer.Name = "btnEditServer";
			this.btnEditServer.Size = new System.Drawing.Size(93, 24);
			this.btnEditServer.TabIndex = 4;
			this.btnEditServer.Text = "Edit Server";
			this.btnEditServer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnEditServer.UseVisualStyleBackColor = true;
			this.btnEditServer.Visible = false;
			this.btnEditServer.Click += new System.EventHandler(this.btnEditServer_Click);
			// 
			// btnAddServer
			// 
			this.btnAddServer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAddServer.Image = global::Gablarski.Clients.Windows.Properties.Resources.ServerAddImage;
			this.btnAddServer.Location = new System.Drawing.Point(126, 287);
			this.btnAddServer.Name = "btnAddServer";
			this.btnAddServer.Size = new System.Drawing.Size(93, 24);
			this.btnAddServer.TabIndex = 2;
			this.btnAddServer.Text = "Add Server";
			this.btnAddServer.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.btnAddServer.UseVisualStyleBackColor = true;
			this.btnAddServer.Click += new System.EventHandler(this.btnAddServer_Click);
			// 
			// LoginForm
			// 
			this.AcceptButton = this.btnConnect;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
			this.ClientSize = new System.Drawing.Size(306, 318);
			this.Controls.Add(this.settingsButton);
			this.Controls.Add(this.startLocal);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnSaveServer);
			this.Controls.Add(this.btnEditServer);
			this.Controls.Add(this.pnlModServer);
			this.Controls.Add(this.btnAddServer);
			this.Controls.Add(this.btnConnect);
			this.Controls.Add(this.servers);
			this.MinimumSize = new System.Drawing.Size(285, 300);
			this.Name = "LoginForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Connect to Gablarski";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.LoginForm_FormClosing);
			this.Load += new System.EventHandler(this.LoginForm_Load);
			this.pnlModServer.ResumeLayout(false);
			this.pnlModServer.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.serverStatus)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView servers;
		private System.Windows.Forms.Button btnConnect;
		private System.Windows.Forms.Button btnAddServer;
		private System.Windows.Forms.Panel pnlModServer;
		private System.Windows.Forms.TextBox inPassword;
		private Gablarski.Clients.Windows.NumericRequiredTextBox inPort;
		private System.Windows.Forms.Label labelNickname;
		private System.Windows.Forms.Label labelServer;
		private System.Windows.Forms.Label labelName;
		private System.Windows.Forms.TextBox inUsername;
		private Gablarski.Clients.Windows.RequiredTextBox inNickname;
		private Gablarski.Clients.Windows.RequiredTextBox inServer;
		private Gablarski.Clients.Windows.RequiredTextBox inName;
		private System.Windows.Forms.Label labelPassword;
		private System.Windows.Forms.Label labelUsername;
		private System.Windows.Forms.Button btnEditServer;
		private System.Windows.Forms.Button btnSaveServer;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.TextBox inServerPassword;
		private System.Windows.Forms.Label lblServerPassword;
		private System.Windows.Forms.Button startLocal;
		private System.Windows.Forms.Button settingsButton;
		private System.Windows.Forms.TextBox inPhonetic;
		private System.Windows.Forms.Label lblPhonetic;
		private System.Windows.Forms.Button playPhonetic;
		private System.Windows.Forms.PictureBox serverStatus;
	}
}