using Documentation.DocumentationAttributes;

namespace DocumentationTesting.Models;

[ContainerDocumentation("EnumA", "Enum A")]
internal enum EnumA
{
    [PropertyFieldDocumentation("None", "This is none", "int")]
    None = 0,
    [PropertyFieldDocumentation("One", "One enum", "int")]
    One = 1,
    [NoteDocumentation("Dont use yet", "EnumA","Two")]
    [NoteDocumentation("or do", "EnumA", "Two")]
    [PropertyFieldDocumentation("Two", "Two total enum", "int")]
    Two = 2,
}
