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

    public List<string> files = [];

    public List<ClipFormat> clips = [];

    [JsonIgnore]
    public const ProjectVersions latest = ProjectVersions.DB7;
}

class ProjectLegacyVDB6
{
    public List<string> files = [];

    public List<ClipFormat> clips = [];

    public Project migrate()
    {
        return new() {files=files, clips=clips};
    }
}




class ProjectVersion
{
    public ProjectVersions version = 0;
} 

enum ProjectVersions
{
    DB6 = 0,
    DB7 = 1, // introdotte le versioni
}

class ProjectLoader
{
    private static readonly JsonSerializerOptions jsonDeserilazionOptions = new() { AllowTrailingCommas = true, ReadCommentHandling = JsonCommentHandling.Skip, IncludeFields = true };

    public static Project Load(string filename)
    {
        var json = File.ReadAllText(filename);

        ProjectVersions version = (JsonSerializer.Deserialize<ProjectVersion>(json, jsonDeserilazionOptions)).version;

        switch (version)
        {
            case ProjectVersions.DB6:
                {
                    ProjectLegacyVDB6 project = JsonSerializer.Deserialize<ProjectLegacyVDB6>(json, jsonDeserilazionOptions)!;
                    return project.migrate();
                }
                
            case Project.latest:
                {
                    return JsonSerializer.Deserialize<Project>(json, jsonDeserilazionOptions)!;
                }
            default:
                {
                    ProjectLegacyVDB6 project = JsonSerializer.Deserialize<ProjectLegacyVDB6>(json, jsonDeserilazionOptions)!;
                    return project.migrate();
                }
        }
        
        
    }
}



public class ClipFormat
{
    public string Filename { get; set; } = string.Empty;
    public double FramesCount { get; set; }
    public double FPS { get; set; }

    public double Duration { get; set; }
}
