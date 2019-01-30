- Direct convertion of XML to objects (instantiation, setting properties, setting attached properties) - works by handling XmlnsDefinitionAttribute and looking for Get/Set{Name} methods when applicable, falling back to property setters/getters when not
- Support for [Content] attribute both for direct content and for collections
- Support for TypeConverterAttribute and a way to provide conveters for types without one.
- A way to execute a part of markup in a deferred way (probably multiple times) for later use
- Support for intercepting property setters and BeginInit/EndInit (needed for bindings to work)
- Support for mc:Ignorable
- IsUsableDuringInitialization (assign first, set properties/contents later)
- ISupportInitialize


Primitive types (sys:String, sys:Int32, sys:TimeSpan etc)
https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/built-in-types-for-common-xaml-language-primitives
https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-arguments-directive
https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-factorymethod-directive

https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-key-directive

https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-name-directive
+
https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-reference-markup-extension

https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-null-markup-extension
https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-static-markup-extension
https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-typearguments-directive
https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/x-array-markup-extension

See XamlLanguage from Portable.Xaml