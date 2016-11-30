using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EnumerableTest.Sdk
{
    /// <summary>
    /// Represents a value returned by a property.
    /// </summary>
    [Serializable]
    public sealed class MarshalResultValue
        : MarshalResult
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        public MarshalValue MarshalValue { get; }

        /// <summary>
        /// Invokes <paramref name="onValue"/>.
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="onValue"></param>
        /// <param name="onException"></param>
        /// <returns></returns>
        public override X Match<X>(Func<MarshalValue, X> onValue, Func<Exception, X> onException)
        {
            return onValue(MarshalValue);
        }

        internal MarshalResultValue(MarshalValue value)
        {
            MarshalValue = value;
        }
    }

    /// <summary>
    /// Represents an exception thrown by a property.
    /// </summary>
    [Serializable]
    public sealed class MarshalResultException
        : MarshalResult
    {
        /// <summary>
        /// Gets the exception.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Invokes <paramref name="onException"/>.
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="onValue"></param>
        /// <param name="onException"></param>
        /// <returns></returns>
        public override X Match<X>(Func<MarshalValue, X> onValue, Func<Exception, X> onException)
        {
            return onException(Exception);
        }

        internal MarshalResultException(Exception exception)
        {
            Exception = exception;
        }
    }

    /// <summary>
    /// Represents a marshal value or an exception.
    /// </summary>
    [Serializable]
    public abstract class MarshalResult
    {
        /// <summary>
        /// Determines the runtime type of this and evaluates a function for it.
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="onValue"></param>
        /// <param name="onException"></param>
        /// <returns></returns>
        public abstract X Match<X>(Func<MarshalValue, X> onValue, Func<Exception, X> onException);

        internal static MarshalResult Create(Func<MarshalValue> create)
        {
            try
            {
                return new MarshalResultValue(create());
            }
            catch (Exception exception)
            {
                return new MarshalResultException(exception);
            }
        }
    }

    /// <summary>
    /// Represents a marshal value.
    /// </summary>
    [Serializable]
    public sealed class MarshalValue
    {
        /// <summary>
        /// Gets the name of the type of the object.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Gets a string which represents the object.
        /// </summary>
        public string String { get; }

        /// <summary>
        /// Gets the properties of the object.
        /// </summary>
        public KeyValuePair<string, MarshalResult>[] Properties { get; }

        /// <summary>
        /// See <see cref="String"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String;
        }

        static KeyValuePair<string, MarshalResult>[] EmptyProperties { get; } =
            new KeyValuePair<string, MarshalResult>[] { };

        static KeyValuePair<string, MarshalResult> CreateProperty(string name, Func<MarshalValue> get)
        {
            return KeyValuePair.Create(name, MarshalResult.Create(get));
        }

        MarshalValue(string typeName, string @string, KeyValuePair<string, MarshalResult>[] properties)
        {
            TypeName = typeName;
            String = @string;
            Properties = properties;
        }

        MarshalValue(string typeName, string @string)
            : this(typeName, @string, EmptyProperties)
        {
        }

        static MarshalValue Null { get; } =
            new MarshalValue("null", "null");

        static MarshalValue FromObject(object obj, int maxLevel)
        {
            if (ReferenceEquals(obj, null)) return Null;

            var type = obj.GetType();
            if (maxLevel <= 0)
            {
                return new MarshalValue(type.FullName, obj.ToString());
            }

            var fromObject = new Func<object, MarshalValue>(o => FromObject(o, maxLevel - 1));
            var enumerable = obj as IEnumerable;
            if (enumerable != null && !(obj is string))
            {
                var items = new List<KeyValuePair<string, MarshalResult>>();
                var i = 0;
                foreach (var element in enumerable)
                {
                    var propertyName = string.Concat("[", i, "]");
                    items.Add(CreateProperty(propertyName, () => fromObject(element)));
                    i++;
                }
                var join =
                    string.Join(
                        ", ",
                        items.Select(item => item.Value.Match(v => v.String, e => "!"))
                    );
                var @string = string.Concat("{", join, "}");
                return new MarshalValue(type.FullName, @string, items.ToArray());
            }
            else
            {
                var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
                var properties =
                    type.GetProperties(bindingFlags)
                    .Where(pi => pi.GetMethod != null)
                    .Select(pi => CreateProperty(pi.Name, () => fromObject(pi.GetValue(obj))));
                var fields =
                    type.GetFields(bindingFlags)
                    .Select(fi => CreateProperty(fi.Name, () => fromObject(fi.GetValue(obj))));
                var items = properties.Concat(fields).ToArray();
                return new MarshalValue(type.FullName, obj.ToString(), items);
            }
        }

        /// <summary>
        /// Gets or sets the number of recursion.
        /// </summary>
        public static int Recursion { get; set; }

        /// <summary>
        /// Creates an instance which reprensents an object.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="shallow"></param>
        /// <returns></returns>
        public static MarshalValue FromObject(object obj, bool shallow)
        {
            return FromObject(obj, shallow ? 0 : Recursion);
        }
    }
}
