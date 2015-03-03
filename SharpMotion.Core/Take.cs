using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;

namespace SharpMotion.Core
{
    public class Take : IImageSequence, IList<Bitmap>, INotifyCollectionChanged
    {
        private List<Bitmap> _images = new List<Bitmap>();

        public IEnumerable<Bitmap> Images
        {
            get { return _images; }
        }

        public IEnumerator<Bitmap> GetEnumerator()
        {
            return _images.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _images).GetEnumerator();
        }

        public void Add(Bitmap item)
        {
            _images.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Clear()
        {
            _images.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(Bitmap item)
        {
            return _images.Contains(item);
        }

        public void CopyTo(Bitmap[] array, int arrayIndex)
        {
            _images.CopyTo(array, arrayIndex);
        }

        public bool Remove(Bitmap item)
        {
            var result =  _images.Remove(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return result;
        }

        public int Count
        {
            get { return _images.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public int IndexOf(Bitmap item)
        {
            return _images.IndexOf(item);
        }

        public void Insert(int index, Bitmap item)
        {
            _images.Insert(index, item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void RemoveAt(int index)
        {
            _images.RemoveAt(index);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public Bitmap this[int index]
        {
            get { return _images[index]; }
            set
            {
                _images[index] = value;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var handler = CollectionChanged;
            if (handler != null) handler(this, e);
        }
    }
}
