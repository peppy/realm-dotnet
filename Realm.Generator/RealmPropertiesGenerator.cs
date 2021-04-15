﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Realm.Generator
{
    [Generator]
    public class RealmPropertiesGenerator : ISourceGenerator
    {
        private const string realmClassAttributeTest = @"
using System;

namespace Realm.Generator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RealmClassAttribute : Attribute
    {

    }
}"
;

        private static readonly DiagnosticDescriptor IgnoredFieldWarning = new DiagnosticDescriptor(id: "Realm001",
                                                                                          title: "Field Ignored",
                                                                                          messageFormat: "Field '{0}' in class '{1} is ignored because its accessibility is not private.",
                                                                                          category: "AutoPropertyGenerator",
                                                                                          DiagnosticSeverity.Warning,
                                                                                          isEnabledByDefault: true);
        public void Execute(GeneratorExecutionContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
#endif 
            var syntaxReceiver = context.SyntaxContextReceiver as AutoGeneratedSyntaxReceiver;
            if (syntaxReceiver?.ClassDeclaration == null)
            {
                return;
            }

            var classNode = syntaxReceiver.ClassDeclaration;
            var model = context.Compilation.GetSemanticModel(classNode.SyntaxTree);

            var className = classNode.Identifier.ValueText;
            var namespaceName = (model.GetDeclaredSymbol(syntaxReceiver.NamespaceDeclaration) as INamespaceSymbol).Name;

            var sourceBuilder = new StringBuilder();

            sourceBuilder.Append(GenerateUsingStrings(syntaxReceiver.UsingDeclarations));

            sourceBuilder.Append($@"
namespace {namespaceName}
{{
    public partial class {className}
    {{
");
            var fieldDeclarations = classNode.Members.OfType<FieldDeclarationSyntax>();

            foreach (var field in fieldDeclarations.SelectMany(fd => fd.Declaration.Variables))
            {
                var fieldSymbol = model.GetDeclaredSymbol(field) as IFieldSymbol;
                var type = fieldSymbol.Type.Name;
                var name = fieldSymbol.Name;
                var visibility = fieldSymbol.DeclaredAccessibility;
                if (visibility != Accessibility.Private)
                {
                    context.ReportDiagnostic(Diagnostic.Create(IgnoredFieldWarning, null, name, className));
                    continue;
                }

                sourceBuilder.Append(GenerateAutomaticPropertyString(type, name.FirstCharToUpper()));
            }

            sourceBuilder.Append(@"
    }
}");

            context.AddSource($"class_{className}", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            //This is executed once before all the "execute" steps
            context.RegisterForPostInitialization((pi) => pi.AddSource("RealmClassAttribute", realmClassAttributeTest));  //To add one static file
            context.RegisterForSyntaxNotifications(() => new AutoGeneratedSyntaxReceiver());
        }

        private string GenerateUsingStrings(List<UsingDirectiveSyntax> usingDeclarations)
        {
            return string.Join("", usingDeclarations.Select(ud => ud.ToFullString()));
        }

        private string GenerateAutomaticPropertyString(string type, string name)
        {
            return $"public {type} {name} {{ get; set; }}\n";  //Can be very much expanded for Realm
        }

        class AutoGeneratedSyntaxReceiver : ISyntaxContextReceiver
        {
            public ClassDeclarationSyntax ClassDeclaration { get; private set; }
            public NamespaceDeclarationSyntax NamespaceDeclaration { get; private set; }
            public List<UsingDirectiveSyntax> UsingDeclarations { get; private set; }

            //This thing here is useful because we can create "candidates" for code generation to use later in "Execute"
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is ClassDeclarationSyntax classSyntax && ContainsCorrectAttribute(classSyntax, "RealmClass"))  //TODO probably we should check if the class is private too...
                {
                    //Debugger.Launch();
                    ClassDeclaration = classSyntax;
                    NamespaceDeclaration = classSyntax.Parent as NamespaceDeclarationSyntax;
                    UsingDeclarations = (NamespaceDeclaration.Parent as CompilationUnitSyntax).Usings.ToList();
                }
            }

            private bool ContainsCorrectAttribute(ClassDeclarationSyntax c, string attributeName)  //This could go on an extension method
            {
                return c.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == attributeName);
            }
        }
    }
}

public static class StringExtensions
{
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => input.First().ToString().ToUpper() + input.Substring(1)
        };
}