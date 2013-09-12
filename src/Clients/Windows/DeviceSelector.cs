﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gablarski.Audio;
using Gablarski.Clients;
using Gablarski.Clients.Windows;

namespace Gablarski
{
	public partial class DeviceSelector
		: UserControl
	{
		public DeviceSelector ()
		{
			InitializeComponent ();
		}

		public string ProviderLabel
		{
			get { return this.label1.Text; }
			set { this.label1.Text = value; }
		}

		public string DeviceLabel
		{
			get { return this.label2.Text; }
			set { this.label2.Text = value; }
		}

		public IEnumerable<IAudioDeviceProvider> ProviderSource
		{
			set
			{
				IList<IAudioDeviceProvider> pvalue = value.ToList ();
				this.provider.DataSource = pvalue;
				if (pvalue.Count == 1)
					this.provider.SelectedItem = pvalue[0];
			}
		}

		public IAudioDeviceProvider Provider
		{
			get { return (provider.SelectedItem != null) ? (IAudioDeviceProvider)provider.SelectedItem : null; }
		}

		public IAudioDevice Device
		{
			get { return (device.SelectedItem != null) ? (IAudioDevice)device.SelectedItem : null; }
		}

		public void SetProvider (string providerName)
		{
			if (String.IsNullOrEmpty (providerName))
				return;

			provider.SelectedItem = provider.Items.Cast<IAudioDeviceProvider>().FirstOrDefault (p => p.GetType().GetSimpleName() == providerName)
									?? provider.Items.Cast<IAudioDeviceProvider>().FirstOrDefault();

			provider_SelectedIndexChanged (provider, EventArgs.Empty);
		}

		public void SetDevice (string deviceName)
		{
			if (String.IsNullOrEmpty (deviceName))
				return;
			
			if (deviceName == "Default")
			{
				device.SelectedItem = this.defaultDevice;
				return;
			}

			device.SelectedItem = device.Items.Cast<IAudioDevice>().FirstOrDefault (d => d.Name == deviceName)
									?? this.defaultDevice;
		}

		private readonly DefaultDevice defaultDevice = new DefaultDevice();

		private void provider_SelectedIndexChanged (object sender, EventArgs e)
		{
			this.device.DataSource = null;

			if (this.provider.SelectedItem == null)
				this.device.Enabled = false;
			else
			{
				this.device.Enabled = true;
				try
				{
					var devices = Provider.GetDevices().ToList();
					devices.Insert (0, this.defaultDevice);
					this.device.DataSource = devices;
					this.device.SelectedItem = this.defaultDevice;
				}
				catch
				{
					this.provider.SelectedItem = null;
				}
			}
		}

		private class DefaultDevice
			: IAudioDevice
		{
			public string Name
			{
				get { return "Default"; }
			}

			public void Dispose()
			{
			}
		}
	}
}