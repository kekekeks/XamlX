using System;

namespace XamlX
{
    public class XamlXAstVisitor
    {
        public IXamlAstNode Visit(IXamlAstNode node)
        {
            if (node is IXamlAstValueNode vn)
                return VisitValue(vn);
            
            if (node is XamlXAstPropertyAssignmentNode pa)
                return VisitPropertyAssignment(pa);

            if (node is IXamlAstTypeReference tr)
                return VisitTypeReference(tr);

            if (node is IXamlAstPropertyReference pr)
                return VisitPropertyReference(pr);

            return VisitUnknown(node);
        }

        public virtual IXamlAstNode VisitUnknown(IXamlAstNode node) =>
            throw new ArgumentException($"{node.GetType()} type is not known");


        protected virtual IXamlAstValueNode VisitValue(IXamlAstValueNode node)
        {
            if (node is XamlXAstNewInstanceNode ni)
                return VisitNewInstance(ni);
            if (node is XamlXAstMarkupExtensionNode ma)
                return VisitMarkupExtension(ma);
            if (node is XamlAstTextNode text)
                return VisitTextNode(text);
            if (node is XamlXAstValueNodeList list)
                return VisitValueList(list);
            return (IXamlAstValueNode) VisitUnknown(node);
        }

        private IXamlAstValueNode VisitValueList(XamlXAstValueNodeList list)
        {
            for (var c = 0; c < list.Children.Count; c++)
                list.Children[c] = VisitValue(list.Children[c]);
            return list;
        }

        protected virtual IXamlAstValueNode VisitTextNode(XamlAstTextNode text) => text;

        protected virtual IXamlAstValueNode VisitMarkupExtension(XamlXAstMarkupExtensionNode ma)
        {
            ma.Type = VisitTypeReference(ma.Type);
            for (var c = 0; c < ma.ConstructorArguments.Count; c++)
                ma.ConstructorArguments[c] = Visit(ma.ConstructorArguments[c]);
            for (var c = 0; c < ma.Properties.Count; c++)
                ma.Properties[c] = VisitPropertyAssignment(ma.Properties[c]);

            return ma;
        }

        protected virtual IXamlAstValueNode VisitNewInstance(XamlXAstNewInstanceNode ni)
        {
            ni.Type = VisitTypeReference(ni.Type);
            for (var c = 0; c < ni.Children.Count; c++)
                ni.Children[c] = VisitManipulationNode(ni.Children[c]);
            return ni;
        }

        protected virtual IXamlAstManipulationNode VisitManipulationNode(IXamlAstManipulationNode node)
        {
            if (node is XamlXAstPropertyAssignmentNode pa)
                return VisitPropertyAssignment(pa);
            if (node is IXamlAstValueNode valueNode)
                return VisitValue(valueNode);
            if (node is IXamlXAstDirective directive)
                return VisitDirective(directive);
            return (IXamlAstManipulationNode)VisitUnknown(node);

        }

        protected virtual XamlXAstPropertyAssignmentNode VisitPropertyAssignment(XamlXAstPropertyAssignmentNode pa)
        {
            pa.Property = VisitPropertyReference(pa.Property);
            pa.Value = VisitValue(pa.Value);
            return pa;
        }

        protected virtual IXamlAstPropertyReference VisitPropertyReference(IXamlAstPropertyReference node)
        {
            if (node is XamlAstNamePropertyReference named)
                return VisitNamePropertyReference(named);
            return (IXamlAstPropertyReference) VisitUnknown(node);
        }

        protected virtual IXamlAstPropertyReference VisitNamePropertyReference(XamlAstNamePropertyReference named) => named;


        protected virtual IXamlAstTypeReference VisitTypeReference(IXamlAstTypeReference tr)
        {
            if (tr is XamlAstXmlTypeReference xmlt)
                return VisitXmlTypeReference(xmlt);
            if (tr is XamlXAstClrNameTypeReference clrnt)
                return VisitClrNameTypeReference(clrnt);
            return (IXamlAstTypeReference) VisitUnknown(tr);
        }

        protected virtual IXamlAstTypeReference VisitClrNameTypeReference(XamlXAstClrNameTypeReference clrnt) => clrnt;

        protected virtual IXamlAstTypeReference VisitXmlTypeReference(XamlAstXmlTypeReference xmlt) => xmlt;

        protected virtual IXamlXAstDirective VisitDirective(IXamlXAstDirective directive)
        {
            if (directive is XamlAstXmlDirective xmld)
                return VisitXmlDirective(xmld);
            return (IXamlXAstDirective) VisitUnknown(directive);
        }

        protected virtual IXamlXAstDirective VisitXmlDirective(XamlAstXmlDirective directive) => directive;

    }
}