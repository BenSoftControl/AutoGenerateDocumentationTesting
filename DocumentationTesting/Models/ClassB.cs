using Documentation.DocumentationAttributes;
using Documentation.DocumentationAttributes.UML;

namespace DocumentationTesting.Models;

[ContainerDocumentation("ClassB", "This class is B, not A")]
[RelationDocumentation("ClassB", "ClassA", RelationEnum.Collection)]
[RelationDocumentation("ClassB", "EnumA", RelationEnum.Single)]
[NoteDocumentation("WIP", "ClassB")]
internal class ClassB
{
    [PropertyFieldDocumentation("As", "This is a collection of As", "IEnumerable<ClassA>", FieldEnum.Field)]
    public IEnumerable<ClassA> As { get; set; }
    [PropertyFieldDocumentation("BestEnum","The very best","EnumA", FieldEnum.Field)]
    public EnumA BestEnum { get; set; }
}
