using Documentation.DocumentationAttributes;
using Documentation.DocumentationAttributes.UML;

namespace DocumentationTesting.Models;

[ContainerDocumentation("EnumA", "Enum A")]
internal enum EnumA
{
    [PropertyFieldDocumentation("None", "This is none", "int", FieldEnum.Enum, "0")]
    None = 0,
    [PropertyFieldDocumentation("One", "One enum", "int", FieldEnum.Enum, "1")]
    One = 1,
    [NoteDocumentation("Dont use yet", "EnumA","Two")]
    [NoteDocumentation("or do", "EnumA", "Two")]
    [PropertyFieldDocumentation("Two", "Two total enum", "int", FieldEnum.Enum, "2")]
    Two = 2,
}
