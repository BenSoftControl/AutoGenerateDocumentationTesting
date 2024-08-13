using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Reflection;
using Documentation.DocumentationAttributes;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DocumentationSourceGenerator 
{
    // Super important read 
    // https://github.com/JoanComasFdz/dotnet-how-to-debug-source-generator-vs2022
    // https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview
    // https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#source-generators-cookbook
    // READ THOSE BEFORE WORKING WITH THE GENERATOR
    // It does not seem like it is required to restart Visual Studio between changes to the Generator before being able to test the changes.
    // Generators only ruin on build and rebuild.
    [Generator]
    public class DocSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Code generation goes here
            DocumentationAttributeHandling dah = new DocumentationAttributeHandling();
            var t  = context.Compilation.SourceModule;
            var roots = context.Compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodes());
            IEnumerable<BaseTypeDeclarationSyntax> classes = roots
                .Where(x => x is ClassDeclarationSyntax)
                .Cast<ClassDeclarationSyntax>()
                .ToList();
            IEnumerable<BaseTypeDeclarationSyntax> enums = roots
                .Where(x => x is EnumDeclarationSyntax)
                .Cast<EnumDeclarationSyntax>()
                .ToList();
            var loc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            dah.Test(classes.Concat(enums), loc + "\\" + context.Compilation.AssemblyName + "UML.txt", loc + "\\" + context.Compilation.AssemblyName + "Doc.txt");
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}

internal enum AttributeType
{
    Unknown = 0,
    Note = 1,
    Container = 2,
    Field = 3,
    Property = 4,
    Relation = 5,
}

internal class DocumentationAttributeHandling
{
    private int i;
    public void Test(IEnumerable<BaseTypeDeclarationSyntax> types, string filename, string docFileName)
    {
        StringBuilder umlStr = new StringBuilder("@startuml\n");
        StringBuilder docStr = new StringBuilder();
        //var types = assembly.DefinedTypes;
        var documentations = types.Where(x => x.AttributeLists.Any(xx => xx.Attributes.Any(xxx => xxx.Name.ToString() + "Attribute" == nameof(ContainerDocumentationAttribute))));
        foreach (var documentation in documentations)
        {
            if (documentation is ClassDeclarationSyntax)
                umlStr.Append("class ");
            else if (documentation is EnumDeclarationSyntax)
                umlStr.Append("enum ");
            var doc = documentation.AttributeLists.SelectMany(x => x.Attributes).First(x => x.Name.ToString() + "Attribute" == nameof(ContainerDocumentationAttribute));
                HandleDocumentation(doc, umlStr, docStr, AttributeType.Container);
            umlStr.AppendLine("{");
            if(documentation is ClassDeclarationSyntax clDoc)
            {
                var members = clDoc.Members;
                foreach(var member in members)
                {
                    var pfd = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == nameof(PropertyFieldDocumentationAttribute)).FirstOrDefault();
                    if(pfd != null)
                    {
                        HandleDocumentation(pfd, umlStr, docStr, AttributeType.Property);
                        var subNotes = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name + "Attribute" == nameof(NoteDocumentationAttribute));
                    }
                }
            }
            else if (documentation is EnumDeclarationSyntax eDoc)
            {
                var members = eDoc.Members;
                foreach (var member in members)
                {
                    var pfd = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == nameof(PropertyFieldDocumentationAttribute)).FirstOrDefault();
                    if(pfd != null)
                    {
                        HandleDocumentation(pfd, umlStr, docStr, AttributeType.Field);
                        var subNotes = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name + "Attribute" == nameof(NoteDocumentationAttribute));
                    }
                }
            }
            umlStr.AppendLine("}");
            var notes = documentation.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == nameof(NoteDocumentationAttribute));
            foreach (var note in notes)
                HandleDocumentation(note, umlStr, docStr, AttributeType.Note);
            var relations = documentation.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == nameof(RelationDocumentationAttribute));
            foreach (var relation in relations)
                HandleDocumentation(relation, umlStr, docStr, AttributeType.Relation);
            if (documentation is ClassDeclarationSyntax clDocNote)
            {
                var members = clDocNote.Members;
                foreach (var member in members)
                {
                    var pfd = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == nameof(PropertyFieldDocumentationAttribute)).FirstOrDefault();
                    if (pfd != null)
                    {
                        var subNotes = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name + "Attribute" == nameof(NoteDocumentationAttribute));
                        foreach (var subNote in subNotes) 
                            HandleDocumentation(subNote, umlStr, docStr, AttributeType.Note);
                    }
                }
            }
            else if (documentation is EnumDeclarationSyntax eDocNote)
            {
                var members = eDocNote.Members;
                foreach (var member in members)
                {
                    var pfd = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == nameof(PropertyFieldDocumentationAttribute)).FirstOrDefault();
                    if (pfd != null)
                    {
                        var subNotes = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name + "Attribute" == nameof(NoteDocumentationAttribute));
                        foreach (var subNote in subNotes) 
                            HandleDocumentation(subNote, umlStr, docStr, AttributeType.Note);
                    }
                }
            }
            docStr.AppendLine("");
        }
        umlStr.AppendLine("@enduml");
        File.WriteAllText(docFileName, docStr.ToString());
        File.WriteAllText(filename, umlStr.ToString()); // TODO: had a lot of cmd windows pop up and disappear "C:\Program Files\Common Files\Oracle\Java\javapath\java.exe" as name. Not sure why
        // Removed the obj and bin folders, cleared, closed and reopen the project and it stopped. It has not reappeared yet. Note sure what happened. Outcommented the code below first, before removing folders and that.

    }

    private void HandleDocumentation(AttributeSyntax attribute, StringBuilder strUml, StringBuilder strDoc, AttributeType type)
    {
        var data = attribute.ArgumentList.Arguments.ToArray();
        switch (type)
        {
            case AttributeType.Container:
                //str.AppendLine("T: " + data[0] + " - " + data[1]);
                strDoc.AppendLine($"{data[0]} - {data[1]}".Replace("\"", ""));
                strUml.AppendLine(data[0].ToString());
                break;

            case AttributeType.Field:
                //str.AppendLine("F:  " + data[0] + " - " + data[1] + " - " + data[2]);
                strDoc.AppendLine($"Field: {data[0]} - {data[1]}".Replace("\"", ""));
                strUml.AppendLine("{field} " + data[0] + " : " + data[2]);
                break;

            case AttributeType.Property:
                //str.AppendLine("P:  " + data[0] + " - " + data[1] + " - " + data[2]);
                strDoc.AppendLine($"Property: {data[0]} - {data[1]}".Replace("\"", ""));
                strUml.AppendLine("{field} " + data[0] + " : " + data[2]);
                break;

            case AttributeType.Note:
                //str.AppendLine("N: " + data[0]);
                i++;
                if (data.Length == 3)
                {
                    strUml.AppendLine($"note right of {data[1]}::{data[2]}".Replace("\"",""));
                    strUml.AppendLine($" {data[0]}\n end note");
                }
                else if (data.Length == 2 && data[1].ToString() != "1")
                {
                    strUml.AppendLine("note " + data[0] + " as N" + i);
                    strUml.AppendLine($"N{i} .. {data[1]}");
                }
                
                break;

            case AttributeType.Relation:
                //str.AppendLine("R: " + data[0] + " <-> " + data[1]);
                strUml.AppendLine(data[0] + " --> " + data[1]);
                break;

            default: break;

        }
    }

    private void HandleDocumentation(NoteDocumentationAttribute attribute, StringBuilder str)
    {
        str.AppendLine("N: " + attribute.Note);
    }

    private void HandleDocumentation(RelationDocumentationAttribute attribute, StringBuilder str)
    {
        str.AppendLine("R: " + attribute.RelatedObject + " <-> " + attribute.Relation);
    }

    private void HandleDocumentation(ContainerDocumentationAttribute attribute, StringBuilder str)
    {
        str.AppendLine("T: " + attribute.ObjectName + " - " + attribute.Information);
    }

    private void HandleDocumentation(PropertyFieldDocumentationAttribute attribute, StringBuilder str)
    {
        str.AppendLine("P:  " + attribute.ObjectName + " - " + attribute.Information + " - " + attribute.Type);
    }
}