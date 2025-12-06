using Microsoft.CodeAnalysis;

namespace H00N.ObservableValue.Generator
{
    public sealed class ObservableFieldInfoResult
    {
        public ObservableFieldInfo FieldInfo { get; }
        public Diagnostic Diagnostic { get; }

        public ObservableFieldInfoResult(ObservableFieldInfo fieldInfo, Diagnostic diagnostic)
        {
            FieldInfo = fieldInfo;
            Diagnostic = diagnostic;
        }
    }
}
