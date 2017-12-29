using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Psythyst;
using Psythyst.Core.Data;

namespace Psythyst.Core.Cli
{
    /// <summary>
    /// DefaultSerializer Class.
    /// </summary>
    public class DefaultSerializer : ISerializer<ProjectModel>
    {
        //JsonSerializer _Serializer = new JsonSerializer();

        public DefaultSerializer(){
            //_Serializer.Converters.Add(new StringEnumConverter());
        }

        public ProjectModel Deserialize(string ProjectModel) 
        {
            //var StringReader = new StringReader(ProjectModel);
            //var JsonTextReader = new JsonTextReader(StringReader);
            //return _Serializer.Deserialize<ProjectModel>(JsonTextReader);
            return JsonConvert.DeserializeObject<ProjectModel>(ProjectModel, new StringEnumConverter());
        }

        public string Serialize(ProjectModel ProjectModel) { 
            //var StringWriter = new StringWriter();
            //var JsonTextWriter = new JsonTextWriter(StringWriter);
            //_Serializer.Serialize(JsonTextWriter, ProjectModel);
            //return StringWriter.ToString();
            return JsonConvert.SerializeObject(ProjectModel, new StringEnumConverter());
        }
    }
}