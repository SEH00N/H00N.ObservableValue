namespace ObservableValue.Generator
{
    public static class ObservableValueGeneratorFormat
    {
        public static string GetDefaultEventName(string fieldName)
        {
            return $"On{fieldName}Changed";
        }

        public static string GetDefaultPropertyName(string fieldName)
        {
            return fieldName;
        }

        public static string GetObservableValueBlock(string typeName, string eventName, string propertyName, string fieldName)
        {
            return 
@$"
public event Action<{typeName}, {typeName}> {eventName};
public {typeName} {propertyName} 
{{ 
    get => {fieldName};
    set
    {{
        if (EqualityComparer<{typeName}>.Default.Equals({fieldName}, value))
            return;

        {typeName} oldValue = {fieldName};
        {fieldName} = value;

        {eventName}?.Invoke(oldValue, value);
    }}
}}
";
        }

        public static string GetDocument(string namespaceName, string className, string content)
        {
            return
@$"
using System;

namespace {namespaceName}
{{
    partial class {className}
    {{
        {content}
    }}
}}
";
        }
    }
}