using System.Collections.Generic;
using System.Reflection.Emit;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform.Emitters
{
    public class ObjectInitializationNodeEmitter : IXamlXAstNodeEmitter
    {
        public XamlXNodeEmitResult Emit(IXamlXAstNode node, XamlXEmitContext context, IXamlXEmitter codeGen)
        {
            if (!(node is XamlXObjectInitializationNode init))
                return null;
            var supportInitType = context.Configuration.TypeMappings.SupportInitialize;
            var supportsInitialize = supportInitType != null
                                     && context.Configuration.TypeMappings.SupportInitialize
                                         .IsAssignableFrom(init.Type);

            if (supportsInitialize)
            {

                codeGen
                    // We need a copy for/EndInit
                    .Emit(OpCodes.Dup);
                if (!init.SkipBeginInit)
                    codeGen
                        .Emit(OpCodes.Dup)
                        .EmitCall(supportInitType.FindMethod(m => m.Name == "BeginInit"));
            }
            
            IXamlXType objectListType = null;
            var addToParentStack = context.RuntimeContext.ParentListField != null
                                   && !init.Type.IsValueType
                                   && context.GetOrCreateItem<XamlXNeedsParentStackCache>().NeedsParentStack(node);
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
            
            
            return XamlXNodeEmitResult.Void(1);
        }
    }
    
    class XamlXNeedsParentStackCache : Dictionary<IXamlXAstNode, bool>
    {
        public static void Verify(XamlXContextBase ctx, IXamlXAstNode node)
        {
            var cache = ctx.GetItem<XamlXNeedsParentStackCache>();
            // There is no parent stack
            if (cache == null)
                return;
            if (!cache.ContainsKey(node))
                throw new XamlXLoadException("Node needs parent stack, but one doesn't seem to be provided", node);
        }
        class ParentStackVisitor : IXamlXAstVisitor
        {
            private readonly XamlXNeedsParentStackCache _cache;

            public ParentStackVisitor(XamlXNeedsParentStackCache cache)
            {
                _cache = cache;
            }
            Stack<IXamlXAstNode> _parents = new Stack<IXamlXAstNode>();
            public IXamlXAstNode Visit(IXamlXAstNode node)
            {
                if (_cache.ContainsKey(node))
                    return node;
                if (node is IXamlXAstNodeNeedsParentStack nps && nps.NeedsParentStack)
                {
                    _cache[node] = true;
                    foreach (var parent in _parents)
                        _cache[parent] = true;
                }
                else
                    _cache[node] = false;

                return node;
            }

            public void Push(IXamlXAstNode node) => _parents.Push(node);

            public void Pop() => _parents.Pop();
        }

        public bool NeedsParentStack(IXamlXAstNode node)
        {
            if (TryGetValue(node, out var rv))
                return rv;
            node.Visit(new ParentStackVisitor(this));
            return this[node];
        }
    }
}