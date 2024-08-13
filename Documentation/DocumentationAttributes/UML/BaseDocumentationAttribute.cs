using System;

namespace Documentation.DocumentationAttributes.UML
{
    public abstract class BaseDocumentationAttribute : Attribute
    {
        internal protected readonly string _objectName;
        internal protected readonly string _information;

        public string ObjectName => _objectName;
        public string Information => _information;

        protected BaseDocumentationAttribute(string objectName, string information)
        {
            _objectName = objectName;
            _information = information;
        }
    }
}
