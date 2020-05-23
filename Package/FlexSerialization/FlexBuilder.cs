using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using com.Dunkingmachine.BitSerialization;

namespace com.Dunkingmachine.FlexSerialization
{
    public class FlexBuilder : BitBuilder<FlexDataAttribute>
    {
        private const string MetaFileExtension = "flexmeta";
        private Dictionary<string,FlexClassInfo> _infos = new Dictionary<string, FlexClassInfo>();
        
        private void BuildMetaFiles(string path)
        {
            
        }

        private void LoadOldMetaFiles(string path)
        {
            foreach (var meta in Directory.GetFiles(path).Where(p => Path.GetExtension(p) == MetaFileExtension))
            {
                var classinfo = FlexClassInfoUtility.ReadClassInfo(meta);
                _infos[classinfo.TypeName] = classinfo;
            }
        }
    }
}