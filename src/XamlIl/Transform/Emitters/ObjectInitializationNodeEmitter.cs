using System.Collections.Generic;
using System.Reflection.Emit;
using XamlIl.Ast;
using XamlIl.TypeSystem;

namespace XamlIl.Transform.Emitters
{
    public class ObjectInitializationNodeEmitter : IXamlIlAstNodeEmitter
    {
        public XamlIlNodeEmitResult Emit(IXamlIlAstNode node, XamlIlEmitContext context, IXamlIlEmitter codeGen)
        {
            if (!(node is XamlIlObjectInitializationNode init))
                return null;
            var supportInitType = context.Configuration.TypeMappings.SupportInitialize;
            var supportsInitialize = supportInitType != null
                                     && context.Configuration.TypeMappings.SupportInitialize
                                         .IsAssignableFrom(init.Type);

            if (supportsInitialize)
            {

                codeGen
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
                    
                using(var local = context.GetLocal(init.Type))
                codeGen
                    .Stloc(local.Local)
                    .Ldloc(context.ContextLocal).Ldfld(context.RuntimeContext.ParentListField)
                    .Ldloc(local.Local)
                    .EmitCall(objectListType.FindMethod("Add", context.Configuration.WellKnownTypes.Void,
                        false, context.Configuration.WellKnownTypes.Object))
                    .Ldloc(local.Local);

            }

            context.Emit(init.Manipulation, codeGen, null);

            if (addToParentStack)
            {
                codeGen
                    .Ldloc(context.ContextLocal).Ldfld(context.RuntimeContext.ParentListField)
                    .Ldloc(context.ContextLocal).Ldfld(context.RuntimeContext.ParentListField)
                    .EmitCall(objectListType.FindMethod(m => m.Name == "get_Count"))
                    .Ldc_I4(1).Emit(OpCodes.Sub)
                    .EmitCall(objectListType.FindMethod(m => m.Name == "RemoveAt"));
            }
            
            if (supportsInitialize)
                codeGen
                    .EmitCall(supportInitType.FindMethod(m => m.Name == "EndInit"));
            
            
            return XamlIlNodeEmitResult.Void(1);
        }
    }
}