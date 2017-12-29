using System;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

using Psythyst;
using Psythyst.Core;
using Psythyst.Core.Data;
using System.Collections.Generic;

namespace Psythyst.Core.Cli
{
    internal sealed class AssemblyResolver : IDisposable
    {
        private readonly ICompilationAssemblyResolver assemblyResolver;
        private readonly DependencyContext dependencyContext;
        private readonly AssemblyLoadContext loadContext;

        public AssemblyResolver(string path)
        {
            this.Assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            this.dependencyContext = DependencyContext.Load(this.Assembly);

            this.assemblyResolver = new CompositeCompilationAssemblyResolver
                                    (new ICompilationAssemblyResolver[]
            {
                new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(path)),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver()
            });

            this.loadContext = AssemblyLoadContext.GetLoadContext(this.Assembly);
            this.loadContext.Resolving += OnResolving;
        }

        public Assembly Assembly { get; }

        public void Dispose()
        {
            this.loadContext.Resolving -= this.OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            bool NamesMatch(RuntimeLibrary runtime)
            {
                return string.Equals(runtime.Name, name.Name, StringComparison.OrdinalIgnoreCase);
            }

            RuntimeLibrary library =
                this.dependencyContext.RuntimeLibraries.FirstOrDefault(NamesMatch);
            if (library != null)
            {
                var wrapper = new CompilationLibrary(
                    library.Type,
                    library.Name,
                    library.Version,
                    library.Hash,
                    library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                    library.Dependencies,
                    library.Serviceable);

                var assemblies = new List<string>();
                this.assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);
                if (assemblies.Count > 0)
                {
                    return this.loadContext.LoadFromAssemblyPath(assemblies[0]);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Program Class.
    /// </summary>
    class Program
    {
        static List<String> PluginFileCollection = new List<String>();

        static void Main(string[] Args)
        {
            var Parser = new CommandParser(false);

            //Parser.FullName = "Spark CLI";
            Parser.Name = "Spark";
            Parser.Description = "Entitas CodeGenerator.";    


            Parser.Command("Generate", GenerateCommand, false); 

            /* Add Option(s). */
            Parser.HelpOption("-H|--Help");

            var PluginOption = Parser.Option("-P|--Plugin <Value>", "Plugin File \n", CommandOptionType.MultipleValue, false);

            /* Execute. */
            Parser.OnExecute(() => {
                
                PluginOption.Values.Each(x => PluginFileCollection.Add(x));
                
                return 0;
            });

            /* Execute Application. */
            Parser.Execute(Args);
        }

        static (IEnumerable<IGenerator<TSource, TResult>> GeneratorCollection, IEnumerable<IPostProcessor<TResult>> PostProcessorCollection) GetTypeCollection<TSource, TResult>(IEnumerable<Assembly> AssemblyCollection)
        {
            var GeneratorList = new List<IGenerator<TSource, TResult>>();
            var PostProcessorList = new List<IPostProcessor<TResult>>();

            var GeneratorInterface = typeof(IGenerator<TSource, TResult>);
            var PostProcessorInterface = typeof(IPostProcessor<TResult>);

            foreach (var Assembly in AssemblyCollection)
            {
                foreach (TypeInfo Type in Assembly.DefinedTypes)
                {
                    if(GeneratorInterface.IsAssignableFrom(Type)){
                        var Generator = Activator.CreateInstance(Type.AsType()) as IGenerator<TSource, TResult>;
                        GeneratorList.Add(Generator);
                    }

                    if(PostProcessorInterface.IsAssignableFrom(Type)){
                        var PostProcessor = Activator.CreateInstance(Type.AsType()) as IPostProcessor<TResult>;
                        PostProcessorList.Add(PostProcessor);
                    }
                }
            }

            return (GeneratorList, PostProcessorList);
        }

        static (IEnumerable<IGenerator<TSource, TResult>> CodeGeneratorCollection, IEnumerable<IPostProcessor<TResult>> PostProcessorCollection) GetTypeCollection<TSource, TResult>(IEnumerable<string> FileCollection)
        {
            var AssemblyCollection = new List<Assembly>();
            foreach (var File in FileCollection){
                var FullPath = Path.GetFullPath(File);
                var Resolver = new AssemblyResolver(FullPath);
                
                if (Resolver.Assembly != null){
                    AssemblyCollection.Add(Resolver.Assembly);
                }
            }

            return GetTypeCollection<TSource, TResult>(AssemblyCollection);
        }
        static void GenerateCommand(ICommandParser Root)
        {
            Root.Description = "Generate Project Content.";
            Root.HelpOption("-H|--Help");

            /* Generate Option(s). */
            var ProjectOption = Root.Option("-F|--File <Value>", "Project File \n", CommandOptionType.SingleValue, true);

            /* Load Plugin(s). */
            var (GeneratorCollection, PostProcessorCollection) = 
                GetTypeCollection<ProjectModel, OutputModel>(PluginFileCollection);

            /* Add Option(s). */
            GeneratorCollection.Each(x => Root.Option($"--{x.ToString()}", String.Empty, CommandOptionType.NoValue));
            PostProcessorCollection.Each(x => Root.Option($"--{x.ToString()}", String.Empty, CommandOptionType.NoValue));

            /* Execute Command. */
            Root.OnExecute(() => 
            {
                var ActiveOptionCollection = Root.GetOptions()
                    .Where(x => x.HasValue() && x.OptionType == CommandOptionType.NoValue)
                    .Select(y => y.LongName);

                var GeneratorList = new List<IGenerator<ProjectModel, OutputModel>>();
                var PostProcessorList = new List<IPostProcessor<OutputModel>>();

                GeneratorCollection.Each(x => {
                    if (ActiveOptionCollection.Contains(x.ToString())) 
                        GeneratorList.Add(x);
                });

                PostProcessorCollection.Each(x => {
                    if (ActiveOptionCollection.Contains(x.ToString())) 
                        PostProcessorList.Add(x);
                });

                /* Load ProjectModel. */
                var ProjectModel = Project.Read(ProjectOption.Value());

                /* Generate Project File(s)! */
                ProjectUnit<ProjectModel, OutputModel>.Create()
                    .AddGeneratorCollection(GeneratorList)
                    .AddPostProcessorCollection(PostProcessorList)
                    .Run(ProjectModel, 
                        /* OnGeneratorError Callback. */
                        (Generator,Exception) => Console.WriteLine($"Generator ({Generator.ToString()}): {Exception.ToString()}"),
                        /* OnPostProcessorError Callback. */
                        (PostProcessor,Exception) => Console.WriteLine($"PostProcessor ({PostProcessor.ToString()}): {Exception.ToString()}"));

                return 0;
            });
        }
    }
}