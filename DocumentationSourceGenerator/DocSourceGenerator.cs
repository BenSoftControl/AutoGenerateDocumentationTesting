﻿using Microsoft.CodeAnalysis;
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

            dah.Test(classes.Concat(enums), loc + "\\" + context.Compilation.AssemblyName + ".txt");
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
    public void Test(IEnumerable<BaseTypeDeclarationSyntax> types, string filename)
    {
        StringBuilder str = new StringBuilder("@startuml\n");
        //var types = assembly.DefinedTypes;
        var documentations = types.Where(x => x.AttributeLists.Any(xx => xx.Attributes.Any(xxx => xxx.Name.ToString() + "Attribute" == nameof(ContainerDocumentationAttribute))));
        foreach (var documentation in documentations)
        {
            if (documentation is ClassDeclarationSyntax)
                str.Append("class ");
            else if (documentation is EnumDeclarationSyntax)
                str.Append("enum ");
            var doc = documentation.AttributeLists.SelectMany(x => x.Attributes).First(x => x.Name.ToString() + "Attribute" == nameof(ContainerDocumentationAttribute));
                HandleDocumentation(doc, str, AttributeType.Container);
            str.AppendLine("{");
            if(documentation is ClassDeclarationSyntax clDoc)
            {
                var members = clDoc.Members;
                foreach(var member in members)
                {
                    var pfd = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == nameof(PropertyFieldDocumentationAttribute)).FirstOrDefault();
                    if(pfd != null)
                    {
                        HandleDocumentation(pfd, str, AttributeType.Property);
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
                        HandleDocumentation(pfd, str, AttributeType.Field);
                        var subNotes = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name + "Attribute" == nameof(NoteDocumentationAttribute));
                    }
                }
            }
            str.AppendLine("}");
            var notes = documentation.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == nameof(NoteDocumentationAttribute));
            foreach (var note in notes)
                HandleDocumentation(note, str, AttributeType.Note);
            var relations = documentation.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == nameof(RelationDocumentationAttribute));
            foreach (var relation in relations)
                HandleDocumentation(relation, str, AttributeType.Relation);
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
                            HandleDocumentation(subNote, str, AttributeType.Note);
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
                            HandleDocumentation(subNote, str, AttributeType.Note);
                    }
                }
            }
        }
        str.AppendLine("@enduml");
        File.WriteAllText(filename, str.ToString());

        var proc = new ProcessStartInfo("java", @$" -jar {Environment.CurrentDirectory}\\DocumentationSourceGenerator\\plantuml.jar {filename}");
        Process.Start(proc);
        Process cmd = new Process();
        cmd.StartInfo.FileName = "java";
        cmd.StartInfo.Arguments = @$" -jar {Environment.CurrentDirectory}\\DocumentationSourceGenerator\\plantuml.jar {filename}";
        //cmd.StartInfo.RedirectStandardInput = true;
        //cmd.StartInfo.RedirectStandardOutput = true;
        //cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.UseShellExecute = false;
        cmd.Start();

        //cmd.StandardInput.WriteLine(" -jar " + Environment.CurrentDirectory + "\\DocumentationSourceGenerator\\plantuml.jar " + filename);
        //cmd.StandardInput.Flush();
        //cmd.StandardInput.Close();
        //cmd.WaitForExit();
    }

    private void HandleDocumentation(AttributeSyntax attribute, StringBuilder str, AttributeType type)
    {
        var data = attribute.ArgumentList.Arguments.ToArray();
        switch (type)
        {
            case AttributeType.Container:
                //str.AppendLine("T: " + data[0] + " - " + data[1]);
                str.AppendLine(data[0].ToString());
                break;

            case AttributeType.Field:
                //str.AppendLine("F:  " + data[0] + " - " + data[1] + " - " + data[2]);
                str.AppendLine("{field} " + data[0] + " : " + data[2]);
                break;

            case AttributeType.Property:
                //str.AppendLine("P:  " + data[0] + " - " + data[1] + " - " + data[2]);
                str.AppendLine("{field} " + data[0] + " : " + data[2]);
                break;

            case AttributeType.Note:
                //str.AppendLine("N: " + data[0]);
                i++;
                if (data.Length == 3)
                {
                    str.AppendLine($"note right of {data[1]}::{data[2]}".Replace("\"",""));
                    str.AppendLine($" {data[0]}\n end note");
                }
                else if (data.Length == 2 && data[1].ToString() != "1")
                {
                    str.AppendLine("note " + data[0] + " as N" + i);
                    str.AppendLine($"N{i} .. {data[1]}");
                }
                
                break;

            case AttributeType.Relation:
                //str.AppendLine("R: " + data[0] + " <-> " + data[1]);
                str.AppendLine(data[0] + " --> " + data[1]);
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