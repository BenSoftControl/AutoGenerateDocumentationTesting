using Documentation.DocumentationAttributes;

namespace DocumentationTesting.Models;

[ContainerDocumentation("ClassA", "This is ClassA")]
[RelationDocumentation("ClassA", "ClassB", RelationEnum.Single)]
[NoteDocumentation("This is a note on ClassA", "ClassA")]
internal class ClassA
{
    [PropertyFieldDocumentation("Id","The id", "int")]
    public int Id { get; set; }
    [PropertyFieldDocumentation("B", "The class B", "B")]
    public ClassB B { get; set; }
}
