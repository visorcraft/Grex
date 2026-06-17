using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Grex.Models
{
    /// <summary>
    /// An ObservableCollection that can add or replace many items while raising only
    /// a single CollectionChanged(Reset) event. This avoids freezing the UI dispatcher
    /// when large result sets are bound to a ListView.
    /// </summary>
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotifications;

        public RangeObservableCollection()
        {
        }

        public RangeObservableCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        /// <summary>
        /// Adds a range of items and raises a single Reset event at the end.
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var list = items.ToList();
            if (list.Count == 0)
            {
                return;
            }

            _suppressNotifications = true;
            try
            {
                foreach (var item in list)
                {
                    Add(item);
                }
            }
            finally
            {
                _suppressNotifications = false;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Replaces the entire contents with the supplied items and raises a single Reset event.
        /// </summary>
        public void Reset(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _suppressNotifications = true;
            try
            {
                Clear();
                foreach (var item in items)
                {
                    Add(item);
                }
            }
            finally
            {
                _suppressNotifications = false;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotifications)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}
