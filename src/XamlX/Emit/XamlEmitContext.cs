using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.Emit
{
#if !XAMLX_INTERNAL
    public
#endif
    abstract class XamlEmitContext<TBackendEmitter, TEmitResult> : XamlContextBase
        where TEmitResult : IXamlEmitResult
    {
        public IFileSource File { get; }
        public List<object> Emitters { get; }

        private IXamlAstNode _currentNode;

        public TransformerConfiguration Configuration { get; }
        public XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> EmitMappings { get; }
        public XamlRuntimeContext<TBackendEmitter, TEmitResult> RuntimeContext { get; }
        public IXamlLocal ContextLocal { get; }
        public Func<string, IXamlType, IXamlTypeBuilder<TBackendEmitter>> CreateSubType { get; }
        public TBackendEmitter Emitter { get; }

        public XamlEmitContext(TBackendEmitter emitter, TransformerConfiguration configuration,
            XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> emitMappings,
            XamlRuntimeContext<TBackendEmitter, TEmitResult> runtimeContext,
            IXamlLocal contextLocal,
            Func<string, IXamlType, IXamlTypeBuilder<TBackendEmitter>> createSubType, IFileSource file,
            IEnumerable<object> emitters)
        {
            File = file;
            Emitter = emitter;
            Emitters = emitters.ToList();
            Configuration = configuration;
            RuntimeContext = runtimeContext;
            ContextLocal = contextLocal;
            CreateSubType = createSubType;
            EmitMappings = emitMappings;
        }

        public TEmitResult Emit(IXamlAstNode value, TBackendEmitter codeGen, IXamlType expectedType)
        {
            TEmitResult res;
            if (_currentNode != null)
            {
                PushParent(_currentNode);
                _currentNode = value;
                res = EmitCore(value, codeGen, expectedType);
                _currentNode = PopParent();
            }
            else
            {
                _currentNode = value;
                res = EmitCore(value, codeGen, expectedType);
                _currentNode = null;
            }

            return res;
        }

        public void Emit(IXamlPropertySetter setter, TBackendEmitter codeGen)
        {
            bool foundEmitter = EmitCore(setter, codeGen);

            if (!foundEmitter)
            {
                throw new InvalidOperationException("Unable to find emitter for property setter type: " + setter.GetType().ToString());
            }
        }

        protected virtual bool EmitCore(IXamlPropertySetter setter, TBackendEmitter codeGen)
        {
            bool foundEmitter = false;
            foreach (var e in Emitters)
            {
                if (e is IXamlPropertySetterEmitter<TBackendEmitter> pse)
                {
                    foundEmitter = pse.EmitCall(setter, codeGen);
                    if (foundEmitter)
                    {
                        break;
                    }
                }
            }

            if (!foundEmitter)
            {
                if (setter is IXamlEmitablePropertySetter<TBackendEmitter> eps)
                {
                    foundEmitter = true;
                    eps.Emit(codeGen);
                }
            }

            return foundEmitter;
        }

        public void Emit(IXamlWrappedMethod wrapped, TBackendEmitter codeGen, bool swallowResult)
        {
            bool foundEmitter = EmitCore(wrapped, codeGen, swallowResult);

            if (!foundEmitter)
            {
                throw new InvalidOperationException("Unable to find emitter for wrapped method type: " + wrapped.GetType().ToString());
            }
        }

        protected virtual bool EmitCore(IXamlWrappedMethod wrapped, TBackendEmitter codeGen, bool swallowResult)
        {
            bool foundEmitter = false;
            foreach (var e in Emitters)
            {
                if (e is IXamlWrappedMethodEmitter<TBackendEmitter, TEmitResult> wme)
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
                if (wrapped is IXamlEmitableWrappedMethod<TBackendEmitter, TEmitResult> ewm)
                {
                    foundEmitter = true;
                    ewm.Emit(this, codeGen, swallowResult);
                }
            }

            return foundEmitter;
        }

        private TEmitResult EmitCore(IXamlAstNode value, TBackendEmitter codeGen, IXamlType expectedType)
        {
            TEmitResult res = EmitNode(value, codeGen);
            IXamlType returnedType = res.ReturnType;

            if (returnedType != null || expectedType != null)
            {
                if (returnedType != null && expectedType == null)
                    throw new XamlLoadException(
                        $"Emit of node {value} resulted in {returnedType.GetFqn()} while caller expected void", value);

                if (expectedType != null && returnedType == null)
                    throw new XamlLoadException(
                        $"Emit of node {value} resulted in void while caller expected {expectedType.GetFqn()}", value);

                if (!returnedType.Equals(expectedType))
                {
                    EmitConvert(value, codeGen, expectedType, returnedType);
                }
            }

            return res;
        }

        protected virtual TEmitResult EmitNode(IXamlAstNode value, TBackendEmitter codeGen)
        {
#if XAMLX_DEBUG
            var res = EmitNodeCore(value, codeGen);
#else
            TEmitResult res;
            try
            {
                res = EmitNodeCore(value, codeGen, out var foundEmitter);

                if (!foundEmitter)
                {
                    throw new XamlLoadException("Unable to find emitter for node type: " + value.GetType().FullName, value);
                }
            }
            catch (Exception e) when (!(e is XmlException))
            {
                throw new XamlLoadException(
                    "Internal compiler error while emitting node " + value + ":\n" + e, value);
            }
#endif
            return res;
        }

        protected abstract void EmitConvert(IXamlAstNode value, TBackendEmitter codeGen, IXamlType expectedType, IXamlType returnedType);

        protected virtual TEmitResult EmitNodeCore(IXamlAstNode value, TBackendEmitter codeGen, out bool foundEmitter)
        {
            TEmitResult res = default;
            foreach (var e in Emitters)
            {
                if (e is IXamlAstNodeEmitter<TBackendEmitter, TEmitResult> ve)
                {
                    res = ve.Emit(value, this, codeGen);
                    if (res != null && res.Valid)
                    {
                        foundEmitter = true;
                        return res;
                    }
                }
            }

            if (value is IXamlAstEmitableNode<TBackendEmitter, TEmitResult> en)
            {
                foundEmitter = true;
                return en.Emit(this, codeGen);
            }

            foundEmitter = false;
            return res;
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlEmitResult
    {
        IXamlType ReturnType { get; }
        bool Valid { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstNodeEmitter<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        TEmitResult Emit(IXamlAstNode node, XamlEmitContext<TBackendEmitter, TEmitResult> context, TBackendEmitter codeGen);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstEmitableNode<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        TEmitResult Emit(XamlEmitContext<TBackendEmitter, TEmitResult> context, TBackendEmitter codeGen);
    }
#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlCustomEmitMethod<TBackendEmitter> : IXamlMethod
    {
        void EmitCall(TBackendEmitter emitter);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlPropertySetterEmitter<TBackendEmitter>
    {
        bool EmitCall(IXamlPropertySetter setter, TBackendEmitter emitter);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlEmitablePropertySetter<TBackendEmitter> : IXamlPropertySetter
    {
        void Emit(TBackendEmitter emitter);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlWrappedMethodEmitter<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        bool EmitCall(XamlEmitContext<TBackendEmitter, TEmitResult> context, IXamlWrappedMethod method, TBackendEmitter emitter, bool swallowResult);
    }


#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlEmitableWrappedMethod<TBackendEmitter, TEmitResult> : IXamlWrappedMethod
        where TEmitResult: IXamlEmitResult
    {
        void Emit(XamlEmitContext<TBackendEmitter, TEmitResult> context, TBackendEmitter emitter, bool swallowResult);
    }
}
