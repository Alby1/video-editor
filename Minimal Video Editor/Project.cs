using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;


namespace Minimal_Video_Editor;

class Project
{
    public ProjectVersions version = latest;

    public Dictionary<Guid, string> files = [];

    public List<ClipFormat> clips = [];

    [JsonIgnore]
    public const ProjectVersions latest = ProjectVersions.DB8;
}

class ProjectLegacyVDB7
{
    public ProjectVersions version = ProjectVersions.DB7;

    public List<string> files = [];

    public List<ClipFormat> clips = [];
}

class ProjectLegacyVDB6
{
    public List<string> files = [];

    public List<ClipFormat> clips = [];
}




class ProjectVersion
{
    public ProjectVersions version = 0;
} 

enum ProjectVersions
{
    DB6 = 0,
    DB7 = 1, // introdotte le versioni
    DB8 = 2, // clips reference a file
}

class ProjectLoader
{
    private static readonly JsonSerializerOptions jsonDeserializationOptions = new() { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip, IncludeFields = true };

    public static Project Load(string filename)
    {
        var json = File.ReadAllText(filename);

        ProjectVersions version = (JsonSerializer.Deserialize<ProjectVersion>(json, jsonDeserializationOptions)).version;

        switch (version)
        {
            case ProjectVersions.DB6:
                {
                    ProjectLegacyVDB6 project = JsonSerializer.Deserialize<ProjectLegacyVDB6>(json, jsonDeserializationOptions)!;
                    return ProjectMigrations.Migrate(project);
                }
                
            case Project.latest:
                {
                    return JsonSerializer.Deserialize<Project>(json, jsonDeserializationOptions)!;
                }
            default:
                {
                    ProjectLegacyVDB6 project = JsonSerializer.Deserialize<ProjectLegacyVDB6>(json, jsonDeserializationOptions)!;
                    return ProjectMigrations.Migrate(project);
                }
        }

        
    }
}


static class ProjectMigrations
{
    public static Project Migrate(ProjectLegacyVDB6 input)
    {
        var next1 = new ProjectLegacyVDB7() { clips = input.clips, files = input.files };
        return Migrate(next1);
    }
    public static Project Migrate(ProjectLegacyVDB7 input)
    {
        Dictionary<Guid, string> files = [];
        for (int i = 0; i < input.files.Count; i++)
        {
            files.Add(Guid.NewGuid(), input.files[i]);
        }
        var next1 = new Project() { clips = input.clips, files=files };

        return next1;
    }
}




public class ClipFormat
{
    public string Filename { get; set; } = string.Empty;

    public int References { get; set; }

    public double FramesCount { get; set; }
    public double FPS { get; set; }

    public double Duration { get; set; }
}

