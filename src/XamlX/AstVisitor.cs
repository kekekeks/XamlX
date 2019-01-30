using System;

namespace XamlX
{
    public class XamlXAstVisitor
    {
        public IXamlXAstNode Visit(IXamlXAstNode node)
        {
            if (node is IXamlXAstValueNode vn)
                return VisitValue(vn);
            
            if (node is XamlXAstPropertyAssignmentNode pa)
                return VisitPropertyAssignment(pa);

            if (node is IXamlXAstTypeReference tr)
                return VisitTypeReference(tr);

            if (node is IXamlXAstPropertyReference pr)
                return VisitPropertyReference(pr);

            return VisitUnknown(node);
        }

        public virtual IXamlXAstNode VisitUnknown(IXamlXAstNode node) =>
            throw new ArgumentException($"{node.GetType()} type is not known");


        protected virtual IXamlXAstValueNode VisitValue(IXamlXAstValueNode node)
        {
            if (node is XamlXAstNewInstanceNode ni)
                return VisitNewInstance(ni);
            if (node is XamlXAstMarkupExtensionNode ma)
                return VisitMarkupExtension(ma);
            if (node is XamlXAstTextNode text)
                return VisitTextNode(text);
            if (node is XamlXAstValueNodeList list)
                return VisitValueList(list);
            return (IXamlXAstValueNode) VisitUnknown(node);
        }

        private IXamlXAstValueNode VisitValueList(XamlXAstValueNodeList list)
        {
            for (var c = 0; c < list.Children.Count; c++)
                list.Children[c] = VisitValue(list.Children[c]);
            return list;
        }

        protected virtual IXamlXAstValueNode VisitTextNode(XamlXAstTextNode text) => text;

        protected virtual IXamlXAstValueNode VisitMarkupExtension(XamlXAstMarkupExtensionNode ma)
        {
            ma.Type = VisitTypeReference(ma.Type);
            for (var c = 0; c < ma.ConstructorArguments.Count; c++)
                ma.ConstructorArguments[c] = Visit(ma.ConstructorArguments[c]);
            for (var c = 0; c < ma.Properties.Count; c++)
                ma.Properties[c] = VisitPropertyAssignment(ma.Properties[c]);

            return ma;
        }

        protected virtual IXamlXAstValueNode VisitNewInstance(XamlXAstNewInstanceNode ni)
        {
            ni.Type = VisitTypeReference(ni.Type);
            for (var c = 0; c < ni.Children.Count; c++)
                ni.Children[c] = VisitManipulationNode(ni.Children[c]);
            return ni;
        }

        protected virtual IXamlXAstManipulationNode VisitManipulationNode(IXamlXAstManipulationNode node)
        {
            if (node is XamlXAstPropertyAssignmentNode pa)
                return VisitPropertyAssignment(pa);
            if (node is IXamlXAstValueNode valueNode)
                return VisitValue(valueNode);
            if (node is IXamlXAstDirective directive)
                return VisitDirective(directive);
            return (IXamlXAstManipulationNode)VisitUnknown(node);

        }

        protected virtual XamlXAstPropertyAssignmentNode VisitPropertyAssignment(XamlXAstPropertyAssignmentNode pa)
        {
            pa.Property = VisitPropertyReference(pa.Property);
            pa.Value = VisitValue(pa.Value);
            return pa;
        }

        protected virtual IXamlXAstPropertyReference VisitPropertyReference(IXamlXAstPropertyReference node)
        {
            if (node is XamlXAstNamePropertyReference named)
                return VisitNamePropertyReference(named);
            return (IXamlXAstPropertyReference) VisitUnknown(node);
        }

        protected virtual IXamlXAstPropertyReference VisitNamePropertyReference(XamlXAstNamePropertyReference named) => named;


        protected virtual IXamlXAstTypeReference VisitTypeReference(IXamlXAstTypeReference tr)
        {
            if (tr is XamlXAstXmlTypeReference xmlt)
                return VisitXmlTypeReference(xmlt);
            if (tr is XamlXAstClrNameTypeReference clrnt)
                return VisitClrNameTypeReference(clrnt);
            return (IXamlXAstTypeReference) VisitUnknown(tr);
        }

        protected virtual IXamlXAstTypeReference VisitClrNameTypeReference(XamlXAstClrNameTypeReference clrnt) => clrnt;

        protected virtual IXamlXAstTypeReference VisitXmlTypeReference(XamlXAstXmlTypeReference xmlt) => xmlt;

        protected virtual IXamlXAstDirective VisitDirective(IXamlXAstDirective directive)
        {
            if (directive is XamlXAstXmlDirective xmld)
                return VisitXmlDirective(xmld);
            return (IXamlXAstDirective) VisitUnknown(directive);
        }

        protected virtual IXamlXAstDirective VisitXmlDirective(XamlXAstXmlDirective directive) => directive;

    }
}