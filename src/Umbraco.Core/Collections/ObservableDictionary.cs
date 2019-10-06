﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace Umbraco.Core.Collections
{
    /// <summary>
    /// An ObservableDictionary
    /// </summary>
    /// <remarks>
    /// Assumes that the key will not change and is unique for each element in the collection.
    /// Collection is not thread-safe, so calls should be made single-threaded.
    /// </remarks>
    /// <typeparam name="TValue">The type of elements contained in the BindableCollection</typeparam>
    /// <typeparam name="TKey">The type of the indexing key</typeparam>
    public class ObservableDictionary<TKey, TValue> : ObservableCollection<TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue>
    {
        protected Dictionary<TKey, int> Indecies { get; }
        protected Func<TValue, TKey> KeySelector { get; }

        /// <summary>
        /// Create new ObservableDictionary
        /// </summary>
        /// <param name="keySelector">Selector function to create key from value</param>
        /// <param name="equalityComparer">The equality comparer to use when comparing keys, or null to use the default comparer.</param>
        public ObservableDictionary(Func<TValue, TKey> keySelector, IEqualityComparer<TKey> equalityComparer = null)
        {
            KeySelector = keySelector ?? throw new ArgumentException(nameof(keySelector));
            Indecies = new Dictionary<TKey, int>(equalityComparer);
        }

        #region Protected Methods

        protected override void InsertItem(int index, TValue item)
        {
            var key = KeySelector(item);
            if (Indecies.ContainsKey(key))
                throw new ArgumentException($"An element with the same key '{key}' already exists in the dictionary.", nameof(item));

            if (index != Count)
            {
                foreach (var k in Indecies.Keys.Where(k => Indecies[k] >= index).ToList())
                {
                    Indecies[k]++;
                }
            }

            base.InsertItem(index, item);
            Indecies[key] = index;
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            Indecies.Clear();
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            var key = KeySelector(item);

            base.RemoveItem(index);

            Indecies.Remove(key);

            foreach (var k in Indecies.Keys.Where(k => Indecies[k] > index).ToList())
            {
                Indecies[k]--;
            }
        }

        #endregion

        public bool ContainsKey(TKey key)
        {
            return Indecies.ContainsKey(key);
        }

        /// <summary>
        /// Gets or sets the element with the specified key.  If setting a new value, new value must have same key.
        /// </summary>
        /// <param name="key">Key of element to replace</param>
        /// <returns></returns>
        public TValue this[TKey key]
        {

            get => this[Indecies[key]];
            set
            {
                //confirm key matches
                if (!KeySelector(value).Equals(key))
                    throw new InvalidOperationException("Key of new value does not match.");

                if (!Indecies.ContainsKey(key))
                {
                    Add(value);
                }
                else
                {
                    this[Indecies[key]] = value;
                }
            }
        }

        /// <summary>
        /// Replaces element at given key with new value.  New value must have same key.
        /// </summary>
        /// <param name="key">Key of element to replace</param>
        /// <param name="value">New value</param>
        ///
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns>False if key not found</returns>
        public bool Replace(TKey key, TValue value)
        {
            if (!Indecies.ContainsKey(key)) return false;

            //confirm key matches
            if (!KeySelector(value).Equals(key))
                throw new InvalidOperationException("Key of new value does not match.");

            this[Indecies[key]] = value;
            return true;

        }

        public void ReplaceAll(IEnumerable<TValue> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            Clear();

            foreach (var value in values)
            {
                Add(value);
            }
        }

        public bool Remove(TKey key)
        {
            if (!Indecies.ContainsKey(key)) return false;

            RemoveAt(Indecies[key]);
            return true;

        }

        /// <summary>
        /// Allows us to change the key of an item
        /// </summary>
        /// <param name="currentKey"></param>
        /// <param name="newKey"></param>
        public void ChangeKey(TKey currentKey, TKey newKey)
        {
            if (!Indecies.ContainsKey(currentKey))
            {
                throw new InvalidOperationException($"No item with the key '{currentKey}' was found in the dictionary.");
            }

            if (ContainsKey(newKey))
            {
                throw new ArgumentException($"An element with the same key '{newKey}' already exists in the dictionary.", nameof(newKey));
            }

            var currentIndex = Indecies[currentKey];

            Indecies.Remove(currentKey);
            Indecies.Add(newKey, currentIndex);
        }

        #region IDictionary and IReadOnlyDictionary implementation

        public bool TryGetValue(TKey key, out TValue val)
        {
            if (Indecies.TryGetValue(key, out var index))
            {
                val = this[index];
                return true;
            }
            val = default;
            return false;
        }

        /// <summary>
        /// Returns all keys
        /// </summary>
        public IEnumerable<TKey> Keys => Indecies.Keys;

        /// <summary>
        /// Returns all values
        /// </summary>
        public IEnumerable<TValue> Values => base.Items;

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Indecies.Keys;

        //this will never be used
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values.ToList();

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            foreach (var i in Values)
            {
                var key = KeySelector(i);
                yield return new KeyValuePair<TKey, TValue>(key, i);
            }
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            Add(value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        #endregion

        /// <summary>
        /// The exception that is thrown when a duplicate key inserted.
        /// </summary>
        /// <seealso cref="System.Collections.ObjectModel.ObservableCollection{TValue}" />
        /// <seealso cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}" />
        /// <seealso cref="System.Collections.Generic.IDictionary{TKey, TValue}" />
        [Obsolete("Throw an ArgumentException when trying to add a duplicate key instead.")]
        [Serializable]
        internal class DuplicateKeyException : Exception
        {
            /// <summary>
            /// Gets the key.
            /// </summary>
            /// <value>
            /// The key.
            /// </value>
            public string Key { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="DuplicateKeyException" /> class.
            /// </summary>
            public DuplicateKeyException()
            { }

            /// <summary>
            /// Initializes a new instance of the <see cref="DuplicateKeyException" /> class.
            /// </summary>
            /// <param name="key">The key.</param>
            public DuplicateKeyException(string key)
                : this(key, null)
            { }

            /// <summary>
            /// Initializes a new instance of the <see cref="DuplicateKeyException" /> class.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
            public DuplicateKeyException(string key, Exception innerException)
                : base("Attempted to insert duplicate key \"" + key + "\" in collection.", innerException)
            {
                Key = key;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="DuplicateKeyException" /> class.
            /// </summary>
            /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
            protected DuplicateKeyException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
                Key = info.GetString(nameof(Key));
            }

            /// <summary>
            /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
            /// </summary>
            /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
            /// <exception cref="ArgumentNullException">info</exception>
            public override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException(nameof(info));
                }

                info.AddValue(nameof(Key), Key);

                base.GetObjectData(info, context);
            }
        }
    }
}
