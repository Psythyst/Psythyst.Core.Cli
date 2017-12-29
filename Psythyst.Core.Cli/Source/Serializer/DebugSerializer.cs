using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Psythyst;
using Psythyst.Core.Data;

namespace Psythyst.Core.Cli
{
    /// <summary>
    /// DebugSerializer Class.
    /// </summary>
    public class DebugSerializer : ISerializer<ProjectModel>
    {
        public ProjectModel Deserialize(string ProjectModel) 
        {
            return JsonConvert.DeserializeObject<ProjectModel>(ProjectModel, new StringEnumConverter());
        }

        public string Serialize(ProjectModel ProjectModel) { 
            return JsonConvert.SerializeObject(ProjectModel, Formatting.Indented, new StringEnumConverter());
        }
    }
}