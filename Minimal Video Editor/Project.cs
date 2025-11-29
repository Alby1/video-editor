using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minimal_Video_Editor;

class Project
{
    public List<string> files = [];

    public List<ClipFormat> clips = [];

}



public class ClipFormat
{
    public string Filename { get; set; }
    public double FramesCount { get; set; }
    public double FPS { get; set; }

    public double duration { get; set; }


    public ClipFormat()
    {

    }
}
