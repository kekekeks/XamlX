using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class PropertyReferenceResolver : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstNamePropertyReference prop)
            {
                if (!(prop.DeclaringType is XamlAstClrTypeReference declaringRef))
                {
                    if (context.StrictMode)
                        throw new XamlParseException(
                            $"Unable to resolve property {prop.Name} on {prop.DeclaringType}", node);
                    else
                        return node;
                }

                if (!(prop.TargetType is XamlAstClrTypeReference targetRef))
                {
                    if (context.StrictMode)
                        throw new XamlParseException($"Unable to resolve property on {prop.DeclaringType}", node);
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
                        return new XamlAstClrProperty(prop, found, context.Configuration);
                    var clrEvent = declaringType.GetAllEvents().FirstOrDefault(p => p.Name == prop.Name
                                                                                    && p.Add != null);
                    if (clrEvent != null)
                        return new XamlAstClrProperty(prop,
                            prop.Name, clrEvent.Add.DeclaringType, null, clrEvent.Add);
                }

                // Look for attached properties on declaring type
                IXamlMethod setter = null, getter = null, adder = null;
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
                    return new XamlAstClrProperty(prop, prop.Name, declaringType, getter, setter);

                if (adder != null)
                    return new XamlAstClrProperty(prop, prop.Name, declaringType, null, adder);

                if (context.StrictMode)
                    throw new XamlParseException(
                        $"Unable to resolve suitable regular or attached property {prop.Name} on type {declaringType.GetFqn()}",
                        node);
                return null;
            }

            return node;
        }
    }

}
