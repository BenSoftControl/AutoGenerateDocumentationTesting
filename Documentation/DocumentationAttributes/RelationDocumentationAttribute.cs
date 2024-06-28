using System;

namespace Documentation.DocumentationAttributes
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RelationDocumentationAttribute : Attribute
    {
        private readonly string _object;
        private readonly string _relatedObject;
        private readonly RelationEnum _relation;

        public string Object => _object;
        public string RelatedObject => _relatedObject;
        public RelationEnum Relation => _relation;

        public RelationDocumentationAttribute(string @object, string relatedObject, RelationEnum relation)
        {
            _object = @object;
            _relatedObject = relatedObject;        
            _relation = relation;
        }
    }
}
