# XamlX

General purpose pluggable XAML compiler with no runtime dependencies. 
Currently being used by Avalonia project as the XAML engine.

The compiler isn't tied to Avalonia in any way, shape or form and can be used for any purposes 
by configuring `XamlLanguageTypeMappings` to match the needs of your particular framework.
Further customization can be done by AST manipulations, see examples of those in Avalonia repository.

![default](https://user-images.githubusercontent.com/1067584/52111361-90ad7900-2614-11e9-8133-a5aa6ebb1804.png)


## Implemented features

- Direct convertion of XML to objects (instantiation, setting properties, setting attached properties)
- Create / Populate semantics
- Properties with `[DeferredContent]` get assigned a `Func<IServiceProvider, object>` delegate which emits child nodes (can be customized, see DeferredContentTests)
- Implicit type converting for types with `static T Parse(string, [IFormatProvider])` method (e. g. `int`, `double`, `TimeSpan`, etc)
- Compile-time parsing of primitive types (numbers and boolean)
- Support for TypeConverterAttribute and a way to provide conveters for types without one.
- Support for `[Content]` attribute both for direct content and for collections
- Support for collections themselves (e. g. `<List x:TypeArguments="sys:String"></List>`)
- x:Arguments Directive
- x:TypeArguments Directive
- x:Key Directive 
- Markup extensions with extension point for handling non-convertable values at runtime (e. g. `Binding`)
- Duck-typing for markup extensions, following signatures are checked for markup extension detection (`T` is anything that's not `System.Object`):
```cs
T ProvideValue();
T ProvideValue(IServiceProvider provider);
object ProvideValue();
object ProvideValue(IServiceProvider provider);
```
If strongly typed markup extension overload is available, it's used to avoid unnecessary casts and boxing
- x:Null Markup Extension (intrinsic: `ldnull`)
- x:Type Markup Extension (intrinsic: `ldtoken` + `Type.FromRuntimeHandle`)
- x:Static Markup Extension (intrinsic: properties (`call get_PropName`), fields (`ldsfld`), constants/enums (`ldc_*`/`ldstr`)
- IRootObjectProvider
- UsableDuringInitializationAttribute (assign first, set properties/contents later)
- ISupportInitialize
- XAML parents stack (see IXamlParentsStack in tests) as an lightweight alternative for IAmbientProvider
- Support for mc:Ignorable
- IProvideValueTarget (property name is provided for regular properties, RuntimeMethodInfo is provided for attached ones)
- IUriContext
- Primitive types (sys:String, sys:Int32, sys:TimeSpan etc) https://docs.microsoft.com/en-us/dotnet/framework/xaml-services/built-in-types-for-common-xaml-language-primitives
- Runtime xmlns information via `IXamlXmlNamespaceInfoProvider` (provides `Dictionary<string, List<(string clrNamespace, string asm)>`)

- xml:space Handling in XAML (automatically via XmlReader)
- Event handlers from codebehind

## Architecture

The flow looks like this:
 
1) Parse XAML into some basic AST (we can use different language markup parser at this point, like C#/VB in Roslyn)
2) Transform AST via visitors. At this stage types get resolved, property values get transformed either in setting properties or collection access, etc
3) Emit IL code

## Features to implement (TODO)

Features marked with *[dontneed]* aren't required for the Avalonia project, but might be implemented later if the need arises.
Features marked with *[opt]* are considered optional and will be implemented after non-optional features

- x:Array Markup Extension *[opt]*
- x:Name Directive *[opt]*
- x:FactoryMethod Directive *[opt]*
- x:Reference Markup Extension *[opt]*
- xml:lang Handling in XAML *[dontneed]*
- IDestinationTypeProvider (probably don't need it) *[dontneed]*


These are questinable due to heavy reliance on reflection:
- IXamlTypeResolver (can be implemented in runtime via `IXamlXmlNamespaceInfoProviderV1`) *[dontneed]*
- IXamlNameResolver (probably without forward references) *[dontneed]*
- IXamlNamespaceResolver *[dontneed]*


These are framework-specific and can be implemented via custom transformers/emitters or custom IServiceProvider
- x:Property Directive
- x:Uid Directive
- x:XData Intrinsic XAML Type
- x:Shared Attribute
- x:Class Directive
- x:Subclass Directive
- x:ClassModifier Directive
- x:FieldModifier Directive
- x:Member Directive
- x:Members Directive


### Won't fix:


- IXamlSchemaContextProvider: we don't have a schema context at run time
- IAmbientProvider - we don't have "xaml type system" at run time, only plain CLR types


Future: 
x:Code Intrinsic XAML Type (probably use Roslyn to inline C# code)


## Possible optimizations (TODO):

- Right now if IXamlParentStack feature is enabled, each object initialization triggers push/pop to the parent objects stack. 
That could be optimized out for objects that don't have anything that uses IServiceProvider (markup extensions, `TypeConverter`'s, `DeferredContent`) inside of them
- Parent's `RootObject` could be saved in a closure, right deferred content builder attempts to extract it from
passed `IServiceProvider` 
