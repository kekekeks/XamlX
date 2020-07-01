using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Xml;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace XamlX.IL
{
#if !XAMLX_INTERNAL
    public
#endif
    class ILEmitContext : XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult>
    {
        public bool EnableIlVerification { get; }

        public ILEmitContext(IXamlILEmitter emitter, TransformerConfiguration configuration,
            XamlLanguageEmitMappings<IXamlILEmitter, XamlILNodeEmitResult> emitMappings,
            XamlRuntimeContext<IXamlILEmitter, XamlILNodeEmitResult> runtimeContext,
            IXamlLocal contextLocal,
            Func<string, IXamlType, IXamlTypeBuilder<IXamlILEmitter>> createSubType, IFileSource file, IEnumerable<object> emitters)
            : base(emitter, configuration, emitMappings, runtimeContext,
                contextLocal, createSubType, file, emitters)
        {
            EnableIlVerification = configuration.GetOrCreateExtra<ILEmitContextSettings>().EnableILVerification;
        }
        
        protected override XamlILNodeEmitResult EmitNode(IXamlAstNode value, IXamlILEmitter codeGen)
        {
            CheckingILEmitter parent = null;
            CheckingILEmitter checkedEmitter = null;
            if (EnableIlVerification)
            {
                parent = codeGen as CheckingILEmitter;

                parent?.Pause();
                checkedEmitter = new CheckingILEmitter(codeGen);
            }
#if XAMLX_DEBUG
            var res = base.EmitNode(value, checkedEmitter);
#else
            var res = base.EmitNode(value, checkedEmitter ?? codeGen);
#endif
            if (EnableIlVerification)
            {
                var expectedBalance = res.ProducedItems - res.ConsumedItems;
                var checkResult =
                    checkedEmitter.Check(res.ProducedItems - res.ConsumedItems, false);
                if (checkResult != null)
                    throw new XamlLoadException($"Error during IL verification: {checkResult}\n{checkedEmitter}\n",
                        value);
                parent?.Resume();
                parent?.ExplicitStack(expectedBalance);
            }

            return res;
        }

        protected override XamlILNodeEmitResult EmitNodeCore(IXamlAstNode value, IXamlILEmitter codeGen, out bool foundEmitter)
        {
            if(File!=null)
                codeGen.InsertSequencePoint(File, value.Line, value.Position);

            return base.EmitNodeCore(value, codeGen, out foundEmitter);
        }

        protected override void EmitConvert(IXamlAstNode value, IXamlILEmitter codeGen, IXamlType expectedType, IXamlType returnedType)
        {
            XamlLocalsPool.PooledLocal local = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            // Value is assigned inside the closure in certain conditions

            ILEmitHelpers.EmitConvert(this, value, returnedType, expectedType, ldaddr =>
            {
                if (ldaddr && returnedType.IsValueType)
                {
                    // We need to store the value to a temporary variable, since *address*
                    // is required (probably for  method call on the value type)
                    local = GetLocalOfType(returnedType);
                    codeGen
                        .Stloc(local.Local)
                        .Ldloca(local.Local);

                }
                // Otherwise do nothing, value is already at the top of the stack
                return codeGen;
            });
            local?.Dispose();
        }

        public override void LoadLocalValue(XamlAstCompilerLocalNode node, IXamlILEmitter codeGen)
        {
            if (_locals.TryGetValue(node, out var local))
                codeGen.Emit(OpCodes.Ldloc, local);
            else
                throw new XamlLoadException("Attempt to read uninitialized local variable", node);
        }
    }

#if !XAMLX_INTERNAL
    public
#endif
    class ILEmitContextSettings
    {
        public bool EnableILVerification { get; set; }
    }
}
