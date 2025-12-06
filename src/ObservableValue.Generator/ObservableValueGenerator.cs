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

        private static readonly DiagnosticDescriptor MustBePrivateField = new DiagnosticDescriptor(
            id: "OBS001",
            title: "ObservableValue can only be used on private fields",
            messageFormat: "Field '{0}' must be private",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor MustFollowFieldNaming = new DiagnosticDescriptor(
            id: "OBS002",
            title: "ObservableValue fields must start with '_' or a lower-case letter",
            messageFormat: "Field '{0}' must start with '_' or a lower-case letter",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor CannotGenerateExistingMember = new DiagnosticDescriptor(
            id: "OBS003",
            title: "ObservableValue cannot overwrite existing members",
            messageFormat: "Field '{0}' cannot generate members ('{1}', '{2}') because they already exist",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor MustBePartialClass = new DiagnosticDescriptor(
            id: "OBS004",
            title: "ObservableValue can only be used on partial classes",
            messageFormat: "Class '{0}' must be partial",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1) Attribute가 붙어 있을 "가능성"이 있는 FieldDeclaration만 먼저 필터링
            IncrementalValuesProvider<IEnumerable<ObservableFieldInfo>> candidateFields = context.SyntaxProvider
                .CreateSyntaxProvider((node, _) => IsCandidateField(node), (ctx, ct) => TransformField(ctx, ct))
                .Where(info => info != null)
                .Select((info, _) => info);

            // 2) 컴파일 정보와 묶어서 SourceOutput 단계로 전달
            IncrementalValueProvider<(Compilation, ImmutableArray<IEnumerable<ObservableFieldInfo>>)> compilationAndFields = context.CompilationProvider.Combine(candidateFields.Collect());

            context.RegisterSourceOutput(compilationAndFields, static (spc, source) =>
            {
                (Compilation compilation, ImmutableArray<IEnumerable<ObservableFieldInfo>> fieldInfosList) = source;

                var documents = new Dictionary<INamedTypeSymbol, StringBuilder>(SymbolEqualityComparer.Default);

                foreach (IEnumerable<ObservableFieldInfo> fieldInfos in fieldInfosList)
                {
                    int index = -1;
                    foreach (ObservableFieldInfo fieldInfo in fieldInfos)
                    {
                        index++;
                        IFieldSymbol fieldSymbol = fieldInfo.FieldSymbol;

                        // private 필드 강제
                        if (fieldSymbol.DeclaredAccessibility != Accessibility.Private)
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(
                                MustBePrivateField,
                                fieldSymbol.Locations[index],
                                fieldSymbol.Name));

                            continue;
                        }

                        string fieldName = fieldSymbol.Name;
                        bool startsWithUnderscore = fieldName.StartsWith("_");
                        bool startsWithLower = fieldName.Length > 0 && char.IsLower(fieldName[0]);
                        bool followsNaming = startsWithUnderscore || startsWithLower;

                        if (!followsNaming)
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(
                                MustFollowFieldNaming,
                                fieldSymbol.Locations[index],
                                fieldSymbol.Name));

                            continue;
                        }

                        AttributeData attributeData = fieldInfo.AttributeData;
                        string typeName = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        INamedTypeSymbol containingType = fieldSymbol.ContainingType;

                        bool isPartial = containingType
                            .DeclaringSyntaxReferences
                            .Select(r => r.GetSyntax() as TypeDeclarationSyntax)
                            .Any(t => t != null && t.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

                        if (!isPartial)
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(
                                MustBePartialClass,
                                fieldSymbol.Locations[index],
                                containingType.Name));

                            continue;
                        }

                        string eventName = attributeData.ConstructorArguments.Length > 0 ? attributeData.ConstructorArguments[0].Value as string : null;
                        string propertyName = attributeData.ConstructorArguments.Length > 1 ? attributeData.ConstructorArguments[1].Value as string : null;

                        propertyName ??= ObservableValueGeneratorFormat.GetDefaultPropertyName(fieldName);
                        eventName ??= ObservableValueGeneratorFormat.GetDefaultEventName(propertyName);

                        // 기존 멤버와 충돌 여부 확인
                        bool propertyExists = containingType.GetMembers(propertyName).Length > 0;
                        bool eventExists = containingType.GetMembers(eventName).Length > 0;
                        if (propertyExists || eventExists)
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(
                                CannotGenerateExistingMember,
                                fieldSymbol.Locations[index],
                                fieldSymbol.Name,
                                propertyName,
                                eventName));

                            continue;
                        }

                        if (documents.TryGetValue(containingType, out StringBuilder sb) == false)
                        {
                            sb = new StringBuilder();
                            documents[containingType] = sb;
                        }

                        string eventAttributes = "";
                        string propertyAttributes = "";
                        sb.AppendLine(ObservableValueGeneratorFormat.GetObservableValueBlock(typeName, eventName, propertyName, fieldName, eventAttributes, propertyAttributes));
                        sb.AppendLine();
                    }
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
        private static IEnumerable<ObservableFieldInfo> TransformField(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var fieldSyntax = (FieldDeclarationSyntax)context.Node;

            // 하나의 FieldDeclaration에 여러 변수 선언 가능 (예: int a, b;)
            // 지금은 Skeleton이니까 첫 번째 변수만 보고, 나중에 확장해도 됨.
            if (fieldSyntax.Declaration.Variables.Count == 0)
                return null;

            return fieldSyntax.Declaration.Variables
                .Select(variable => {
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
                })
                .Where(info => info != null);
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
