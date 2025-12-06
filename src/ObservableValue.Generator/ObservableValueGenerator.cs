using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace H00N.ObservableValue.Generator
{
    [Generator]
    public sealed class ObservableValueGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1) Attribute가 붙어 있을 "가능성"이 있는 FieldDeclaration만 먼저 필터링
            IncrementalValuesProvider<ObservableFieldInfoResult> candidateFields = context.SyntaxProvider
                .CreateSyntaxProvider((node, _) => GeneratorHelper.IsCandidateField(node), (ctx, ct) => GeneratorHelper.GetObservableFieldInfo(ctx, ct))
                .Where(info => info != null)
                .Select((info, _) => info);

            // 2) 컴파일 정보와 묶어서 SourceOutput 단계로 전달
            IncrementalValueProvider<(Compilation, ImmutableArray<ObservableFieldInfoResult>)> compilationAndFields = context.CompilationProvider.Combine(candidateFields.Collect());

            context.RegisterSourceOutput(compilationAndFields, static (spc, source) =>
            {
                (Compilation compilation, ImmutableArray<ObservableFieldInfoResult> fieldInfoList) = source;

                var documents = new Dictionary<INamedTypeSymbol, StringBuilder>(SymbolEqualityComparer.Default);

                foreach (ObservableFieldInfoResult result in fieldInfoList)
                {
                    if (result.Diagnostic != null)
                        spc.ReportDiagnostic(result.Diagnostic);

                    ObservableFieldInfo fieldInfo = result.FieldInfo;
                    if (fieldInfo == null)
                        continue;

                    IFieldSymbol fieldSymbol = fieldInfo.FieldSymbol;

                    // private 필드 강제
                    if (GeneratorHelper.EnsureIsPrivateFieldRule(spc, fieldSymbol) == false)
                        continue;

                    // 필드 이름 강제
                    if (GeneratorHelper.EnsureFollowFieldNamingRule(spc, fieldSymbol) == false)
                        continue;

                    // partial class 강제
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

                    string eventAttributes = GeneratorHelper.GetAttributeString(attributeData, 2);
                    string propertyAttributes = GeneratorHelper.GetAttributeString(attributeData, 3);

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
    }
}
