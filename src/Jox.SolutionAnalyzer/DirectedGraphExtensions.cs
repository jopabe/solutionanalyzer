using OpenSoftware.DgmlTools.Model;
using System.Xml.Serialization;

namespace Jox.SolutionAnalyzer;

internal static class DirectedGraphExtensions
{
    public static void SerializeTo(this DirectedGraph graph, TextWriter output)
    {
        var serializer = new XmlSerializer(typeof(DirectedGraph));
        serializer.Serialize(output, graph);
    }
}
