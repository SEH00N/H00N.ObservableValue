using System.Linq;

namespace H00N.ObservableValue.Generator
{
    public static class ObservableValueGeneratorFormat
    {
        public static string GetDefaultEventName(string propertyName)
        {
            return $"On{propertyName}ChangedEvent";
        }

        public static string GetDefaultPropertyName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return fieldName;

            string trimmed = fieldName[0] == '_' ? fieldName.Substring(1) : fieldName;
            if (trimmed.Length == 0)
                return fieldName;

            char first = char.ToUpper(trimmed[0]);
            return trimmed.Length == 1 ? first.ToString() : first + trimmed.Substring(1);
        }

        public static string GetObservableValueBlock(string typeName, string eventName, string propertyName, string fieldName)
        {
            return
@$"
    public event global::System.Action<{typeName}, {typeName}> {eventName};
    public {typeName} {propertyName} 
    {{ 
        get => {fieldName};
        set
        {{
            if (global::System.Collections.Generic.EqualityComparer<{typeName}>.Default.Equals({fieldName}, value))
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
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return
@$"
partial class {className}
{{
{content}
}}
";
            }

            string indentedContent = Indent(content, "    ");
            return
@$"
namespace {namespaceName}
{{
    partial class {className}
    {{
{indentedContent}
    }}
}}
";
        }

        private static string Indent(string text, string indent)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return string.Join("\n", text.Split('\n').Select(line => line.Length == 0 ? line : indent + line));
        }
    }
}
