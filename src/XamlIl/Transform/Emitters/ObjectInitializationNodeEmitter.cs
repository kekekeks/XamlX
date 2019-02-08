using System.Collections.Generic;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class ObjectInitializationNodeEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlCodeGen codeGen)
        {
            if (!(node is XamlIlObjectInitializationNode init))
                return null;
            var supportInitType = context.Configuration.TypeMappings.SupportInitialize;
            var supportsInitialize = context.Configuration.TypeMappings.SupportInitialize.IsAssignableFrom(init.Type);

            if (supportsInitialize)
            {

                codeGen.Generator
                    // We need two copies of the object for Begin/EndInit
                    .Emit(OpCodes.Dup)
                    .Emit(OpCodes.Dup)
                    .EmitCall(supportInitType.FindMethod(m => m.Name == "BeginInit"));
            }
            
            IXamlIlType objectListType = null;
            var addToParentStack = context.RuntimeContext.ParentListField != null && !init.Type.IsValueType;
            if(addToParentStack)
            {
                objectListType = context.Configuration.TypeSystem.GetType("System.Collections.Generic.List`1")
                    .MakeGenericType(new[] {context.Configuration.WellKnownTypes.Object});
                    
                var local = codeGen.Generator.DefineLocal(init.Type);
                codeGen.Generator
                    .Stloc(local)
                    .Ldloc(context.ContextLocal).Ldfld(context.RuntimeContext.ParentListField)
                    .Ldloc(local)
                    .EmitCall(objectListType.FindMethod("Add", context.Configuration.WellKnownTypes.Void,
                        false, context.Configuration.WellKnownTypes.Object))
                    .Ldloc(local);

            }

            context.Emit(init.Manipulation, codeGen, null);

            if (addToParentStack)
            {
                codeGen.Generator
                    .Ldloc(context.ContextLocal).Ldfld(context.RuntimeContext.ParentListField)
                    .Ldloc(context.ContextLocal).Ldfld(context.RuntimeContext.ParentListField)
                    .EmitCall(objectListType.FindMethod(m => m.Name == "get_Count"))
                    .Ldc_I4(1).Emit(OpCodes.Sub)
                    .EmitCall(objectListType.FindMethod(m => m.Name == "RemoveAt"));
            }
            
            if (supportsInitialize)
                codeGen.Generator
                    .EmitCall(supportInitType.FindMethod(m => m.Name == "EndInit"));
            
            
            return XamlIlNodeEmitResult.Void;
        }
    }
}