using System;

namespace Documentation.DocumentationAttributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class NoteDocumentationAttribute : Attribute
    {
        private readonly string _note;
        private readonly string _belongsToo;
        private readonly string _fieldOrProperty;

        public string Note => _note;
        public string BelongsTo => _belongsToo;
        public string FieldOrProperty => _fieldOrProperty;    

        public NoteDocumentationAttribute(string note, string belongsToo, string fieldOrProperty = "")
        {
            _note = note;        
            _belongsToo = belongsToo;
            _fieldOrProperty = fieldOrProperty;
        }
    }
}
