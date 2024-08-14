using Documentation.DocumentationAttributes;
using Documentation.DocumentationAttributes.UML;

namespace DocumentationTesting.Models;

[ContainerDocumentation("ClassA", "This is ClassA")]
[RelationDocumentation("ClassA", "ClassB", RelationEnum.Single)]
[NoteDocumentation("This is a note on ClassA", "ClassA")]
internal class ClassA
{
    [PropertyFieldDocumentation("Id","The id", "int", FieldEnum.Field)]
    public int Id { get; set; }
    [PropertyFieldDocumentation("B", "The class B", "B", FieldEnum.Field)]
    public ClassB B { get; set; }
}
