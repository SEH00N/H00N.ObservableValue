using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace H00N.ObservableValue.Generator
{
    [Generator]
    public sealed class ObservableValueGenerator : IIncrementalGenerator
    {
        private const string AttributeFullName = "H00N.ObservableValue.ObservableValueAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1) Attribute가 붙어 있을 "가능성"이 있는 FieldDeclaration만 먼저 필터링
            IncrementalValuesProvider<ObservableFieldInfo> candidateFields = context.SyntaxProvider
                .CreateSyntaxProvider((node, _) => IsCandidateField(node), (ctx, ct) => TransformField(ctx, ct))
                .Where(info => info != null)
                .Select((info, _) => info);

            // 2) 컴파일 정보와 묶어서 SourceOutput 단계로 전달
            IncrementalValueProvider<(Compilation, ImmutableArray<ObservableFieldInfo>)> compilationAndFields = context.CompilationProvider.Combine(candidateFields.Collect());

            context.RegisterSourceOutput(compilationAndFields, static (spc, source) =>
            {
                (Compilation compilation, ImmutableArray<ObservableFieldInfo> fieldInfoList) = source;

                var documents = new Dictionary<INamedTypeSymbol, StringBuilder>(SymbolEqualityComparer.Default);

                foreach (ObservableFieldInfo fieldInfo in fieldInfoList)
                {
                    IFieldSymbol fieldSymbol = fieldInfo.FieldSymbol;

                    // private 필드 강제
                    if (GeneratorHelper.EnsureIsPrivateFieldRule(spc, fieldSymbol) == false)
                        continue;

                    // 필드 이름 강제
                    if (GeneratorHelper.EnsureFollowFieldNamingRule(spc, fieldSymbol) == false)
                        continue;

                    INamedTypeSymbol containingType = fieldSymbol.ContainingType;
                    if (GeneratorHelper.EnsureIsPartialClassRule(spc, containingType) == false)
                        continue;

                    AttributeData attributeData = fieldInfo.AttributeData;
                    string eventName = attributeData.ConstructorArguments.Length > 0 ? attributeData.ConstructorArguments[0].Value as string : null;
                    string propertyName = attributeData.ConstructorArguments.Length > 1 ? attributeData.ConstructorArguments[1].Value as string : null;

                    propertyName ??= ObservableValueGeneratorFormat.GetDefaultPropertyName(fieldSymbol.Name);
                    eventName ??= ObservableValueGeneratorFormat.GetDefaultEventName(propertyName);

                    // 기존 멤버와 충돌 여부 확인
                    if (GeneratorHelper.EnsureCannotGenerateExistingMemberRule(spc, fieldSymbol, propertyName, eventName) == false)
                        continue;

                    string eventAttributes = FormatAttributes(GetAttributeStrings(attributeData, "EventAttributes", ctorIndex: 2));
                    string propertyAttributes = FormatAttributes(GetAttributeStrings(attributeData, "PropertyAttributes", ctorIndex: 3));

                    if (documents.TryGetValue(containingType, out StringBuilder sb) == false)
                    {
                        sb = new StringBuilder();
                        documents[containingType] = sb;
                    }

                    string typeName = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    sb.AppendLine(ObservableValueGeneratorFormat.GetObservableValueBlock(typeName, eventName, propertyName, fieldSymbol.Name, eventAttributes, propertyAttributes));
                    sb.AppendLine();
                }

                foreach (KeyValuePair<INamedTypeSymbol, StringBuilder> document in documents)
                {
                    INamedTypeSymbol containingType = document.Key;
                    INamespaceSymbol namespaceSymbol = containingType.ContainingNamespace;
                    string namespaceName = namespaceSymbol is { IsGlobalNamespace: true } ? null : namespaceSymbol.ToDisplayString();
                    string hintName = $"{containingType.Name}.ObservableValue.g.cs";

                    spc.AddSource(hintName, ObservableValueGeneratorFormat.GetDocument(namespaceName, containingType.Name, document.Value.ToString()));
                }
            });
        }

        // Attribute가 붙어 있을 가능성이 있는 필드만 1차 필터링
        private static bool IsCandidateField(SyntaxNode node) => node is FieldDeclarationSyntax f && f.AttributeLists.Count > 0;

        // Syntax → Semantic으로 올려서 실제로 [ObservableValue]가 붙은 필드만 추출
        private static ObservableFieldInfo TransformField(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var fieldSyntax = (FieldDeclarationSyntax)context.Node;

            // 하나의 FieldDeclaration에 여러 변수 선언 가능 (예: int a, b;)
            // 지금은 Skeleton이니까 첫 번째 변수만 보고, 나중에 확장해도 됨.
            if (fieldSyntax.Declaration.Variables.Count == 0)
                return null;

            VariableDeclaratorSyntax variable = fieldSyntax.Declaration.Variables[0];
            if (context.SemanticModel.GetDeclaredSymbol(variable, cancellationToken) is not IFieldSymbol fieldSymbol)
                return null;

            // [ObservableValue] Attribute가 붙었는지 확인
            foreach (var attributeData in fieldSymbol.GetAttributes())
            {
                INamedTypeSymbol attrClass = attributeData.AttributeClass;
                if (attrClass is null)
                    continue;

                string fullName = attrClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // global::H00N.ObservableValue.ObservableValueAttribute
                if (fullName == "global::" + AttributeFullName)
                {
                    INamedTypeSymbol containingType = fieldSymbol.ContainingType;
                    return new ObservableFieldInfo(fieldSymbol, containingType, attributeData);
                }
            }

            return null;
        }

        private static string[] GetAttributeStrings(AttributeData attributeData, string name, int ctorIndex)
        {
            foreach (KeyValuePair<string, TypedConstant> item in attributeData.NamedArguments)
            {
                if (item.Key == name)
                    return ToStrings(item.Value);
            }

            if (attributeData.ConstructorArguments.Length > ctorIndex)
                return ToStrings(attributeData.ConstructorArguments[ctorIndex]);

            return Array.Empty<string>();
        }

        private static string[] ToStrings(TypedConstant constant)
        {
            if (constant.Kind == TypedConstantKind.Array && !constant.IsNull)
                return constant.Values.Select(v => v.Value as string).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            return Array.Empty<string>();
        }

        private static string FormatAttributes(IEnumerable<string> attributes)
        {
            if (attributes is null)
                return string.Empty;

            string[] items = attributes.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
            if (items.Length == 0)
                return string.Empty;

            return string.Join("\n    ", items.Select(a => $"[{a}]")) + "\n    ";
        }

        private class ObservableFieldInfo
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
}
