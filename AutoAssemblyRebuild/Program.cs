using SldWorks;
using System;
using System.Threading;

namespace AutoAssemblyRebuild
{
    class Program
    {
        static void Main(string[] args)
        {
            var swInstance = new SldWorks.SldWorks();
            var model = (ModelDoc2)swInstance.ActiveDoc;

            var matesToFlip = new string[] { };

            // read rebuil.txt app data file
            var rebuildAppDataPath = @"C:\Users\bolinger\Documents\SolidWorks Projects\Prefab Blob - Cover Blob\app data\rebuild.txt";
            var rebuildAppDataLines = System.IO.File.ReadAllLines(rebuildAppDataPath);
            var assemblyConfigPath = @rebuildAppDataLines[0];
            var assemblyConfigLines = System.IO.File.ReadAllLines(assemblyConfigPath);

            // if rebuild app data contains a dimension list - creates a new array for the mates that need to be flipped
            if (rebuildAppDataLines.Length > 1)
            {
                matesToFlip = new string[rebuildAppDataLines.Length - 1];
                for (var i = 1; i < rebuildAppDataLines.Length; ++i)
                {
                    matesToFlip[i - 1] = rebuildAppDataLines[i];
                }
                // flips the mate if the X/Z offset is negative relative to current position
                var cutOff = 5_000;
                var firstFeature = (Feature)model.FirstFeature();
                while (firstFeature != null && cutOff-- > 0)
                {
                    if ("MateGroup" == firstFeature.GetTypeName())
                    {
                        var mateGroup = (Feature)firstFeature.GetFirstSubFeature();
                        var index = 0;
                        while (mateGroup != null)
                        {
                            var mate = (Mate2)mateGroup.GetSpecificFeature2();
                            var mateName = mateGroup.Name;
                            foreach (string dimension in matesToFlip)
                            {
                                if (dimension == mateName)
                                {
                                    Console.WriteLine(mate.Flipped);
                                    mate.Flipped = !mate.Flipped;
                                    Console.WriteLine(mate.Flipped);
                                }
                            }

                            mateGroup = (Feature)mateGroup.GetNextSubFeature();
                            ++index;
                        }
                    }
                    firstFeature = (Feature)firstFeature.GetNextFeature();
                }
                

                Thread.Sleep(1_000);
            }

            model.ForceRebuild3(true);
        }
    }
}
