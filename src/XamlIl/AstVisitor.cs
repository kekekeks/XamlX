using System;

namespace XamlIl
{
    public class XamlIlAstVisitor
    {
        public IXamlIlAstNode Visit(IXamlIlAstNode node)
        {
            if (node is IXamlIlAstValueNode vn)
                return VisitValue(vn);
            
            if (node is XamlIlAstPropertyAssignmentNode pa)
                return VisitPropertyAssignment(pa);

            if (node is IXamlIlAstTypeReference tr)
                return VisitTypeReference(tr);

            if (node is IXamlIlAstPropertyReference pr)
                return VisitPropertyReference(pr);

            return VisitUnknown(node);
        }

        public virtual IXamlIlAstNode VisitUnknown(IXamlIlAstNode node) =>
            throw new ArgumentException($"{node.GetType()} type is not known");


        protected virtual IXamlIlAstValueNode VisitValue(IXamlIlAstValueNode node)
        {
            if (node is XamlIlAstNewInstanceNode ni)
                return VisitNewInstance(ni);
            if (node is XamlIlAstMarkupExtensionNode ma)
                return VisitMarkupExtension(ma);
            if (node is XamlIlAstTextNode text)
                return VisitTextNode(text);
            if (node is XamlIlAstValueNodeList list)
                return VisitValueList(list);
            return (IXamlIlAstValueNode) VisitUnknown(node);
        }

        private IXamlIlAstValueNode VisitValueList(XamlIlAstValueNodeList list)
        {
            for (var c = 0; c < list.Children.Count; c++)
                list.Children[c] = VisitValue(list.Children[c]);
            return list;
        }

        protected virtual IXamlIlAstValueNode VisitTextNode(XamlIlAstTextNode text) => text;

        protected virtual IXamlIlAstValueNode VisitMarkupExtension(XamlIlAstMarkupExtensionNode ma)
        {
            ma.Type = VisitTypeReference(ma.Type);
            for (var c = 0; c < ma.ConstructorArguments.Count; c++)
                ma.ConstructorArguments[c] = Visit(ma.ConstructorArguments[c]);
            for (var c = 0; c < ma.Properties.Count; c++)
                ma.Properties[c] = VisitPropertyAssignment(ma.Properties[c]);

            return ma;
        }

        protected virtual IXamlIlAstValueNode VisitNewInstance(XamlIlAstNewInstanceNode ni)
        {
            ni.Type = VisitTypeReference(ni.Type);
            for (var c = 0; c < ni.Children.Count; c++)
                ni.Children[c] = VisitManipulationNode(ni.Children[c]);
            return ni;
        }

        protected virtual IXamlIlAstManipulationNode VisitManipulationNode(IXamlIlAstManipulationNode node)
        {
            if (node is XamlIlAstPropertyAssignmentNode pa)
                return VisitPropertyAssignment(pa);
            if (node is IXamlIlAstValueNode valueNode)
                return VisitValue(valueNode);
            if (node is IXamlIlAstDirective directive)
                return VisitDirective(directive);
            return (IXamlIlAstManipulationNode)VisitUnknown(node);

        }

        protected virtual XamlIlAstPropertyAssignmentNode VisitPropertyAssignment(XamlIlAstPropertyAssignmentNode pa)
        {
            pa.Property = VisitPropertyReference(pa.Property);
            pa.Value = VisitValue(pa.Value);
            return pa;
        }

        protected virtual IXamlIlAstPropertyReference VisitPropertyReference(IXamlIlAstPropertyReference node)
        {
            if (node is XamlIlAstNamePropertyReference named)
                return VisitNamePropertyReference(named);
            return (IXamlIlAstPropertyReference) VisitUnknown(node);
        }

        protected virtual IXamlIlAstPropertyReference VisitNamePropertyReference(XamlIlAstNamePropertyReference named) => named;


        protected virtual IXamlIlAstTypeReference VisitTypeReference(IXamlIlAstTypeReference tr)
        {
            if (tr is XamlIlAstXmlTypeReference xmlt)
                return VisitXmlTypeReference(xmlt);
            if (tr is XamlIlAstClrNameTypeReference clrnt)
                return VisitClrNameTypeReference(clrnt);
            return (IXamlIlAstTypeReference) VisitUnknown(tr);
        }

        protected virtual IXamlIlAstTypeReference VisitClrNameTypeReference(XamlIlAstClrNameTypeReference clrnt) => clrnt;

        protected virtual IXamlIlAstTypeReference VisitXmlTypeReference(XamlIlAstXmlTypeReference xmlt) => xmlt;

        protected virtual IXamlIlAstDirective VisitDirective(IXamlIlAstDirective directive)
        {
            if (directive is XamlIlAstXmlDirective xmld)
                return VisitXmlDirective(xmld);
            return (IXamlIlAstDirective) VisitUnknown(directive);
        }

        protected virtual IXamlIlAstDirective VisitXmlDirective(XamlIlAstXmlDirective directive) => directive;

    }
}