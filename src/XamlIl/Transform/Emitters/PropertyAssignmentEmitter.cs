using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
#if !XAMLIL_INTERNAL
    public
#endif
    class PropertyAssignmentEmitter : IXamlIlAstNodeEmitter
    {
        List<IXamlIlPropertySetter> ValidateAndGetSetters(XamlIlPropertyAssignmentNode an)
        {
            var lst = an.PossibleSetters.Where(x => x.Parameters.Count == an.Values.Count).ToList();
            if(an.Values.Count>1 && lst.Count>1)
                for (var c = 0; c < an.Values.Count - 2; c++)
                {
                    var failed = an.PossibleSetters.FirstOrDefault(x =>
                        !x.Parameters[c].IsDirectlyAssignableFrom(an.Values[c].Type.GetClrType()));
                    if (failed != null)
                    {
                        throw new XamlIlLoadException(
                            $"Can not statically cast {an.Values[c].Type.GetClrType().GetFqn()} to {failed.Parameters[c].GetFqn()} and runtime type checking is only supported for the last setter argument",
                            an);
                    }
                }

            if (lst.Count == 0)
                throw new XamlIlLoadException("No setters found for property assignment", an);
            return lst;
        }
        
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!(node is XamlIlPropertyAssignmentNode an))
                return null;

            var setters = ValidateAndGetSetters(an);
            for (var c = 0; c < an.Values.Count - 1; c++)
            {
                context.Emit(an.Values[c], codeGen, an.Values[c].Type.GetClrType());
            }

            var value = an.Values.Last();
            
            var isValueType = value.Type.GetClrType().IsValueType;
            // If there is only one available setter or if value is a value type, always use the first one
            if (setters.Count == 1 || isValueType)
            {
                var setter = an.PossibleSetters.First();
                context.Emit(value, codeGen, setter.Parameters.Last());
                setter.Emit(codeGen);
            }
            else
            {
                var checkedTypes = new List<IXamlIlType>();
                IXamlIlLabel exit = codeGen.DefineLabel();
                IXamlIlLabel next = null;
                var hadJumps = false;
                context.Emit(value, codeGen, value.Type.GetClrType());
                
                foreach (var setter in setters)
                {
                    var type = setter.Parameters.Last();
                    
                    // We have already checked this type or its base type
                    if (checkedTypes.Any(ch => ch.IsAssignableFrom(type)))
                        continue;

                    if (next != null)
                    {
                        codeGen.MarkLabel(next);
                        next = null;
                    }

                    IXamlIlLabel Next() => next ?? (next = codeGen.DefineLabel());

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
                    
                    TypeSystemHelpers.EmitConvert(context, codeGen, value, value.Type.GetClrType(), type);
                    setter.Emit(codeGen);
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

            return XamlIlNodeEmitResult.Void(1);
        }
    }
}
