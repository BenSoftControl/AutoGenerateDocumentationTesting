using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DocIncrementalGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        { // https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/
            List<SyntaxTree> tests = new List<SyntaxTree>();
            var rootsContext = context.CompilationProvider.Select((x, i) => x.SyntaxTrees.SelectMany(xx => xx.GetRoot().DescendantNodes()));
            var options = context.CompilationProvider.Select((x, i) => x.Options);
            var name = "";
            context.RegisterSourceOutput(options, (spc, o) =>
            {
                name = o.ModuleName;
            });
            context.RegisterSourceOutput(rootsContext, (spc,roots) =>
            {
                var loc = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                try
                {
                    DocumentationAttributeHandling dah = new DocumentationAttributeHandling();
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
                    dah.Generate(classes.Concat(enums), loc + "\\" + name + "UML.txt", loc + "\\" + name + "Doc.txt");
                }
                catch (Exception e)
                {
                    File.WriteAllText(loc + $@"\{name}Exception.txt", e.ToString());
                }
            });
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
    private List<RelationData> _relations = new List<RelationData>();
    public void Generate(IEnumerable<BaseTypeDeclarationSyntax> types, string filename, string docFileName)
    {
        StringBuilder umlStr = new StringBuilder("@startuml\nskinparam groupInheritance 2\nskinparam linetype polyline\nskinparam linetype ortho\n");
        StringBuilder docStr = new StringBuilder();
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
                }
                else if (data.Length == 2 && data[1].ToString() != "1")
                {
                    strUml.AppendLine("note " + data[0] + " as N" + noteIndex);
                    strUml.AppendLine($"N{noteIndex} .. {data[1]}");
                }
                strDoc.AppendLine($"\tNote: {data[0]}".Replace("\"", ""));
                break;

            case AttributeType.Relation:
                _relations.Add(new RelationData(data[0].ToString(), data[1].ToString(), data[2].ToString()));
                break;

            default: break;

        }
    }
    internal class RelationData
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