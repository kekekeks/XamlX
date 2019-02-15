using System.Collections.Generic;
using System.Linq;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Transformers
{
    public class XamlIlPropertyReferenceResolver : IXamlIlAstTransformer
    {
        public IXamlIlAstNode Transform(XamlIlAstTransformationContext context, IXamlIlAstNode node)
        {
            if (node is XamlIlAstNamePropertyReference prop)
            {
                if (!(prop.DeclaringType is XamlIlAstClrTypeReference declaringRef))
                {
                    if (context.StrictMode)
                        throw new XamlIlParseException(
                            $"Unable to resolve property {prop.Name} on {prop.DeclaringType}", node);
                    else
                        return node;
                }

                if (!(prop.TargetType is XamlIlAstClrTypeReference targetRef))
                {
                    if (context.StrictMode)
                        throw new XamlIlParseException($"Unable to resolve property on {prop.DeclaringType}", node);
                    else
                        return node;
                }

                var targetType = targetRef.Type;
                var declaringType = declaringRef.Type;

                // Can set normal properties of ancestor types and self
                if (declaringType.IsAssignableFrom(targetType))
                {
                    var found = declaringType.GetAllProperties().FirstOrDefault(p =>
                        p.Name == prop.Name
                        && ((p.Getter != null && !p.Getter.IsStatic && p.Getter.Parameters.Count == 0)
                            || p.Setter != null && !p.Setter.IsStatic && p.Setter.Parameters.Count == 1));
                    if (found != null)
                        return new XamlIlAstClrPropertyReference(prop, found);
                    var clrEvent = declaringType.GetAllEvents().FirstOrDefault(p => p.Name == prop.Name
                                                                                    && p.Add != null);
                    if (clrEvent != null)
                        return new XamlIlAstClrPropertyReference(prop,
                            new XamlIlAstCustomProperty(prop.Name, clrEvent.Add, null));
                }

                // Look for attached properties on declaring type
                IXamlIlMethod setter = null, getter = null, adder = null;
                var setterName = "Set" + prop.Name;
                var getterName = "Get" + prop.Name;
                var adderName = "Add" + prop.Name + "Handler";
                foreach (var m in declaringType.Methods)
                {
                    if (m.IsPublic && m.IsStatic)
                    {
                        if (m.Name == getterName && m.Parameters.Count == 1 &&
                            m.Parameters[0].IsAssignableFrom(targetType))
                            getter = m;

                        if (m.Name == setterName && m.Parameters.Count == 2 &&
                            m.Parameters[0].IsAssignableFrom(targetType))
                            setter = m;

                        if (m.Name == adderName
                            && m.Parameters.Count == 2
                            && m.Parameters[0].IsAssignableFrom(targetType))
                            adder = m;
                    }
                }

                if (setter != null || getter != null)
                    return new XamlIlAstClrPropertyReference(prop, new XamlIlAstAttachedProperty(prop.Name, setter, getter));

                if (adder != null)
                    return new XamlIlAstClrPropertyReference(prop, new XamlIlAstCustomProperty(prop.Name, adder, null));

                if (context.StrictMode)
                    throw new XamlIlParseException(
                        $"Unable to resolve suitable regular or attached property {prop.Name} on type {declaringType.GetFqn()}",
                        node);
                return null;
            }

            return node;
        }
    }

    class XamlIlAstAttachedProperty : XamlIlAstCustomProperty
    {
        public XamlIlAstAttachedProperty(string name, IXamlIlMethod setter, IXamlIlMethod getter) : base(name, setter, getter)
        {
        }
    }
    
    class XamlIlAstCustomProperty : IXamlIlProperty
    {
        public bool Equals(IXamlIlProperty other)
        {
            if (other == null)
                return false;
            return other.Name == Name
                   && other.Getter.Equals(Getter)
                   && other.Setter.Equals(Setter);
        }

        public string Name { get; }
        public IXamlIlType PropertyType { get; }
        public IXamlIlMethod Setter { get; }
        public IXamlIlMethod Getter { get; }
        public IReadOnlyList<IXamlIlCustomAttribute> CustomAttributes { get; set; } = new IXamlIlCustomAttribute[0];

        public XamlIlAstCustomProperty(string name, IXamlIlMethod setter, IXamlIlMethod getter)
        {
            Name = name;
            Setter = setter;
            Getter = getter;
            PropertyType = getter != null ? getter.ReturnType : setter.Parameters.Last();
        }
    }

}