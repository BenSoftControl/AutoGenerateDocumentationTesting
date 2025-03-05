using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DocumentationSourceGenerator
{
    // Super important read 
    // https://github.com/JoanComasFdz/dotnet-how-to-debug-source-generator-vs2022
    // https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview
    // https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#source-generators-cookbook
    // READ THOSE BEFORE WORKING WITH THE GENERATOR
    // Generators only ruin on build and rebuild. 
    //      About above statement, it does seems like the generator runs each time a file is changed, even if the file has not been saved. Seems like it is intellicode doing it. 

    // To apply modifications in the generator, it is required to (re)build. If that does not work, restart Visual Studio
    // Note that the rebuild might indicate that the generator has updated, but the next time it is run it is used the old version that visual studio cached when it last restarted.

    [Generator]
    public class DocSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var loc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                DocumentationAttributeHandling dah = new();
                var roots = context.Compilation.SyntaxTrees.SelectMany(x => x.GetRoot().DescendantNodes());
                IEnumerable<BaseTypeDeclarationSyntax> classes = roots
                    .Where(x => x is ClassDeclarationSyntax)
                    .Cast<ClassDeclarationSyntax>()
                    .OrderBy(x => x.Identifier.Text)
                    .ToList();
                IEnumerable<BaseTypeDeclarationSyntax> enums = roots
                    .Where(x => x is EnumDeclarationSyntax)
                    .Cast<EnumDeclarationSyntax>()
                    .OrderBy(x => x.Identifier.Text)
                    .ToList();
                dah.Generate(classes.Concat(enums), loc + "\\" + context.Compilation.AssemblyName + "UML.txt", loc + "\\" + context.Compilation.AssemblyName + "Doc.txt");
            }
            catch (Exception e)
            {
                File.WriteAllText(loc + @$"\{context.Compilation.AssemblyName}Exception.txt", e.ToString());
            }
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
    private int noteIndex;
    private List<RelationData> _relations = [];
    public void Generate(IEnumerable<BaseTypeDeclarationSyntax> types, string filename, string docFileName)
    {
        StringBuilder umlStr = new("@startuml\nskinparam groupInheritance 2\nskinparam linetype polyline\nskinparam linetype ortho\n");
        StringBuilder docStr = new();
        var documentations = types.Where(x => x.AttributeLists.Any(xx => xx.Attributes.Any(xxx => xxx.Name.ToString() + "Attribute" == "ContainerDocumentationAttribute")));
        foreach (var documentation in documentations)
        {
            if (documentation is ClassDeclarationSyntax)
                umlStr.Append("class ");
            else if (documentation is EnumDeclarationSyntax)
                umlStr.Append("enum ");
            var doc = documentation.AttributeLists.SelectMany(x => x.Attributes).First(x => x.Name.ToString() + "Attribute" == "ContainerDocumentationAttribute");
            HandleDocumentation(doc, umlStr, docStr, AttributeType.Container);
            umlStr.AppendLine("{");
            if (documentation is ClassDeclarationSyntax clDoc)
            {
                var members = clDoc.Members;
                foreach (var member in members)
                {
                    var t = member.GetText();
                    var pfd = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == "PropertyFieldDocumentationAttribute").FirstOrDefault();
                    if (pfd != null)
                    {
                        HandleDocumentation(pfd, umlStr, docStr, AttributeType.Property);
                        var subNotes = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name + "Attribute" == "NoteDocumentationAttribute");
                    }
                }
            }
            else if (documentation is EnumDeclarationSyntax eDoc)
            {
                var members = eDoc.Members;
                foreach (var member in members)
                {
                    var pfd = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == "PropertyFieldDocumentationAttribute").FirstOrDefault();
                    if (pfd != null)
                    {
                        HandleDocumentation(pfd, umlStr, docStr, AttributeType.Field);
                        var subNotes = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name + "Attribute" == "NoteDocumentationAttribute");
                    }
                }
            }
            umlStr.AppendLine("}");
            var notes = documentation.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == "NoteDocumentationAttribute");
            foreach (var note in notes)
                HandleDocumentation(note, umlStr, docStr, AttributeType.Note);
            var relations = documentation.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == "RelationDocumentationAttribute");
            foreach (var relation in relations)
                HandleDocumentation(relation, umlStr, docStr, AttributeType.Relation);
            if (documentation is ClassDeclarationSyntax clDocNote)
            {
                var members = clDocNote.Members;
                foreach (var member in members)
                {
                    var pfd = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == "PropertyFieldDocumentationAttribute").FirstOrDefault();
                    if (pfd != null)
                    {
                        var subNotes = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name + "Attribute" == "NoteDocumentationAttribute");
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
                    var pfd = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name.ToString() + "Attribute" == "PropertyFieldDocumentationAttribute").FirstOrDefault();
                    if (pfd != null)
                    {
                        var subNotes = member.AttributeLists.SelectMany(x => x.Attributes).Where(x => x.Name + "Attribute" == "NoteDocumentationAttribute");
                        foreach (var subNote in subNotes)
                            HandleDocumentation(subNote, umlStr, docStr, AttributeType.Note);
                    }
                }
            }
            docStr.AppendLine();
        }
        HandleRelations(umlStr);
        umlStr.AppendLine("@enduml");
        File.WriteAllText(docFileName, docStr.ToString());
        File.WriteAllText(filename, umlStr.ToString());

    }

    private void HandleRelations(StringBuilder umlStr)
    {
        var groups = _relations.GroupBy(x => x.From);
        foreach (var group in groups)
        {
            foreach (var data in group.Where(x => !x.Done))
            {
                var target = groups.FirstOrDefault(x => x.Key == data.To)?.FirstOrDefault(x => x.To == data.From);
                data.Applied();
                if (target != default)
                {
                    if (target.Done)
                        continue;
                    if (data.Type == "RelationEnum.Collection")
                    {
                        if (target.Type == "RelationEnum.Collection")
                        {
                            umlStr.AppendLine($"{data.From} <--> {data.To}".Replace("\"", ""));
                        }
                        else
                        {
                            umlStr.AppendLine($"{data.From} <--> {data.To}".Replace("\"", ""));
                        }
                    }
                    else
                    {
                        umlStr.AppendLine($"{data.From} <--> {data.To}".Replace("\"", ""));
                    }
                    target.Applied();
                }
                else
                {
                    umlStr.AppendLine($"{data.From} --> {data.To}");
                }
            }
            umlStr.AppendLine();
        }
    }

    private void HandleDocumentation(AttributeSyntax attribute, StringBuilder strUml, StringBuilder strDoc, AttributeType type)
    {
        var data = attribute.ArgumentList.Arguments.ToArray();
        switch (type)
        {
            case AttributeType.Container:
                strDoc.AppendLine($"{data[0]}\n\tDescription: {data[1]}".Replace("\"", ""));
                strUml.AppendLine(data[0].ToString());
                break;

            case AttributeType.Field:
                if (data[3].ToString() == "FieldEnum.Enum")
                {
                    strUml.AppendLine($"{{field}} {data[0]} : {data[4]}".Replace("\"", ""));
                    strDoc.AppendLine($"\tField: {data[0]} - {data[4]} - {data[1]}".Replace("\"", ""));
                    break;
                }
                strUml.AppendLine($"{{field}} {data[0]} : {data[2]}".Replace("\"", ""));
                strDoc.AppendLine($"\tField: {data[0]} - {data[1]}".Replace("\"", ""));
                break;

            case AttributeType.Property:
                strDoc.AppendLine($"\tProperty: {data[0]} - {data[2]} - {data[1]}".Replace("\"", ""));
                strUml.AppendLine($"{{field}} {data[0]} : {data[2]}".Replace("\"", ""));
                break;

            case AttributeType.Note:
                noteIndex++;
                if (data.Length == 3)
                {
                    strUml.AppendLine($"note right of {data[1]}::{data[2]}".Replace("\"", ""));
                    strUml.AppendLine($" {data[0]}\n end note");
                    strDoc.AppendLine($"\tNote - {data[2]}: {data[0]}".Replace("\"", ""));
                }
                else if (data.Length == 2 && data[1].ToString() != "1")
                {
                    strUml.AppendLine("note " + data[0] + " as N" + noteIndex);
                    strUml.AppendLine($"N{noteIndex} .. {data[1]}");
                    strDoc.AppendLine($"\tNote: {data[0]}".Replace("\"", ""));
                }
                break;

            case AttributeType.Relation:
                _relations.Add(new(data[0].ToString(), data[1].ToString(), data[2].ToString()));
                break;

            default: break;

        }
    }

    internal record RelationData
    {
        public string From { get; private set; }
        public string To { get; private set; }
        public string Type { get; private set; }
        public bool Done { get; private set; }

        public RelationData(string from, string to, string type)
        {
            From = from;
            To = to;
            Type = type;
            Done = false;
        }

        public void Applied() => Done = true;
    }
}