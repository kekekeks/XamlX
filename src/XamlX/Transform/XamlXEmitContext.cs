using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace XamlX.Transform
{

#if !XAMLX_INTERNAL
    public
#endif
    abstract class XamlXEmitContext<TBackendEmitter, TEmitResult> : XamlContextBase
        where TEmitResult : IXamlEmitResult
    {
        public IFileSource File { get; }
        public List<object> Emitters { get; }

        private IXamlAstNode _currentNode;

        public XamlTransformerConfiguration Configuration { get; }
        public XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> EmitMappings { get; }
        public XamlRuntimeContext<TBackendEmitter, TEmitResult> RuntimeContext { get; }
        public IXamlLocal ContextLocal { get; }
        public Func<string, IXamlType, IXamlTypeBuilder> CreateSubType { get; }
        public TBackendEmitter Emitter { get; }

        public XamlXEmitContext(TBackendEmitter emitter, XamlTransformerConfiguration configuration,
            XamlLanguageEmitMappings<TBackendEmitter, TEmitResult> emitMappings,
            XamlRuntimeContext<TBackendEmitter, TEmitResult> runtimeContext,
            IXamlLocal contextLocal,
            Func<string, IXamlType, IXamlTypeBuilder> createSubType, IFileSource file,
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
                res = EmitNodeCore(value, codeGen);
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

        protected virtual TEmitResult EmitNodeCore(IXamlAstNode value, TBackendEmitter codeGen)
        {
            TEmitResult res = default;
            foreach (var e in Emitters)
            {
                if (e is IXamlAstNodeEmitter<TBackendEmitter, TEmitResult> ve)
                {
                    res = ve.Emit(value, this, codeGen);
                    if (res != default)
                        return res;
                }
            }

            if (value is IXamlAstEmitableNode<TBackendEmitter, TEmitResult> en)
                return en.Emit(this, codeGen);
            else
                throw new XamlLoadException("Unable to find emitter for node type: " + value.GetType().FullName,
                    value);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlEmitResult
    {
        IXamlType ReturnType { get; }
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstNodeEmitter<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        TEmitResult Emit(IXamlAstNode node, XamlXEmitContext<TBackendEmitter, TEmitResult> context, TBackendEmitter codeGen);
    }

#if !XAMLX_INTERNAL
    public
#endif
    interface IXamlAstEmitableNode<TBackendEmitter, TEmitResult>
        where TEmitResult : IXamlEmitResult
    {
        TEmitResult Emit(XamlXEmitContext<TBackendEmitter, TEmitResult> context, TBackendEmitter codeGen);
    }
}
