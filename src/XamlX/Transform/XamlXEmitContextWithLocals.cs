using System;
using System.Collections.Generic;
using System.Text;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{
#if !XAMLX_INTERNAL
    public
#endif
    abstract class XamlXEmitContextWithLocals<TBackendEmitter, TEmitResult> : XamlXEmitContext<TBackendEmitter, TEmitResult>
        where TBackendEmitter : IHasLocalsPool
        where TEmitResult : IXamlEmitResult
    {
        private readonly Dictionary<XamlAstCompilerLocalNode, IXamlLocal>
            _locals = new Dictionary<XamlAstCompilerLocalNode, IXamlLocal>();

        public XamlXEmitContextWithLocals(TBackendEmitter emitter,
            XamlTransformerConfiguration configuration,
            XamlRuntimeContext<TBackendEmitter, TEmitResult> runtimeContext,
            IXamlLocal contextLocal,
            Func<string, IXamlType, IXamlTypeBuilder> createSubType,
            IFileSource file,
            IEnumerable<object> emitters)
            : base(emitter, configuration, runtimeContext, contextLocal, createSubType, file, emitters)
        {
        }

        public abstract void StoreLocal(XamlAstCompilerLocalNode node, TBackendEmitter codeGen);

        protected IXamlLocal TryGetLocalForNode(XamlAstCompilerLocalNode node, TBackendEmitter codeGen, bool throwOnUninitialized)
        {
            if (!_locals.TryGetValue(node, out var local))
            {
                if (throwOnUninitialized)
                {
                    throw new XamlLoadException("Attempt to read uninitialized local variable", node);
                }
                _locals[node] = local = codeGen.DefineLocal(node.Type);
            }
            return local;
        }

        public abstract void LoadLocalValue(XamlAstCompilerLocalNode node, TBackendEmitter codeGen);

        public XamlLocalsPool.PooledLocal GetLocal(IXamlType type)
        {
            return Emitter.LocalsPool.GetLocal(type);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IHasLocalsPool
    {
        XamlLocalsPool LocalsPool { get; }
        IXamlLocal DefineLocal(IXamlType type);
    }
}
