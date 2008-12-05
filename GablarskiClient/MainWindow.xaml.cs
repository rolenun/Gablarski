﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GablarskiClient
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow ()
		{
			InitializeComponent ();
		}

		private void connectButton_Click(object sender, RoutedEventArgs e)
		{
			Gablarski.Client.GablarskiClient client = new Gablarski.Client.GablarskiClient();
			client.Connected += client_Connected;
			client.Disconnected += client_Disconnected;
			client.LoggedIn += client_LoggedIn;
			client.Connect (this.host.Text, 6112);
			client.Login (this.nickname.Text);
		}

		void client_LoggedIn (object sender, Gablarski.ConnectionEventArgs e)
		{
			this.SetStatusImage ("./Resources/key.png");
		}

		private void client_Disconnected (object sender, Gablarski.ReasonEventArgs e)
		{
			this.SetStatusImage ("./Resources/disconnect.png");
		}

		private void client_Connected (object sender, Gablarski.ConnectionEventArgs e)
		{
			this.SetStatusImage ("./Resources/connect.png");
		}

		private void SetStatusImage (string uri)
		{
			this.Dispatcher.BeginInvoke((Action)delegate
			{
				this.statusImage.Source =
					new BitmapImage(new Uri(uri, UriKind.Relative));
			});
		}
	}
}