using Documentation.DocumentationAttributes.UML;

namespace DocumentationTesting.Models;

internal partial class ClassA
{
    [PropertyFieldDocumentation("TestInt","This is a partial test", "int", FieldEnum.Field)]
    public int TestInt { get; set; }
}
