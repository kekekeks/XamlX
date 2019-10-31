// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace XamlX.Parsers.SystemXamlMarkupExtensionParser
{
    internal static class MeScannerKnownStrings
    {
        // Built-in strings.
        public const string XmlPrefix = "xml";
        public const string XmlNsPrefix = "xmlns";

        public const string Preserve = "preserve";
        public const string Default = "default";

        public const string UriClrNamespace = "clr-namespace";
        public const string UriAssembly = "assembly";

        public const string StringType = "String";
        public const string ObjectType = "Object";

        public const string Get = "Get";
        public const string Set = "Set";
        public const string Add = "Add";
        public const string Handler = "Handler";
        public const string Extension = "Extension";
        public const string IsReadOnly = "IsReadOnly";
        public const string ShouldSerialize = "ShouldSerialize";

        public const string FrameworkElement = "FrameworkElement"; // workaround for top-down (may be not used anymore);

        public const string
            TypeExtension = "TypeExtension"; // workaround to work around x:Type having two, single arg Ctors.

        public const char GraveQuote = '`';
        public const char NestedTypeDelimiter = '+';
        public const string GetEnumerator = "GetEnumerator";
        public const string ICollectionOfT = "System.Collections.Generic.ICollection`1";
        public const string IDictionary = "System.Collections.IDictionary";
        public const string IDictionaryOfKT = "System.Collections.Generic.IDictionary`2";
        public const string NullableOfT = "Nullable`1";
        public const string KeyValuePairOfTT = "KeyValuePair`2";

        public const string AmbientPropertyAttribute = "AmbientPropertyAttribute";

        public const string DependencyPropertySuffix = "Property";

        public const string
            XpsNamespace =
                "http://schemas.microsoft.com/xps/2005/06"; //ideally wouldn't need to workaround this, so wouldn't even need to know it. 

        public const string LocalPrefix = "local";
        public const string DefaultPrefix = "p";

        public const string ReferenceName = "__ReferenceID";
        public static readonly char[] WhitespaceChars = new char[] { ' ', '\t', '\n', '\r', '\f' };
        public static readonly char SpaceChar = ' ';
        public static readonly char TabChar = '\t';
        public static readonly char NewlineChar = '\n';
        public static readonly char ReturnChar = '\r';

        public const string ClrNamespaceFormat = @"clr-namespace:{0};assembly={1}";

        public const string CreateDelegateHelper = "_CreateDelegate";
        public const string CreateDelegate = "CreateDelegate";
        public const string InvokeMember = "InvokeMember";
        public const string GetTypeFromHandle = "GetTypeFromHandle";

        public const string Member = "Member";
        public const string Property = "Property";
    }
}