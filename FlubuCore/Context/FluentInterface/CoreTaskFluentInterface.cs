﻿using FlubuCore.Context.FluentInterface.Interfaces;
using FlubuCore.Tasks.NetCore;
using FlubuCore.Tasks.Versioning;

namespace FlubuCore.Context.FluentInterface
{
    public class CoreTaskFluentInterface : ICoreTaskFluentInterface
    {
        private readonly ILinuxTaskFluentInterface _linuxFluent;

        public CoreTaskFluentInterface(ILinuxTaskFluentInterface linuxFluent)
        {
            _linuxFluent = linuxFluent;
        }

        public TaskContext Context { get; set; }

        public ExecuteDotnetTask ExecuteDotnetTask(string command)
        {
            return Context.CreateTask<ExecuteDotnetTask>(command);
        }

        public ExecuteDotnetTask ExecuteDotnetTask(StandardDotnetCommands command)
        {
            return Context.CreateTask<ExecuteDotnetTask>(command);
        }

        public UpdateNetCoreVersionTask UpdateNetCoreVersionTask(string filePath)
        {
            return Context.CreateTask<UpdateNetCoreVersionTask>(filePath);
        }

        public UpdateNetCoreVersionTask UpdateNetCoreVersionTask(params string[] files)
        {
            var task = Context.CreateTask<UpdateNetCoreVersionTask>(string.Empty);
            task.AddFiles(files);
            return task;
        }

        public ILinuxTaskFluentInterface LinuxTasks()
        {
            _linuxFluent.Context = Context;
            return _linuxFluent;
        }

        public ExecuteDotnetTask Restore(string projectName = null, string workingFolder = null)
        {
            ExecuteDotnetTask ret = new ExecuteDotnetTask(StandardDotnetCommands.Restore);

            if (!string.IsNullOrEmpty(workingFolder))
            {
                ret.WorkingFolder(workingFolder);
            }

            if (!string.IsNullOrEmpty(projectName))
            {
                ret.WithArguments(projectName);
            }

            return ret;
        }

        public ExecuteDotnetTask Publish(string projectName = null, string workingFolder = null, string configuration = "Release")
        {
            return new ExecuteDotnetTask(StandardDotnetCommands.Publish)
                .WorkingFolder(workingFolder)
                .WithArguments(projectName, "-c", configuration);
        }

        public ExecuteDotnetTask Build(string projectName = null, string workingFolder = null)
        {
            return new ExecuteDotnetTask(StandardDotnetCommands.Build)
                .WorkingFolder(workingFolder)
                .WithArguments(projectName);
        }
    }
}