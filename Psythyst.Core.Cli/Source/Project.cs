using System;
using System.IO;

using Psythyst;
using Psythyst.Core.Data;

namespace Psythyst.Core.Cli
{
    /// <summary>
    /// Project Class.
    /// </summary>
    public static class Project
    {
        public static ProjectModel Read(string Path, ISerializer<ProjectModel> Serializer = null)
        {
            Path = (String.IsNullOrEmpty(Path)) ? "spark.json" : Path;
            var _Serializer = Serializer ?? new DefaultSerializer();
            var _Content = File.ReadAllText(Path);
            var _ProjectModel = _Serializer.Deserialize(_Content);
            return _ProjectModel;
        }

        public static void Write(ProjectModel ProjectModel, string Path, ISerializer<ProjectModel> Serializer)
        {
            Path = (String.IsNullOrEmpty(Path)) ? "spark.json" : Path;
            var _Serializer = Serializer ?? new DefaultSerializer();
            var _Content = _Serializer.Serialize(ProjectModel);
            File.WriteAllText(Path, _Content);
        }  
    }
}