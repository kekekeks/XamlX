using System;
using System.Collections.Generic;
using System.Text;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.Emit
{
#if !XAMLX_INTERNAL
    public
#endif
    abstract class XamlEmitContextWithLocals<TBackendEmitter, TEmitResult> : XamlEmitContext<TBackendEmitter, TEmitResult>
        where TBackendEmitter : IHasLocalsPool
        where TEmitResult : IXamlEmitResult
    {
        protected readonly Dictionary<XamlAstCompilerLocalNode, IXamlLocal>
            _locals = new Dictionary<XamlAstCompilerLocalNode, IXamlLocal>();

        public XamlEmitContextWithLocals(TBackendEmitter emitter,
            TransformerConfiguration configuration,
            XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> emitMappings,
            XamlRuntimeContext<TBackendEmitter, TEmitResult> runtimeContext,
            IXamlLocal contextLocal,
            Func<string, IXamlType, IXamlTypeBuilder<TBackendEmitter>> createSubType,
            IFileSource file,
            IEnumerable<object> emitters)
            : base(emitter, configuration, emitMappings, runtimeContext, contextLocal, createSubType, file, emitters)
        {
        }

        public IXamlLocal GetLocalForNode(XamlAstCompilerLocalNode node, TBackendEmitter codeGen, bool throwOnUninitialized)
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

        public XamlLocalsPool.PooledLocal GetLocalOfType(IXamlType type)
        {
            return Emitter.LocalsPool.GetLocal(type);
        }

        protected override TEmitResult EmitNodeCore(IXamlAstNode value, TBackendEmitter codeGen, out bool foundEmitter)
        {
            var result = base.EmitNodeCore(value, codeGen, out foundEmitter);

            if (result != null && result.Valid)
            {
                return result;
            }

            foreach (var e in Emitters)
            {
                if (e is IXamlAstLocalsNodeEmitter<TBackendEmitter, TEmitResult> ve)
                {
                    result = ve.Emit(value, this, codeGen);
                    if (result != null && result.Valid)
                    {
                        foundEmitter = true;
                        return result;
                    }
                }
            }

            if (!foundEmitter)
            {
                if (value is IXamlAstLocalsEmitableNode<TBackendEmitter, TEmitResult> emittable)
                {
                    foundEmitter = true;
                    return emittable.Emit(this, codeGen);
                }
            }

            return result;
        }
        protected override bool EmitCore(IXamlWrappedMethod wrapped, TBackendEmitter codeGen, bool swallowResult)
        {
            bool foundEmitter = base.EmitCore(wrapped, codeGen, swallowResult);

            if (foundEmitter)
            {
                return true;
            }

            foreach (var e in Emitters)
            {
                if (e is IXamlWrappedMethodEmitterWithLocals<TBackendEmitter, TEmitResult> wme)
                {
                    foundEmitter = wme.EmitCall(this, wrapped, codeGen, swallowResult);
                    if (foundEmitter)
                    {
                        break;
                    }
                }
            }

            if (!foundEmitter)
            {
                if (wrapped is IXamlEmitableWrappedMethodWithLocals<TBackendEmitter, TEmitResult> ewm)
                {
                    ewm.Emit(this, codeGen, swallowResult);
                }
            }

            return foundEmitter;
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

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstLocalsNodeEmitter<TBackendEmitter, TEmitResult>
        where TBackendEmitter : IHasLocalsPool
        where TEmitResult : IXamlEmitResult
    {
        TEmitResult Emit(IXamlAstNode node, XamlEmitContextWithLocals<TBackendEmitter, TEmitResult> context, TBackendEmitter codeGen);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstLocalsEmitableNode<TBackendEmitter, TEmitResult>
        where TBackendEmitter : IHasLocalsPool
        where TEmitResult : IXamlEmitResult
    {
        TEmitResult Emit(XamlEmitContextWithLocals<TBackendEmitter, TEmitResult> context, TBackendEmitter codeGen);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlWrappedMethodEmitterWithLocals<TBackendEmitter, TEmitResult>
        where TBackendEmitter : IHasLocalsPool
        where TEmitResult : IXamlEmitResult
    {
        bool EmitCall(XamlEmitContextWithLocals<TBackendEmitter, TEmitResult> context, IXamlWrappedMethod method, TBackendEmitter emitter, bool swallowResult);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlEmitableWrappedMethodWithLocals<TBackendEmitter, TEmitResult> : IXamlWrappedMethod
        where TBackendEmitter : IHasLocalsPool
        where TEmitResult : IXamlEmitResult
    {
        void Emit(XamlEmitContextWithLocals<TBackendEmitter, TEmitResult> context, TBackendEmitter emitter, bool swallowResult);
    }
}
