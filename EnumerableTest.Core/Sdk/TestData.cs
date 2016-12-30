using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Sdk
{
    /// <summary>
    /// Represents data related to a test.
    /// </summary>
    public abstract class TestData
    {
        internal TestData()
        {
        }

        /// <summary>
        /// Gets an empty data.
        /// </summary>
        public static TestData Empty { get; } =
            new EmptyTestData();
    }

    /// <summary>
    /// Represents an empty data.
    /// </summary>
    public sealed class EmptyTestData
        : TestData
    {
    }

    /// <summary>
    /// Represents a dictionary which contains data related to a test.
    /// </summary>
    public sealed class DictionaryTestData
        : TestData
        , IEnumerable<KeyValuePair<string, object>>
    {
        List<KeyValuePair<string, object>> List { get; }

        DictionaryTestData(List<KeyValuePair<string, object>> list)
        {
            List = list;
        }

        #region IEnumerable
        /// <summary>
        /// Gets an enumerator.
        /// </summary>
        /// <returns></returns>
        public List<KeyValuePair<string, object>>.Enumerator GetEnumerator()
        {
            return List.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        /// <summary>
        /// Represents a builder to create an instance of <see cref="DictionaryTestData"/>.
        /// </summary>
        public struct Builder
        {
            List<KeyValuePair<string, object>> List { get; }

            bool isDisposed;

            /// <summary>
            /// Adds a key-value pair.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <returns></returns>
            public Builder Add(string key, object value)
            {
                if (isDisposed)
                {
                    throw new ObjectDisposedException(nameof(Builder));
                }
                List.Add(new KeyValuePair<string, object>(key, value));
                return this;
            }

            /// <summary>
            /// Creates an instance of <see cref="DictionaryTestData"/>.
            /// </summary>
            /// <returns></returns>
            public DictionaryTestData MakeReadOnly()
            {
                isDisposed = true;
                return new DictionaryTestData(List);
            }

            internal Builder(List<KeyValuePair<string, object>> list)
            {
                List = list;
                isDisposed = false;
            }
        }

        /// <summary>
        /// Creates a builder to create an instance of <see cref="DictionaryTestData"/>.
        /// </summary>
        /// <returns></returns>
        public static Builder Build()
        {
            return new Builder(new List<KeyValuePair<string, object>>());
        }
    }
}
