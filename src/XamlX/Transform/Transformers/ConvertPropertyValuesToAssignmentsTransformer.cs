using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Transformers
{
#if !XAMLX_INTERNAL
    public
#endif
    class ConvertPropertyValuesToAssignmentsTransformer : IXamlAstTransformer
    {
        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is XamlAstXamlPropertyValueNode valueNode)
            {
                var property = valueNode.Property.GetClrProperty();
                var assignments = new List<XamlPropertyAssignmentNode>();
                foreach (var v in valueNode.Values)
                {
                    var keyNode = FindAndRemoveKey(v);
                    var arguments = new List<IXamlAstValueNode>();

                    if (keyNode != null)
                        arguments.Add(keyNode);
                    arguments.Add(v);


                    // Pre-filter setters by non-last argument
                    var filteredSetters = property.Setters.Where(s => s.Parameters.Count == arguments.Count)
                        .ToList();
                    
                    if (arguments.Count > 1)
                    {
                        for (var c = 0; c < arguments.Count - 1; c++)
                        {
                            IXamlType convertedTo = null;
                            for (var s = 0; s < filteredSetters.Count;)
                            {
                                var setter = filteredSetters[s];
                                if (convertedTo == null)
                                {
                                    if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, arguments[c],
                                        setter.Parameters[c], out var converted))
                                    {
                                        filteredSetters.RemoveAt(c);
                                        continue;
                                    }
                                    else
                                    {
                                        convertedTo = converted.Type.GetClrType();
                                        arguments[c] = converted;
                                    }
                                }
                                else
                                {
                                    if (!setter.Parameters[c].IsAssignableFrom(convertedTo))
                                        throw new XamlLoadException(
                                            $"Runtime setter selection is not supported for non-last setter arguments (e. g. x:Key) and can not downcast argument {c} of the setter from {convertedTo} to {setter.Parameters[c]}",
                                            arguments[c]);
                                }
                                s++;
                            }
                        }
                    }

                    XamlPropertyAssignmentNode CreateAssignment()
                    {
                        var matchedSetters = new List<IXamlPropertySetter>();
                        foreach (var setter in filteredSetters)
                        {
                            bool CanAssign(IXamlAstValueNode value, IXamlType type)
                            {
                                // Don't allow x:Null
                                if (!setter.BinderParameters.AllowXNull
                                    && XamlPseudoType.Null.Equals(value.Type.GetClrType()))
                                    return false;

                                // Direct cast
                                if (type.IsAssignableFrom(value.Type.GetClrType()))
                                    return true;

                                // Upcast from System.Object
                                if (value.Type.GetClrType().Equals(context.Configuration.WellKnownTypes.Object))
                                    return true;

                                return false;
                            }

                            var valueArgIndex = arguments.Count - 1;
                            var valueArg = arguments[valueArgIndex];
                            var setterType = setter.Parameters[valueArgIndex];
                            
                            if(CanAssign(valueArg, setterType))
                                matchedSetters.Add(setter);
                            // Converted value have more priority than custom setters, so we just create a setter without an alternative
                            else if (XamlTransformHelpers.TryConvertValue(context, valueArg, setterType, property,
                                out var converted))
                            {
                                
                                arguments[valueArgIndex] = converted;
                                return new XamlPropertyAssignmentNode(valueNode,
                                    property, new[] {setter}, arguments);
                            }
                        }

                        if (matchedSetters.Count > 0)
                            return new XamlPropertyAssignmentNode(v, property, matchedSetters, arguments);

                        throw new XamlLoadException(
                            $"Unable to find suitable setter or adder for property {property.Name} of type {property.DeclaringType.GetFqn()} for argument {v.Type.GetClrType().GetFqn()}"
                            + (keyNode != null ? $" and x:Key of type {keyNode.Type.GetClrType()}" : null)
                            + ", available setter parameter lists are:\n" + string.Join("\n",
                                filteredSetters.Select(setter =>
                                    string.Join(", ", setter.Parameters.Select(p => p.FullName))))
                            , v);
                    }

                    assignments.Add(CreateAssignment());
                }

                if (assignments.Count == 1)
                    return assignments[0];

                if (assignments.Count > 1)
                {
                    // Skip the first one, since we only care about further setters, e. g. the following is perfectly valid:
                    // <Foo.Bar>
                    //   <SomeList/>
                    //   <ListItem/>
                    //   <ListItem/>
                    // </Foo.Bar>
                    // <SomeList/> would be foo.Bar = new SomeList() and <ListItem/> would be foo.Bar.Add(new ListItem());
                    foreach(var ass in assignments.Skip(1))
                    {
                        ass.PossibleSetters = ass.PossibleSetters.Where(s => s.BinderParameters.AllowMultiple).ToList();
                        if (ass.PossibleSetters.Count == 0)
                            throw new XamlLoadException(
                                $"Unable to find a setter that allows multiple assignments to the property {ass.Property.Name} of type {ass.Property.DeclaringType.GetFqn()}",
                                node);
                    }
                }
                
                return new XamlManipulationGroupNode(valueNode, assignments);

            }

            return node;
        }

        static IXamlAstValueNode FindAndRemoveKey(IXamlAstValueNode value)
        {
            IXamlAstValueNode keyNode = null;
            
            bool IsKeyDirective(object node) => node is XamlAstXmlDirective d
                                                && d.Namespace == XamlNamespaces.Xaml2006 &&
                                                d.Name == "Key";
            void ProcessDirective(object d)
            {
                var directive = (XamlAstXmlDirective) d;
                if (directive.Values.Count != 1)
                    throw new XamlParseException("Invalid number of arguments for x:Key directive",
                        directive);
                keyNode = directive.Values[0];
            }

               
            void ProcessDirectiveCandidateList(IList nodes)
            {
                var d = nodes.OfType<object>().FirstOrDefault(IsKeyDirective);
                if (d != null)
                {
                    ProcessDirective(d);
                    nodes.Remove(d);
                }
            }
                
            IXamlAstManipulationNode VisitManipulationNode(IXamlAstManipulationNode man)
            {
                if (IsKeyDirective(man))
                {
                    ProcessDirective(man);
                    return new XamlManipulationGroupNode(man);
                }
                if(man is XamlManipulationGroupNode grp)
                    ProcessDirectiveCandidateList(grp.Children);
                if (man is XamlObjectInitializationNode init)
                    init.Manipulation = VisitManipulationNode(init.Manipulation);
                return man;
            }

            var probe = (value is XamlValueWithSideEffectNodeBase side) ? side.Value : value;
                
            if (probe is XamlAstObjectNode astObject)
                ProcessDirectiveCandidateList(astObject.Children);
            else if (value is XamlValueWithManipulationNode vman)
            {
                vman.Manipulation = VisitManipulationNode(vman.Manipulation);
            }
            else if (value is XamlMarkupExtensionNode mext)
            {
                return FindAndRemoveKey(mext.Value);
            }

            return keyNode;

        }
    }
}
