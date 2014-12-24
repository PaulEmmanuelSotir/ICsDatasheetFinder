using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace ICsDatasheetFinder.Common
{
	// TODO : refaire IncrementalLoadingBase de manière plus adaptée à mon usage ( et enlever tout les trucs inutiles )
	// This class can used as a jumpstart for implementing ISupportIncrementalLoading. 
	// Implementing the ISupportIncrementalLoading interfaces allows you to create a list that loads
	//  more data automatically when the user scrolls to the end of of a GridView or ListView.
	public abstract class IncrementalLoadingBase : IList, ISupportIncrementalLoading, INotifyCollectionChanged, IDisposable
	{
		#region IList

		public int Add(object value)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(object value)
		{
			return _storage.Contains(value);
		}

		public int IndexOf(object value)
		{
			return _storage.IndexOf(value);
		}

		public void Insert(int index, object value)
		{
			throw new NotImplementedException();
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public void Remove(object value)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public object this[int index]
		{
			get
			{
				return _storage[index];
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public void CopyTo(Array array, int index)
		{
			((IList)_storage).CopyTo(array, index);
		}

		public int Count
		{
			get { return _storage.Count; }
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerator GetEnumerator()
		{
			return _storage.GetEnumerator();
		}

		#endregion

		#region ISupportIncrementalLoading

		public bool HasMoreItems
		{
			get { return HasMoreItemsOverride(); }
		}

		public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			return AsyncInfo.Run((c) => LoadMoreItemsAsync(c, count));
		}

		#endregion

		#region INotifyCollectionChanged

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion

		#region Private methods

		async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
		{
			try
			{
				// avoid any other thread to load more items at the same time ( threads will be called in queue when the previous thread called Release() )
				await _semaphoreSlim.WaitAsync();
				// TODO : enlever ça si l'exception n'est jammais levée
				if (_busy)
				{
					throw new InvalidOperationException("Only one operation in flight at a time");
				}

				_busy = true;

				var items = await LoadMoreItemsOverrideAsync(c, count);
				var baseIndex = _storage.Count;

				_storage.AddRange(items);

				// Now notify of the new items
				NotifyOfInsertedItems(baseIndex, items.Count);

				return new LoadMoreItemsResult { Count = (uint)items.Count };
			}
			finally
			{
				_busy = false;
				_semaphoreSlim.Release();
			}
		}

		void NotifyOfInsertedItems(int baseIndex, int count)
		{
			if (CollectionChanged == null)
			{
				return;
			}

			for (int i = 0; i < count; i++)
			{
				var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _storage[i + baseIndex], i + baseIndex);
				CollectionChanged(this, args);
			}
		}

		protected void NotifyOfReset()
		{
			if (CollectionChanged == null)
			{
				return;
			}

			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
			CollectionChanged(this, args);
		}

		#endregion

		#region Overridable methods

		protected abstract Task<IList<object>> LoadMoreItemsOverrideAsync(CancellationToken c, uint count);
		protected abstract bool HasMoreItemsOverride();

		#endregion

		#region State

		protected List<object> _storage = new List<object>();
		protected bool _busy = false;
		private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
		private bool disposed = false;

		#endregion

		#region IDisposable

		///<remarks>Do not make this method virtual. A derived class should not be able to override this method.</remarks> 
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		///<param name="disposing"> If disposing equals true, the method has been called directly or indirectly by a user's code. Managed and unmanaged resources can be disposed. 
		/// If disposing equals false, the method has been called by the runtime from inside the finalizer and you should not reference
		/// other objects. Only unmanaged resources can be disposed.</param>
		/// <see cref="http://joeduffyblog.com/2005/04/08/dg-update-dispose-finalization-and-resource-management/"/>
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if (disposing)
				{
					// MANAGED RESSOURCES
					_semaphoreSlim.Dispose();
				}

				// UNMANAGED RESSOURCES
				//...

				disposed = true;
			}
		}

		///<<summary> This destructor will run only if the Dispose method does not get called.</summary>
		///<remarks>Do not provide destructors in types derived from this class.</remarks>
		~IncrementalLoadingBase()
		{
			// Do not re-create Dispose clean-up code here.
			// Calling Dispose(false) is optimal in terms of
			// readability and maintainability.
			Dispose(false);
		}

		#endregion
	}
}
