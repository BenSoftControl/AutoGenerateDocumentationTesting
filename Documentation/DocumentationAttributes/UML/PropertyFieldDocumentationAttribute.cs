using System;
using Documentation.DocumentationAttributes.UML;

namespace Documentation.DocumentationAttributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PropertyFieldDocumentationAttribute : BaseDocumentationAttribute
    {
        private readonly string _type;

        public string Type => _type;

        public PropertyFieldDocumentationAttribute(string objectName, string information, string type) : base(objectName, information)
        {
            _type = type;
        }
    }
}
