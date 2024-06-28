using Documentation.DocumentationAttributes;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DocIncrementalGenerator
{
    public class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            throw new NotImplementedException();
        }
    }
}


internal class DocumentationAttributeHandling
{
    private int i;
    public void Test(IEnumerable<System.Reflection.TypeInfo> types, string filename)
    {
        StringBuilder str = new StringBuilder();
        //var types = assembly.DefinedTypes;
        var documentations = types.Where(x => x.GetCustomAttributes().Any(xx => xx.GetType() == typeof(ContainerDocumentationAttribute)));
        foreach (var documentation in documentations)
        {
            var doc = documentation.GetCustomAttribute<ContainerDocumentationAttribute>();
            if (doc is ContainerDocumentationAttribute cDoc)
                HandleDocumentation(cDoc, str);
            var relations = documentation.GetCustomAttributes<RelationDocumentationAttribute>();
            foreach (var relation in relations)
                HandleDocumentation(relation, str);
            var notes = documentation.GetCustomAttributes<NoteDocumentationAttribute>();
            foreach (var note in notes)
                HandleDocumentation(note, str);

            var subDocsProperties = documentation.GetProperties().Where(x => x.GetCustomAttributes().Any(xx => xx.GetType() == typeof(PropertyFieldDocumentationAttribute)));
            str.AppendLine("--Properties--");
            foreach (var subDoc in subDocsProperties)
            {
                HandleDocumentation(subDoc.GetCustomAttribute<PropertyFieldDocumentationAttribute>(), str);
                str.AppendLine("--Notes--");
                foreach (var note in subDoc.GetCustomAttributes<NoteDocumentationAttribute>())
                    HandleDocumentation(note, str);
                str.AppendLine("-------------");
            }
            var subDocsFields = documentation.GetFields().Where(x => x.GetCustomAttributes().Any(xx => xx.GetType() == typeof(PropertyFieldDocumentationAttribute)));
            str.AppendLine("--Fields--");
            foreach (var subDoc in subDocsFields)
            {
                HandleDocumentation(subDoc.GetCustomAttribute<PropertyFieldDocumentationAttribute>(), str);
                str.AppendLine("--Notes--");
                foreach (var note in subDoc.GetCustomAttributes<NoteDocumentationAttribute>())
                    HandleDocumentation(note, str);
                str.AppendLine("-------------");
            }
            str.AppendLine("\n----------------------\n");
            File.WriteAllText(filename, str.ToString());

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