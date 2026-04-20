using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using Barotrauma;
using System.Xml.Linq;
using System.Xml.XPath;
using System;
using MoonSharp.Interpreter;

namespace DSSIFactionCraft
{
    internal static class XMLExtensions
    {
        public static IList<XElement> XPathSelectElements(XElement submarineElement, string xpath)
            => submarineElement.XPathSelectElements(xpath).ToList();
    }
}