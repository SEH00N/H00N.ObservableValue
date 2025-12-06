using System;

namespace H00N.ObservableValue
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ObservableValueAttribute : Attribute
    {
        public ObservableValueAttribute(string eventName = null, string propertyName = null, string[] eventAttributes = null, string[] propertyAttributes = null) { }
    }
}
