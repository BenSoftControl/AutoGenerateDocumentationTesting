using System;
using Documentation.DocumentationAttributes.UML;

namespace Documentation.DocumentationAttributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PropertyFieldDocumentationAttribute : BaseDocumentationAttribute
    {
        private readonly string _type;
        private readonly FieldEnum _fieldEnum;
        private readonly string _enumValue;

        public string Type => _type;
        public FieldEnum FieldEnum => _fieldEnum;
        public string EnumValue => _enumValue;

        public PropertyFieldDocumentationAttribute(string objectName, string information, string type, FieldEnum fieldEnum, string enumValue = null) : base(objectName, information)
        {
            _type = type;
            _fieldEnum = fieldEnum;
            _enumValue = enumValue;
        }
    }
}
