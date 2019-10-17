using SldWorks;
using System;
using System.Threading;

namespace AutoAssemblyRebuild
{
    class Program
    {
        static void Main(string[] args)
        {
            var NO_ERROR = true;
            var swInstance = new SldWorks.SldWorks();
            var model = (ModelDoc2)swInstance.ActiveDoc;
            // TODO - for the assembly rebuild daemon - if a feature is flipped write to the assembly config
            //  - the current state if it's opposite - for this program that variable is read-only again
            var matesToFlip = new string[] { };

            // read rebuil.txt app data file
            var rebuildAppDataPath = @"C:\Users\bolinger\Documents\SolidWorks Projects\Prefab Blob - Cover Blob\app data\rebuild.txt";
            var rebuildAppDataLines = System.IO.File.ReadAllLines(rebuildAppDataPath);
            var assemblyConfigPath = rebuildAppDataLines[0];
            var assemblyConfigLines = System.IO.File.ReadAllLines(assemblyConfigPath);

            // get correlated X/Z negation state for hole # and affect X or Z values
            // for that hole number flip the negation state on the assembly config file
            // up to both the X and Z values for that hole number can be flipped
            var holeNumber = "";
            var flipX = false;
            var flipZ = false;
            foreach (string appDataLine in rebuildAppDataLines) {
                foreach (string line in assemblyConfigLines)
                {
                    if (line.Contains(appDataLine) && !flipX)
                    {
                        if (line.Contains("X"))
                        {
                            flipX = true;

                            holeNumber = line.Split('=')[1].Split(' ')[2].Trim();
                        }
                    }

                    if (line.Contains(appDataLine) && !flipZ) { 
                        if (line.Contains("Z"))
                        {
                            flipZ = true;
                            holeNumber = line.Split('=')[1].Split(' ')[2].Trim();
                        }
                    }
                }
            }
            
            // read assembly file and generate the flipped negation state output
            if (flipX || flipZ)
            {
                for (var i = 0; i < assemblyConfigLines.Length; ++i)
                {
                    if (assemblyConfigLines[i].Contains("Negative"))
                    {
                        if (flipX && assemblyConfigLines[i].Contains("X"))
                        {
                            var lineSegments = assemblyConfigLines[i].Split('=');
                            var currentState = lineSegments[1];
                            var newLine = lineSegments[0] + "= " + 
                                (currentState.Contains("1") ? "0" : "1");
                            assemblyConfigLines[i] = newLine;
                        } else if (flipZ && assemblyConfigLines[i].Contains("Z"))
                        {
                            var lineSegments = assemblyConfigLines[i].Split('=');
                            var currentState = lineSegments[1];
                            var newLine = lineSegments[0] + "= " +
                                (currentState.Contains("1") ? "0" : "1");
                            assemblyConfigLines[i] = newLine;
                        }
                    }
                }
            }

            // write to assembly file
            var builder = "";
            foreach (string line in assemblyConfigLines)
            {
                builder += line + "\n";
            }
            NO_ERROR = flagIfEmptyLines(builder);
            if (NO_ERROR)
            {
                //System.IO.File.WriteAllText(assemblyConfigPath, builder);
            }

           // if rebuild app data contains a dimension list - creates a new array for the mates that need to be flipped
           if (rebuildAppDataLines.Length >= 2)
                {
                    if (rebuildAppDataLines[1].Contains("Distance"))
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
                                            mate.Flipped = !mate.Flipped;
                                        }
                                    }

                                    mateGroup = (Feature)mateGroup.GetNextSubFeature();
                                    ++index;
                                }
                            }
                            firstFeature = (Feature)firstFeature.GetNextFeature();
                        }

                        // remove the listed mates so it doesn't flip them again
                        System.IO.File.WriteAllText(rebuildAppDataPath, assemblyConfigPath);
                    }
                }

           //model.ForceRebuild3(true);

           //Thread.Sleep(500);

           //model.ForceRebuild3(true);
        }

        static bool flagIfEmptyLines(String output)
        {
            var lines = output.Split('\n');
            var isBool = false;
            foreach (string line in lines)
            {
                if (line.Length == 0)
                {
                    Console.WriteLine("ERROR: EMPTY LINE");
                     isBool = false;
                } else
                {
                     isBool = true;
                }
            }
            return isBool;
        }
    }
}
