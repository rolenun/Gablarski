﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Windows.Data;

namespace Gablarski.Clients
{
	public sealed class LambdaConverter<TSource, TValue>
		: IValueConverter
	{
		public LambdaConverter (Func<TSource, TValue> convert, Func<TValue, TSource> convertBack)
		{
			if (convert == null)
				throw new ArgumentNullException ("convert");
			if (convertBack == null)
				throw new ArgumentNullException ("convertBack");

			this.convert = convert;
			this.convertBack = convertBack;
		}

		public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			return this.convert ((TSource) value);
		}

		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
		{
			return this.convertBack ((TValue)value);
		}

		private readonly Func<TSource, TValue> convert;
		private readonly Func<TValue, TSource> convertBack;
	}

	public sealed class CollectionView<T>
		: INotifyCollectionChanged, IReadOnlyList<T>
	{
		public CollectionView (IEnumerable itemSource)
		{
			if (itemSource == null)
				throw new ArgumentNullException ("itemSource");

			this.syncContext = SynchronizationContext.Current;
			this.itemSource = itemSource;

			var incc = itemSource as INotifyCollectionChanged;
			if (incc != null)
				incc.CollectionChanged += OnCollectionChanged;

			Reset();
		}

		public CollectionView (IEnumerable itemSource, IValueConverter itemConverter)
		{
			if (itemConverter == null)
				throw new ArgumentNullException ("itemConverter");
			if (itemSource == null)
				throw new ArgumentNullException ("itemSource");

			this.syncContext = SynchronizationContext.Current;
			this.itemSource = itemSource;

			var incc = itemSource as INotifyCollectionChanged;
			if (incc != null)
				incc.CollectionChanged += OnCollectionChanged;

			ItemConverter = itemConverter;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public IValueConverter ItemConverter
		{
			get { return this.itemConverter; }
			set
			{
				if (this.itemConverter == value)
					return;

				this.itemConverter = value;
				Reset();
			}
		}

		public Func<object, bool> ItemFilter
		{
			get { return this.itemFilter; }
			set
			{
				if (this.itemFilter == value)
					return;

				this.itemFilter = value;
				Reset();
			}
		}

		public int Count
		{
			get { return this.items.Count; }
		}

		public T this [int index]
		{
			get { return this.items[index]; }
		}

		public IEnumerator<T> GetEnumerator()
		{
			return this.items.GetEnumerator();
		}

		private SynchronizationContext syncContext;
		private readonly ObservableCollection<T> items = new ObservableCollection<T>();
		private readonly IEnumerable itemSource;
		private IValueConverter itemConverter;
		private Func<object, bool> itemFilter;

		private void OnCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			SendOrPostCallback action = s => {
				var args = (NotifyCollectionChangedEventArgs) s;
				switch (e.Action) {
					case NotifyCollectionChangedAction.Add:
						if (e.NewStartingIndex == -1)
							goto case NotifyCollectionChangedAction.Reset;

						List<T> converted = new List<T> (e.NewItems.Count);

						for (int i = 0; i < e.NewItems.Count; i++) {
							object element = e.NewItems[i];
							AttachListener (element);

							if (ItemConverter != null)
								element = ItemConverter.Convert (element, typeof (T), null, null);

							converted.Add ((T) element);
							this.items.Insert (e.NewStartingIndex + i, (T) element);
						}

						args = new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, converted, e.NewStartingIndex);

						break;

					default:
					case NotifyCollectionChangedAction.Reset:
						Reset();
						args = new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset);
						break;
				}

				OnCollectionChanged (args);
			};


			if (this.syncContext != null)
				this.syncContext.Post (action, e);
			else
				action (e);
		}

		private void OnCollectionChanged (NotifyCollectionChangedEventArgs e)
		{
			var handler = CollectionChanged;
			if (handler != null)
				handler (this, e);
		}

		private void Reset()
		{
			BindingOperations.AccessCollection (this.itemSource, ResetCore, false);
		}

		private void ResetCore()
		{
			foreach (T item in this.items)
				DetatchListener (item);

			this.items.Clear();

			foreach (object item in this.itemSource) {
				object element = item;
				AttachListener (element);

				if (ItemConverter != null)
					element = ItemConverter.Convert (element, typeof(T), null, null);

				this.items.Add ((T)element);
			}
		}

		private void DetatchListener (T item)
		{
			var inpc = item as INotifyPropertyChanged;
			if (inpc != null)
				inpc.PropertyChanged -= OnItemPropertyChanged;
		}

		private void OnItemPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			Reset();// TODO
		}

		private void AttachListener (object item)
		{
			if (ItemFilter == null)
				return;

			var inpc = item as INotifyPropertyChanged;
			if (inpc != null)
				inpc.PropertyChanged += OnItemPropertyChanged;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}