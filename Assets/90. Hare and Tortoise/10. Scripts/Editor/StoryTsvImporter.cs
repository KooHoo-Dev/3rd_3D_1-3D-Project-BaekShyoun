using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace HareAndTortoise.Editor
{
    // TSV 원본을 빌드에도 포함할 수 있는 TextAsset으로 가져온다.
    [ScriptedImporter(1, "tsv")]
    public class StoryTsvImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            var text = new TextAsset(File.ReadAllText(context.assetPath, Encoding.UTF8));
            text.name = Path.GetFileNameWithoutExtension(context.assetPath);
            context.AddObjectToAsset("text", text);
            context.SetMainObject(text);
        }
    }
}
