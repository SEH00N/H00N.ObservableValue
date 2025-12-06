using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace H00N.ObservableValue.Generator
{
    public static class GeneratorHelper
    {
        private const string ATTRIBUTE_FULL_NAME = "H00N.ObservableValue.ObservableValueAttribute";

        private static readonly DiagnosticDescriptor MustBePrivateFieldRule = new DiagnosticDescriptor(
            id: "OBS001",
            title: "ObservableValue can only be used on private fields",
            messageFormat: "Field '{0}' must be private",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor MustFollowFieldNamingRule = new DiagnosticDescriptor(
            id: "OBS002",
            title: "ObservableValue fields must start with '_' or a lower-case letter",
            messageFormat: "Field '{0}' must start with '_' or a lower-case letter",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor CannotGenerateExistingMemberRule = new DiagnosticDescriptor(
            id: "OBS003",
            title: "ObservableValue cannot overwrite existing members",
            messageFormat: "Field '{0}' cannot generate members ('{1}', '{2}') because they already exist",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor MustBePartialClassRule = new DiagnosticDescriptor(
            id: "OBS004",
            title: "ObservableValue can only be used on partial classes",
            messageFormat: "Class '{0}' must be partial",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor MustHaveSingleVariableRule = new DiagnosticDescriptor(
            id: "OBS005",
            title: "ObservableValue field declarations must declare exactly one variable",
            messageFormat: "ObservableValue field declarations must declare exactly one variable",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        private static readonly DiagnosticDescriptor MustHaveValidAttributeTextRule = new DiagnosticDescriptor(
            id: "OBS006",
            title: "ObservableValue attribute text must be valid",
            messageFormat: "ObservableValue attribute text '{0}' must be valid",
            category: "ObservableValue",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );

        public static bool EnsureIsPartialClassRule(SourceProductionContext context, INamedTypeSymbol containingType)
        {
            bool isPartial = containingType.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax() as TypeDeclarationSyntax)
                .Any(t => t != null && t.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
            if (isPartial)
                return true;

            context.ReportDiagnostic(Diagnostic.Create(MustBePartialClassRule, containingType.Locations[0], containingType.Name));
            return false;
        }

        public static bool EnsureIsPrivateFieldRule(SourceProductionContext context, IFieldSymbol fieldSymbol)
        {
            bool isPrivate = fieldSymbol.DeclaredAccessibility == Accessibility.Private;
            if (isPrivate)
                return true;

            context.ReportDiagnostic(Diagnostic.Create(MustBePrivateFieldRule, fieldSymbol.Locations[0], fieldSymbol.Name));
            return false;
        }

        public static bool EnsureFollowFieldNamingRule(SourceProductionContext context, IFieldSymbol fieldSymbol)
        {
            string fieldName = fieldSymbol.Name;
            bool startsWithUnderscore = fieldName.StartsWith("_");
            bool startsWithLower = fieldName.Length > 0 && char.IsLower(fieldName[0]);
            if (startsWithUnderscore || startsWithLower)
                return true;

            context.ReportDiagnostic(Diagnostic.Create(MustFollowFieldNamingRule, fieldSymbol.Locations[0], fieldSymbol.Name));
            return false;
        }

        public static bool EnsureCannotGenerateExistingMemberRule(SourceProductionContext context, IFieldSymbol fieldSymbol, string propertyName, string eventName)
        {
            bool propertyExists = fieldSymbol.ContainingType.GetMembers(propertyName).Length > 0;
            bool eventExists = fieldSymbol.ContainingType.GetMembers(eventName).Length > 0;
            if (propertyExists == false && eventExists == false)
                return true;

            context.ReportDiagnostic(Diagnostic.Create(CannotGenerateExistingMemberRule, fieldSymbol.Locations[0], fieldSymbol.Name, propertyName, eventName));
            return false;
        }
        
        public static bool IsCandidateField(SyntaxNode node)
        {
            return node is FieldDeclarationSyntax f && f.AttributeLists.Count > 0;
        }

        public static ObservableFieldInfoResult GetObservableFieldInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.Node is FieldDeclarationSyntax fieldSyntax == false)
                return null;

            if (fieldSyntax.Declaration.Variables.Count != 1)
            {
                Location location = fieldSyntax.Declaration.Variables.FirstOrDefault()?.GetLocation() ?? fieldSyntax.GetLocation();
                return new ObservableFieldInfoResult(null, Diagnostic.Create(MustHaveSingleVariableRule, location));
            }

            VariableDeclaratorSyntax variable = fieldSyntax.Declaration.Variables[0];
            if (context.SemanticModel.GetDeclaredSymbol(variable, cancellationToken) is not IFieldSymbol fieldSymbol)
                return null;

            foreach (var attributeData in fieldSymbol.GetAttributes())
            {
                INamedTypeSymbol attrClass = attributeData.AttributeClass;
                if (attrClass is null)
                    continue;

                string fullName = attrClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (fullName == "global::" + ATTRIBUTE_FULL_NAME)
                {
                    INamedTypeSymbol containingType = fieldSymbol.ContainingType;
                    return new ObservableFieldInfoResult(new ObservableFieldInfo(fieldSymbol, containingType, attributeData), null);
                }
            }

            return null;
        }

        public static string GetAttributeString(AttributeData attributeData, int ctorIndex)
        {
            if (attributeData.ConstructorArguments.Length <= ctorIndex)
                return string.Empty;

            TypedConstant constant = attributeData.ConstructorArguments[ctorIndex];
            if (constant.Kind != TypedConstantKind.Array || constant.IsNull)
                return string.Empty;

            IEnumerable<string> attributeInfos = constant.Values.Select(v => v.Value as string).Where(s => !string.IsNullOrWhiteSpace(s));
            string attributes = string.Join("\n    ", attributeInfos.Select(a => $"[{a}]")) + "\n    ";
            return attributes;
        }

        public static bool EnsureAttributeTextValidForField(SourceProductionContext context, Location location, string attributesText)
        {
            if (string.IsNullOrWhiteSpace(attributesText))
                return true;

            string member = "int __field__;";

            foreach (string attribute in attributesText.Split('\n'))
            {
                string snippet = $"{attribute}\n{member}";
                SyntaxTree tree = CSharpSyntaxTree.ParseText(snippet, CSharpParseOptions.Default);

                foreach (Diagnostic diagnostic in tree.GetDiagnostics())
                {
                    if (diagnostic.Severity != DiagnosticSeverity.Error)
                        continue;

                    context.ReportDiagnostic(Diagnostic.Create(MustHaveValidAttributeTextRule, location, attribute));
                    return false;
                }
            }

            return true;
        }

        public static bool EnsureAttributeTextValidForProperty(SourceProductionContext context, Location location, string attributesText)
        {
            if (string.IsNullOrWhiteSpace(attributesText))
                return true;

            string member = "int __Property__ { get; set; }";

            foreach (string attribute in attributesText.Split('\n'))
            {
                string snippet = $"{attribute}\n{member}";
                SyntaxTree tree = CSharpSyntaxTree.ParseText(snippet, CSharpParseOptions.Default);

                foreach (Diagnostic diagnostic in tree.GetDiagnostics())
                {
                    if (diagnostic.Severity != DiagnosticSeverity.Error)
                        continue;

                    context.ReportDiagnostic(Diagnostic.Create(MustHaveValidAttributeTextRule, location, attribute));
                    return false;
                }
            }

            return true;
        }
    }
}
