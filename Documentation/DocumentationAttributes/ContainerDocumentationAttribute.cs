﻿using System;

namespace Documentation.DocumentationAttributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
    public sealed class ContainerDocumentationAttribute : BaseDocumentationAttribute
    {
        public ContainerDocumentationAttribute(string objectName, string information) : base(objectName, information)
        {
        }
    }
}
