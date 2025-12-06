using System;

namespace ObservableValue
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ObservableValueAttribute : Attribute
    {
        public string EventName { get; private set; }
        public string PropertyName { get; private set; }
        public string[] EventAttributes { get; private set; }
        public string[] PropertyAttributes { get; private set; }

        public ObservableValueAttribute(string eventName = null, string propertyName = null, string[] eventAttributes = null, string[] propertyAttributes = null)
        {
            EventName = eventName;
            PropertyName = propertyName;
            EventAttributes = eventAttributes;
            PropertyAttributes = propertyAttributes;
        }
    }
}
