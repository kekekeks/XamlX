using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.IL.Emitters
{
#if !XAMLX_INTERNAL
    public
#endif
    class PropertyAssignmentEmitter : IXamlAstLocalsNodeEmitter<IXamlILEmitter, XamlILNodeEmitResult>
    {
        List<IXamlPropertySetter> ValidateAndGetSetters(XamlPropertyAssignmentNode an,
            AstTransformationContext context)
        {
            var result = new List<IXamlPropertySetter>();

            foreach (var setter in an.Property.Setters)
            {
                if (setter.Matches(an.Values))
                {
                    result.Add(setter);
                }
                else
                {
                    var valueArgIndex = an.Values.Count - 1;
                    var valueArg = an.Values[valueArgIndex];

                    if (XamlTransformHelpers.TryConvertValue(context, valueArg, setter.ParameterType, an.Property,
                                out var converted))
                    {

                        arguments[valueArgIndex] = converted;
                        return new XamlPropertyAssignmentNode(valueNode,
                            property, new[] { setter }, arguments);
                    }
                }
            }

            var lst = an.Property.Setters.Where(x => x.Matches(an.Values)).ToList();

            if (lst.Count == 0)
                throw new XamlLoadException("No setters found for property assignment", an);
            return lst;
        }
        
        public XamlILNodeEmitResult Emit(IXamlAstNode node, XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
        {
            if (!(node is XamlPropertyAssignmentNode an))
                return null;

            var setters = ValidateAndGetSetters(an, context.);
            for (var c = 0; c < an.Values.Count - 1; c++)
            {
                context.Emit(an.Values[c], codeGen, an.Values[c].Type.GetClrType());
            }

            var value = an.Values.Last();
            
            var isValueType = value.Type.GetClrType().IsValueType;
            // If there is only one available setter or if value is a value type, always use the first one
            if (setters.Count == 1 || isValueType)
            {
                var setter = setters.First();
                context.Emit(value, codeGen, setter.ParameterType);
                context.Emit(setter, codeGen);
            }
            else
            {
                var checkedTypes = new List<IXamlType>();
                IXamlLabel exit = codeGen.DefineLabel();
                IXamlLabel next = null;
                var hadJumps = false;
                context.Emit(value, codeGen, value.Type.GetClrType());
                
                foreach (var setter in setters)
                {
                    var type = setter.ParameterType;
                    
                    // We have already checked this type or its base type
                    if (checkedTypes.Any(ch => ch.IsAssignableFrom(type)))
                        continue;

                    if (next != null)
                    {
                        codeGen.MarkLabel(next);
                        next = null;
                    }

                    IXamlLabel Next() => next ?? (next = codeGen.DefineLabel());

                    var checkNext = false;
                    if (setter.BinderParameters.AllowRuntimeNull)
                        checkedTypes.Add(type);
                    else
                    {
                        // Check for null; Also don't add this type to the list of checked ones because of the null check
                        codeGen
                            .Dup()
                            .Brfalse(Next());
                        checkNext = true;
                    }

                    // Only do dynamic checks if we know that type is not assignable by downcast 
                    if (!type.IsAssignableFrom(value.Type.GetClrType()))
                    {
                        codeGen
                            .Dup()
                            .Isinst(type)
                            .Brfalse(Next());
                        checkNext = true;
                    }

                    if (checkNext)
                        hadJumps = true;
                    
                    ILEmitHelpers.EmitConvert(context, codeGen, value, value.Type.GetClrType(), type);
                    context.Emit(setter, codeGen);
                    if (hadJumps)
                    {
                        codeGen.Br(exit);
                    }

                    if(!checkNext)
                        break;
                }

                if (next != null)
                {
                    codeGen.MarkLabel(next);

                    if (setters.Any(x => !x.BinderParameters.AllowRuntimeNull))
                    {
                        next = codeGen.DefineLabel();
                        codeGen
                            .Dup()
                            .Brtrue(next)
                            .Newobj(context.Configuration.TypeSystem.GetType("System.NullReferenceException")
                                .FindConstructor())
                            .Throw();
                        codeGen.MarkLabel(next);
                    }

                    codeGen
                        .Newobj(context.Configuration.TypeSystem.GetType("System.InvalidCastException")
                            .FindConstructor())
                        .Throw();
                }

                codeGen.MarkLabel(exit);
            }

            return XamlILNodeEmitResult.Void(1);
        }
    }
}
