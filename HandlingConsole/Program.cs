// See https://aka.ms/new-console-template for more information
using Documentation.DocumentationAttributes;
using DocumentationTesting;
using System.Reflection;

Console.WriteLine(Assembly.GetExecutingAssembly().FullName);
Console.WriteLine("Test");
//DocumentationAttributeHandling.Test();

internal static class DocumentationAttributeHandling
{
    public static void Test()
    {
        var assembly = AssemblyHandling.Value;
        var types = assembly.DefinedTypes;
        var documentations = types.Where(x => x.GetCustomAttributes().Any(xx => xx.GetType() == typeof(ContainerDocumentationAttribute)));
        foreach (var documentation in documentations)
        {
            var doc = documentation.GetCustomAttribute<ContainerDocumentationAttribute>();
            if (doc is ContainerDocumentationAttribute cDoc)
                HandleDocumentation(cDoc);
            var relations = documentation.GetCustomAttributes<RelationDocumentationAttribute>();
            foreach (var relation in relations)
                HandleDocumentation(relation);
            var notes = documentation.GetCustomAttributes<NoteDocumentationAttribute>();
            foreach (var note in notes)
                HandleDocumentation(note);

            var subDocsProperties = documentation.GetProperties().Where(x => x.GetCustomAttributes().Any(xx => xx.GetType() == typeof(PropertyFieldDocumentationAttribute)));
            Console.WriteLine("--Properties--");
            foreach (var subDoc in subDocsProperties)
            {
                HandleDocumentation(subDoc.GetCustomAttribute<PropertyFieldDocumentationAttribute>()!);
                Console.WriteLine("--Notes--");
                foreach(var note in subDoc.GetCustomAttributes<NoteDocumentationAttribute>())
                    HandleDocumentation(note);
                Console.WriteLine("-------------");
            }
            var subDocsFields = documentation.GetFields().Where(x => x.GetCustomAttributes().Any(xx => xx.GetType() == typeof(PropertyFieldDocumentationAttribute)));
            Console.WriteLine("--Fields--");
            foreach (var subDoc in subDocsFields)
            {
                HandleDocumentation(subDoc.GetCustomAttribute<PropertyFieldDocumentationAttribute>()!);
                Console.WriteLine("--Notes--");
                foreach (var note in subDoc.GetCustomAttributes<NoteDocumentationAttribute>())
                    HandleDocumentation(note);
                Console.WriteLine("-------------");
            }
            Console.WriteLine("\n----------------------\n");
        }
    }

    private static void HandleDocumentation(NoteDocumentationAttribute attribute)
    {
        Console.WriteLine("N: " + attribute.Note);
    }

    private static void HandleDocumentation(RelationDocumentationAttribute attribute)
    {
        Console.WriteLine("R: " + attribute.RelatedObject + " <-> " + attribute.Relation);
    }

    private static void HandleDocumentation(ContainerDocumentationAttribute attribute)
    {
        Console.WriteLine("T: " + attribute.ObjectName + " - " + attribute.Information);
    }

    private static void HandleDocumentation(PropertyFieldDocumentationAttribute attribute)
    {
        Console.WriteLine("P:  " + attribute.ObjectName + " - " + attribute.Information + " - " + attribute.Type);
    }
}