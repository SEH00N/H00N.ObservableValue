using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace H00N.ObservableValue.Generator
{
    public static class GeneratorHelper
    {
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
            if (propertyExists || eventExists)
                return true;

            context.ReportDiagnostic(Diagnostic.Create(CannotGenerateExistingMemberRule, fieldSymbol.Locations[0], fieldSymbol.Name, propertyName, eventName));
            return false;
        }
    }
}