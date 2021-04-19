// ////////////////////////////////////////////////////////////////////////////
// //
// // Copyright 2021 Realm Inc.
// //
// // Licensed under the Apache License, Version 2.0 (the "License")
// // you may not use this file except in compliance with the License.
// // You may obtain a copy of the License at
// //
// // http://www.apache.org/licenses/LICENSE-2.0
// //
// // Unless required by applicable law or agreed to in writing, software
// // distributed under the License is distributed on an "AS IS" BASIS,
// // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// // See the License for the specific language governing permissions and
// // limitations under the License.
// //
// ////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Realm.Generator
{
    [Generator]
    public class RealmClassGenerator : ISourceGenerator
    {
        INamedTypeSymbol IListTypeSymbol;

        public void Initialize(GeneratorInitializationContext context)
        {
            //This is executed once before all the "execute" steps
            context.RegisterForSyntaxNotifications(() => new RealmClassSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
#endif 
            var syntaxReceiver = context.SyntaxContextReceiver as RealmClassSyntaxReceiver;
            if (syntaxReceiver?.InterfaceDeclaration == null)
            {
                return;
            }

            IListTypeSymbol ??= context.Compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1");

            var interfaceNode = syntaxReceiver.InterfaceDeclaration;
            var model = context.Compilation.GetSemanticModel(interfaceNode.SyntaxTree);
            var interfaceName = interfaceNode.Identifier.ValueText;
            var namespaceName = syntaxReceiver.Namespace;
            var className = interfaceName.Substring(1);  //Not robust

            var usingsSource = GenerateUsingStrings(syntaxReceiver.UsingDeclarations);

            //Using system is necessary because of the type returned by Roslyn (Int32 instead of int, for instance) -- can be changed
            var classStartSource = $@"
                using System;

                namespace {namespaceName}
                {{
                    [RealmClass]
                    public partial class {className} : RealmObject, {interfaceName}, IRealmObject
                    {{
                ";
            var classEndSource = @"
                    }
                }";

            var propertiesDeclarations = interfaceNode.Members.OfType<PropertyDeclarationSyntax>();

            var propertiesSourceBuilder = new StringBuilder();
            var copyToRealmSourceBuilder = new StringBuilder();

            foreach (var property in propertiesDeclarations)
            {
                var propertySymbol = model.GetDeclaredSymbol(property);

                (var propertyString, var copyToRealmPropertyString) = GeneratePropertyStrings(propertySymbol);
                propertiesSourceBuilder.Append(propertyString);
                copyToRealmSourceBuilder.Append(copyToRealmPropertyString);
            }

            var propertiesSource = propertiesSourceBuilder.ToString();
            string copyToRealmSource = $@"public void CopyToRealm() {{ {copyToRealmSourceBuilder} }}";

            var fullSource =
                usingsSource +
                classStartSource +
                propertiesSource +
                copyToRealmSource +
                classEndSource;

            var formattedSource = CSharpSyntaxTree.ParseText(fullSource).GetRoot().NormalizeWhitespace().ToFullString();

            context.AddSource($"class_{interfaceName}", SourceText.From(formattedSource, Encoding.UTF8));
        }

        private string GenerateUsingStrings(List<UsingDirectiveSyntax> usingDeclarations)
        {
            return string.Join("", usingDeclarations.Select(ud => ud.ToFullString()));
        }

        private (string PropertyString, string CopyToRealmPropertyString) GeneratePropertyStrings(IPropertySymbol propertySymbol)
        {
            var type = propertySymbol.Type as INamedTypeSymbol;
            var typeName = type.Name;

            var backingFieldName = $"_{propertySymbol.Name.ToLower()}";
            var propertyName = propertySymbol.Name;

            //OriginalDefinition is used to get IList<T>, otherwise the comparison returns false because type is IList<something_else>
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, IListTypeSymbol))
            {
                var typeArgument = type.TypeArguments.First(); //TODO It could contain more than one...
                var typeArgumentName = typeArgument.Name;
                var fullTypeName = $"IList<{typeArgumentName}>";
                var propertyString = $@"
                private {fullTypeName} {backingFieldName};

                public {fullTypeName} {propertyName} 
                {{
                    get 
                    {{
                        if (IsManaged)
                        {{
                            return GetListValue<{typeArgumentName}>(""{propertyName}"");
                        }}
                        else
                        {{
                            return {backingFieldName};
                        }}
                    }}
                }}";

                var copyToRealmPropertyString = $"{propertyName} = {backingFieldName};\n";

                return (propertyString, copyToRealmPropertyString);
            }
            else
            {
                var propertyString = $@"
                private {typeName} {backingFieldName};

                public {typeName} {propertyName} 
                {{
                    get 
                    {{
                        if (IsManaged)
                        {{
                            return ({typeName})GetValue(""{propertyName}"");
                        }}
                        else
                        {{
                            return {backingFieldName};
                        }}
                    }}
                    set
                    {{
                        if (IsManaged)
                        {{
                            SetValue(""{propertyName}"", (RealmValue)value);
                        }}
                        else
                        {{
                            {backingFieldName} = value;
                        }}
                    }}
                }}";

                var copyToRealmPropertyString = $"{propertyName} = {backingFieldName};\n";

                return (propertyString, copyToRealmPropertyString);
            }

        }

        class RealmClassSyntaxReceiver : ISyntaxContextReceiver
        {
            public InterfaceDeclarationSyntax InterfaceDeclaration { get; private set; }
            public string Namespace { get; private set; }
            public List<UsingDirectiveSyntax> UsingDeclarations { get; private set; }

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is InterfaceDeclarationSyntax interfaceSyntax) // && ImplementsInterface(context.SemanticModel, interfaceSyntax, "RealmClass"))
                {
                    var interfaceSymbol = context.SemanticModel.GetDeclaredSymbol(interfaceSyntax);

                    if (!interfaceSymbol.AllInterfaces.Any(i => i.Name == "IRealmObject"))
                    {
                        return;
                    }

                    InterfaceDeclaration = interfaceSyntax;
                    Namespace = interfaceSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                    UsingDeclarations = (interfaceSyntax.SyntaxTree.GetRoot() as CompilationUnitSyntax).Usings.ToList();
                }
            }

            private static bool ContainsCorrectAttribute(ClassDeclarationSyntax c, string attributeName)
            {
                return c.AttributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString() == attributeName);
            }
        }
    }
}
