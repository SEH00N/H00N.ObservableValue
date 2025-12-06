using Microsoft.CodeAnalysis;

namespace H00N.ObservableValue.Generator
{
    public class ObservableFieldInfo
    {
        public IFieldSymbol FieldSymbol { get; }
        public INamedTypeSymbol ContainingType { get; }
        public AttributeData AttributeData { get; }

        public ObservableFieldInfo(IFieldSymbol fieldSymbol, INamedTypeSymbol containingType, AttributeData attributeData)
        {
            FieldSymbol = fieldSymbol;
            ContainingType = containingType;
            AttributeData = attributeData;
        }
    }
}