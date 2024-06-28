using Documentation.DocumentationAttributes;

namespace DocumentationTesting.Models;

[ContainerDocumentation("ClassB", "This class is B, not A")]
[RelationDocumentation("ClassB", "ClassA", RelationEnum.Collection)]
[RelationDocumentation("ClassB", "EnumA", RelationEnum.Single)]
[NoteDocumentation("WIP", "ClassB")]
internal class ClassB
{
    [PropertyFieldDocumentation("As", "This is a collection of As", "IEnumerable<ClassA>")]
    public IEnumerable<ClassA> As { get; set; }
    [PropertyFieldDocumentation("BestEnum","The very best","EnumA")]
    public EnumA BestEnum { get; set; }
}
